FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project files and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy all files and build
COPY . ./
RUN dotnet publish -c Release -o /app/out

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install timezone data and set timezone
RUN apt-get update && apt-get install -y --no-install-recommends tzdata \
    && ln -sf /usr/share/zoneinfo/Africa/Casablanca /etc/localtime \
    && echo "Africa/Casablanca" > /etc/timezone \
    && dpkg-reconfigure -f noninteractive tzdata \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/out .

# Render route le trafic HTTPS vers le port 10000 du conteneur
ENV ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000

# Exécution sans privilèges root
USER $APP_UID

ENTRYPOINT ["dotnet", "PortfolioAPI.dll"]
