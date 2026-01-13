# Dockerfile for Commanda - Windows WPF Application
# Security best practices: Use multi-stage build, non-root user (Windows equivalent), minimal attack surface

# Build stage using Windows Server Core for WPF compatibility
FROM mcr.microsoft.com/dotnet/sdk:8.0-windowsservercore-ltsc2022 AS build
# Critical decision: Use Windows Server Core base image as WPF applications require Windows GUI components not available in Nano Server

WORKDIR /src

# Copy project files for efficient caching
COPY ["src/Commanda/Commanda.csproj", "Commanda/"]
COPY ["src/Commanda.Core/Commanda.Core.csproj", "Commanda.Core/"]
COPY ["src/Commanda.Mcp/Commanda.Mcp.csproj", "Commanda.Mcp/"]
COPY ["src/Commanda.Extensions/Commanda.Extensions.csproj", "Commanda.Extensions/"]

# Restore dependencies
RUN dotnet restore "Commanda/Commanda.csproj"
# Critical decision: Restore only once to leverage Docker layer caching

# Copy source code
COPY . .

# Build application
RUN dotnet build "Commanda/Commanda.csproj" -c Release -o /app/build
# Critical decision: Build in Release configuration for optimized production binaries

# Publish stage
FROM build AS publish
RUN dotnet publish "Commanda/Commanda.csproj" -c Release -o /app/publish --self-contained --runtime win-x64
# Critical decision: Self-contained publish includes runtime, no external dependencies needed; win-x64 matches project configuration

# Runtime stage using Windows Server Core (GUI-capable)
FROM mcr.microsoft.com/windows/servercore:ltsc2022 AS runtime
# Critical decision: Use Server Core for GUI applications; smaller than full Windows but includes necessary components

# Create non-admin user for security
RUN net user appuser /add /passwordreq:no
RUN net localgroup "Users" appuser /add
USER appuser
# Critical decision: Run as non-admin user to follow principle of least privilege

WORKDIR /app
COPY --from=publish /app/publish .

# Health check (basic process check)
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD powershell -command "if (Get-Process -Name Commanda -ErrorAction SilentlyContinue) { exit 0 } else { exit 1 }"
# Critical decision: Basic health check to verify application process is running

ENTRYPOINT ["Commanda.exe"]