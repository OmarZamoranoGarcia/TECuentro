# Compilación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["TEContigo.csproj", "./"]

RUN dotnet restore "TEContigo.csproj"

COPY . .

RUN dotnet publish "TEContigo.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# Ejecución
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Dependencia nativa requerida por GSSAPI/Kerberos
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "TEContigo.dll"]