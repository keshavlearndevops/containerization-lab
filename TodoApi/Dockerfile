# ══════════════════════════════════════════════════════════════════
# STAGE 1 — Build
# ══════════════════════════════════════════════════════════════════
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["TodoApi.csproj", "."]
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# ══════════════════════════════════════════════════════════════════
# STAGE 2 — Final (runtime only, ~200 MB)
# ══════════════════════════════════════════════════════════════════
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "TodoApi.dll"]
