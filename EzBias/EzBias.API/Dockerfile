# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["EzBias.API/EzBias.API.csproj", "EzBias.API/"]
COPY ["EzBias.Application/EzBias.Application.csproj", "EzBias.Application/"]
COPY ["EzBias.Domain/EzBias.Domain.csproj", "EzBias.Domain/"]
COPY ["EzBias.Infrastructure/EzBias.Infrastructure.csproj", "EzBias.Infrastructure/"]

RUN dotnet restore "EzBias.API/EzBias.API.csproj"

COPY . .
WORKDIR "/src/EzBias.API"
RUN dotnet publish "EzBias.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EzBias.API.dll"]
