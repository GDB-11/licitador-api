using Application.Core.Config;
using Application.Core.Interfaces.Auth;
using Application.Core.Interfaces.Company;
using Application.Core.Interfaces.Document;
using Application.Core.Interfaces.Shared;
using Application.Core.Services.Auth;
using Application.Core.Services.Company;
using Application.Core.Services.Document;
using Application.Core.Services.Shared;
using Global.Objects.Auth;
using Global.Objects.Company;
using Global.Objects.Document;
using Global.Objects.Encryption;
using Infrastructure.Core.Interfaces.Account;
using Infrastructure.Core.Interfaces.Security;
using Infrastructure.Core.Services.Account;
using Infrastructure.Core.Services.Security;
using Licitador.WebAPI.Logging;
using Licitador.WebAPI.Mappings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using Scalar.AspNetCore;
using System.Data;
using System.Text;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

#region Register Masterkeys
EncryptionConfig encryptionConfig = new()
{
    MasterKey = builder.Configuration["Encryption:MasterKey"] ?? throw new NullReferenceException("MasterKey")
};
builder.Services.AddSingleton(encryptionConfig);

DeterministicEncryptionConfig deterministicEncryptionConfig = new()
{
    MasterKey = builder.Configuration["DeterministicEncryption:MasterKey"] ?? throw new NullReferenceException("MasterKey"),
    IvGenerationKey = builder.Configuration["DeterministicEncryption:IvGenerationKey"] ?? throw new NullReferenceException("IvGenerationKey")
};
builder.Services.AddSingleton(deterministicEncryptionConfig);
#endregion

#region Register Masterkey
JwtConfig jwtConfig = new()
{
    SecretKey = builder.Configuration["JwtSettings:SecretKey"] ?? throw new NullReferenceException("SecretKey"),
    Issuer = builder.Configuration["JwtSettings:Issuer"] ?? throw new NullReferenceException("Issuer"),
    Audience = builder.Configuration["JwtSettings:Audience"] ?? throw new NullReferenceException("Audience"),
    AccessTokenExpiryMinutes = int.Parse(builder.Configuration["JwtSettings:AccessTokenExpiryMinutes"] ?? throw new NullReferenceException("AccessTokenExpiryMinutes")),
    RefreshTokenExpiryMinutes = int.Parse(builder.Configuration["JwtSettings:RefreshTokenExpiryMinutes"] ?? throw new NullReferenceException("RefreshTokenExpiryMinutes"))
};
builder.Services.AddSingleton(jwtConfig);
#endregion

builder.Services.AddTransient<IDbConnection>(sp => 
    new NpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

#region Register repositories
builder.Services.AddScoped<IKeyRepository, KeyRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
#endregion

#region Register loggers
builder.Services.AddScoped<IResultLogger, ConsoleResultLogger>();
#endregion

#region Register HttpErrorMappers (One per controller)
builder.Services.AddScoped<IErrorHttpMapper<ChaChaEncryptionError>, ChaChaEncryptionErrorMapper>();
builder.Services.AddScoped<IErrorHttpMapper<AuthError>, AuthErrorMapper>();
builder.Services.AddScoped<IErrorHttpMapper<CompanyError>, CompanyErrorMapper>();
builder.Services.AddScoped<IErrorHttpMapper<DocumentError>, DocumentErrorMapper>();
#endregion

#region Register services
builder.Services.AddScoped<IJwt, JwtService>();
builder.Services.AddScoped<IPassword, PasswordService>();
builder.Services.AddScoped<IAsymmetricFieldEncryption, AsymmetricFieldEncryptionService>();
builder.Services.AddScoped<IChaChaEncryption, ChaChaEncryptionService>();
builder.Services.AddScoped<IDeterministicEncryption, DeterministicAesEncryptionService>();
builder.Services.AddScoped<IEncryption, EncryptionService>();
builder.Services.AddScoped<ITimeProvider, SystemTimeProviderService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthentication, AuthenticationService>();
builder.Services.AddScoped<ICompany, CompanyService>();
builder.Services.AddScoped<IDocument, DocumentService>();
#endregion

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        // Ensure Components is initialized
        document.Components ??= new OpenApiComponents();

        // Add the Bearer security scheme
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter 'Bearer' followed by your JWT token, e.g., 'Bearer eyJ...'"
        };

        // Add global security requirement (optional, applies to all endpoints)
        document.SecurityRequirements.Add(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        return Task.CompletedTask;
    });
});

builder.Services.AddHttpContextAccessor();

builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = int.MaxValue;
    options.MemoryBufferThreshold = int.MaxValue;
});

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentCors", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });

    options.AddPolicy("ProductionCors", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();
        
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Enable OpenAPI/Swagger
    app.MapOpenApi();

    // Use Scalar for beautiful API documentation
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("API de Licitaciones Públicas - Ley N° 32069")
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
            .AddPreferredSecuritySchemes()
            /*.AddApiKeyAuthentication("ApiKeyAuth", scheme =>
            {
                scheme. = "Bearer";
                scheme.Name = "Authorization"; // Typically the header name
                scheme.Location = ScalarApiKeyLocation.Header;
            })*/;
    });

    app.UseCors("DevelopmentCors");
}
else
{
    app.UseCors("ProductionCors");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();