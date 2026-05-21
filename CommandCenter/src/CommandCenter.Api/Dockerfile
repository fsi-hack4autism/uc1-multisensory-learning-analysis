# ── Build stage ────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for layer caching
COPY ["CommandCenter.Api/CommandCenter.Api.csproj", "CommandCenter.Api/"]
COPY ["CommandCenter.Application/CommandCenter.Application.csproj", "CommandCenter.Application/"]
COPY ["CommandCenter.Domain/CommandCenter.Domain.csproj", "CommandCenter.Domain/"]
COPY ["CommandCenter.Infrastructure/CommandCenter.Infrastructure.csproj", "CommandCenter.Infrastructure/"]

RUN dotnet restore "CommandCenter.Api/CommandCenter.Api.csproj"

COPY . .
WORKDIR "/src/CommandCenter.Api"
RUN dotnet publish "CommandCenter.Api.csproj" -c Release -o /app/publish

# ── Runtime stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Non-root user for Cloud Run security
RUN addgroup --system app && adduser --system --ingroup app app
USER app

COPY --from=build /app/publish .

# Cloud Run injects PORT env var; ASP.NET Core listens on it via ASPNETCORE_URLS
ENV ASPNETCORE_URLS=http://+:$PORT
EXPOSE 8080

ENTRYPOINT ["dotnet", "CommandCenter.Api.dll"]
