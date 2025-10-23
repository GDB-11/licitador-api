using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using System.Text;
using Application.Core.Interfaces;
using Application.Core.Interfaces.Account;
using Application.Core.Interfaces.Shared;
using Application.Core.Services;
using Application.Core.Services.Account;
using Application.Core.Services.Shared;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

#region Register services
builder.Services.AddScoped<IAccount, AccountService>();
builder.Services.AddScoped<IAsymmetricFieldEncryption, AsymmetricFieldEncryptionService>();
builder.Services.AddScoped<IChaChaEncryption, ChaChaEncryptionService>();
builder.Services.AddScoped<IDeterministicEncryption, DeterministicAesEncryptionService>();
builder.Services.AddScoped<IEncryption, EncryptionService>();
builder.Services.AddScoped<ITimeProvider, SystemTimeProviderService>();
#endregion

#region Register repositories

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

    /*options.AddPolicy("ProductionCors", policy =>
    {
        policy.WithOrigins(builder.Configuration["AllowedOrigins"])
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });*/
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