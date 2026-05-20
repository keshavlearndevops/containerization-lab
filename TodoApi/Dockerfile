# Base image: full .NET 8 SDK — needed to run "dotnet publish" during build
FROM mcr.microsoft.com/dotnet/sdk:8.0

# Set the working directory inside the container
WORKDIR /app

# Copy everything from your machine into /app inside the image
COPY . .

# Restore NuGet packages and publish a Release build into /app/publish
RUN dotnet publish -c Release -o /app/publish

# Tell Docker which port the app listens on (documentation only)
EXPOSE 8080

# Command that runs when the container starts
ENTRYPOINT ["dotnet", "/app/publish/TodoApi.dll"]
