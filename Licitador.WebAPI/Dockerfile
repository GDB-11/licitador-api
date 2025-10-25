FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Licitador.WebAPI/Licitador.WebAPI.csproj", "Licitador.WebAPI/"]
COPY ["Application.Core.Services/Application.Core.Services.csproj", "Application.Core.Services/"]
COPY ["Application.Core.Interfaces/Application.Core.Interfaces.csproj", "Application.Core.Interfaces/"]
COPY ["Application.Core.DTOs/Application.Core.DTOs.csproj", "Application.Core.DTOs/"]
COPY ["Infrastructure.Core.Services/Infrastructure.Core.Services.csproj", "Infrastructure.Core.Services/"]
COPY ["Infrastructure.Core.Interfaces/Infrastructure.Core.Interfaces.csproj", "Infrastructure.Core.Interfaces/"]
COPY ["Infrastructure.Core.Models/Infrastructure.Core.Models.csproj", "Infrastructure.Core.Models/"]
COPY ["Global.Helpers/Global.Helpers.csproj", "Global.Helpers/"]
COPY ["Global.Objects/Global.Objects.csproj", "Global.Objects/"]
COPY ["Global.Attributes/Global.Attributes.csproj", "Global.Attributes/"]
COPY ["Application.Core.Config/Application.Core.Config.csproj", "Application.Core.Config/"]
RUN dotnet restore "Licitador.WebAPI/Licitador.WebAPI.csproj"
COPY . .
WORKDIR "/src/Licitador.WebAPI"
RUN dotnet build "./Licitador.WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Licitador.WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Licitador.WebAPI.dll"]
