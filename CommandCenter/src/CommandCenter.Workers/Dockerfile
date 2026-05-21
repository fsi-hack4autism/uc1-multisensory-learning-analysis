# ── Build stage ────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["CommandCenter.Workers/CommandCenter.Workers.csproj", "CommandCenter.Workers/"]
COPY ["CommandCenter.Application/CommandCenter.Application.csproj", "CommandCenter.Application/"]
COPY ["CommandCenter.Domain/CommandCenter.Domain.csproj", "CommandCenter.Domain/"]
COPY ["CommandCenter.Infrastructure/CommandCenter.Infrastructure.csproj", "CommandCenter.Infrastructure/"]

RUN dotnet restore "CommandCenter.Workers/CommandCenter.Workers.csproj"

COPY . .
WORKDIR "/src/CommandCenter.Workers"
RUN dotnet publish "CommandCenter.Workers.csproj" -c Release -o /app/publish

# ── Runtime stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN addgroup --system app && adduser --system --ingroup app app
USER app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "CommandCenter.Workers.dll"]
