# Use the official Microsoft ASP.NET Core runtime image
# Adjust the version tag as needed to match your project's target framework
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8000

# Use the SDK image for building the project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Online_API.csproj", "./"]
RUN dotnet restore "Online_API.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "Online_API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Online_API.csproj" -c Release -o /app/publish

# Final stage/image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Online_API.dll"]