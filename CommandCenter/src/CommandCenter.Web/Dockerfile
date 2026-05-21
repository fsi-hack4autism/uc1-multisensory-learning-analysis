# ── Build stage ────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["CommandCenter.Web/CommandCenter.Web.csproj", "CommandCenter.Web/"]
COPY ["CommandCenter.Application/CommandCenter.Application.csproj", "CommandCenter.Application/"]
COPY ["CommandCenter.Domain/CommandCenter.Domain.csproj", "CommandCenter.Domain/"]
COPY ["CommandCenter.Infrastructure/CommandCenter.Infrastructure.csproj", "CommandCenter.Infrastructure/"]

RUN dotnet restore "CommandCenter.Web/CommandCenter.Web.csproj"

COPY . .
WORKDIR "/src/CommandCenter.Web"
RUN dotnet publish "CommandCenter.Web.csproj" -c Release -o /app/publish

# ── Runtime stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN addgroup --system app && adduser --system --ingroup app app
USER app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:$PORT
EXPOSE 8080

ENTRYPOINT ["dotnet", "CommandCenter.Web.dll"]
