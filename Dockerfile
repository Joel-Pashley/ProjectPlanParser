FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
RUN apt-get update && apt-get install -y libgdiplus libx11-dev && rm -rf /var/lib/apt/lists/*
WORKDIR /app
# Create a dedicated directory for licenses and set ownership
RUN mkdir -p /app/licenses && chown -R $APP_UID:$APP_UID /app/licenses
USER $APP_UID
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ProjectPlanParser/ProjectPlanParser.csproj", "ProjectPlanParser/"]
RUN dotnet restore "ProjectPlanParser/ProjectPlanParser.csproj"
COPY . .
WORKDIR "/src/ProjectPlanParser"
RUN dotnet build "./ProjectPlanParser.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ProjectPlanParser.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProjectPlanParser.dll"]
