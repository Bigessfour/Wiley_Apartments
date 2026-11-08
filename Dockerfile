# ClerkSuite — Wiley Apartments (SQLite default)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV TZ=America/Denver

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Wiley.Apartments.Web/Wiley.Apartments.Web.csproj", "src/Wiley.Apartments.Web/"]
COPY ["src/Wiley.Apartments.Domain/Wiley.Apartments.Domain.csproj", "src/Wiley.Apartments.Domain/"]
COPY ["src/Wiley.Apartments.Contracts/Wiley.Apartments.Contracts.csproj", "src/Wiley.Apartments.Contracts/"]
RUN dotnet restore "src/Wiley.Apartments.Web/Wiley.Apartments.Web.csproj"
COPY . .
WORKDIR "/src/src/Wiley.Apartments.Web"
RUN dotnet publish "Wiley.Apartments.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /data /docs
VOLUME ["/data", "/docs"]
ENV ConnectionStrings__DefaultConnection="Data Source=/data/clerksuite.db"
ENV ClerkSuite__DocumentRoot="/docs"
ENV ClerkSuite__DatabaseProvider="Sqlite"
ENV ClerkSuite__LateFeesEnabled="false"
ENTRYPOINT ["dotnet", "Wiley.Apartments.Web.dll"]
