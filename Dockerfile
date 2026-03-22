# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore (layer cache)
COPY FreakLete.Core/FreakLete.Core.csproj FreakLete.Core/
COPY FreakLete.Api/FreakLete.Api.csproj FreakLete.Api/
RUN dotnet restore FreakLete.Api/FreakLete.Api.csproj

# Copy source and publish
COPY FreakLete.Core/ FreakLete.Core/
COPY FreakLete.Api/ FreakLete.Api/
RUN dotnet publish FreakLete.Api/FreakLete.Api.csproj -c Release -o /app --no-restore

# ---- runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FreakLete.Api.dll"]
