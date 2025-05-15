FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["CodeVault.csproj", "."]
RUN dotnet restore "./CodeVault.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./CodeVault.csproj" -c $BUILD_CONFIGURATION -o /app/build


FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CodeVault.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false


FROM base AS final
WORKDIR /app

ENV OpenAI__ApiKey=""
RUN mkdir -p /app/wwwroot
COPY --from=publish /app/publish .
COPY --from=publish /app/publish/wwwroot /app/wwwroot
COPY wwwroot/ /app/wwwroot/

ENTRYPOINT ["dotnet", "CodeVault.dll"]