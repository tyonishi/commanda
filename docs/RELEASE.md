# Release Guide

This guide explains how to build and release Commanda.

## Prerequisites

- Windows 10/11 (64-bit)
- .NET 8.0 SDK or later
- PowerShell 5.1 or later
- Git

## Quick Build

### Option 1: Using PowerShell Script (Recommended)

```powershell
# Navigate to the repository root
cd C:\Users\tyonishi\source\repos\commanda

# Run the build script
.\tools\build-release.ps1

# Or skip tests for faster build
.\tools\build-release.ps1 -SkipTests
```

The script will:
1. Clean previous builds
2. Restore NuGet packages
3. Build the solution in Release mode
4. Run all tests
5. Publish self-contained executable
6. Create release package (ZIP file)
7. Generate checksums

Output will be in the `artifacts/` directory.

### Option 2: Manual Build

```powershell
# Navigate to repository
cd C:\Users\tyonishi\source\repos\commanda

# Restore packages
dotnet restore

# Build solution
dotnet build -c Release

# Run tests
dotnet test -c Release --no-build

# Publish executable
dotnet publish src/Commanda/Commanda.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  --output ./publish

# Create ZIP package
Compress-Archive -Path ./publish/* -DestinationPath ./Commanda-v1.0.0-win-x64.zip
```

## Automated Release (GitHub Actions)

### Creating a Release

1. **Update version numbers** in all `.csproj` files:
   - `src/Commanda/Commanda.csproj`
   - `src/Commanda.Core/Commanda.Core.csproj`
   - `src/Commanda.Mcp/Commanda.Mcp.csproj`
   - `src/Commanda.Extensions/Commanda.Extensions.csproj`

2. **Update CHANGELOG.md** with new version details

3. **Commit changes**:
   ```bash
   git add .
   git commit -m "chore(release): prepare v1.0.0"
   ```

4. **Create and push tag**:
   ```bash
   git tag -a v1.0.0 -m "Release v1.0.0"
   git push origin main
   git push origin v1.0.0
   ```

5. **GitHub Actions will automatically**:
   - Build the solution
   - Run all tests
   - Create release package
   - Generate release notes from CHANGELOG.md
   - Create GitHub Release with artifacts

### CI/CD Workflows

- **CI Workflow** (`.github/workflows/ci.yml`): Runs on every push to `main` or `develop`
- **Release Workflow** (`.github/workflows/release.yml`): Runs on every version tag push

## Release Checklist

Before creating a release, verify:

- [ ] All tests pass locally
- [ ] Version numbers updated in all project files
- [ ] CHANGELOG.md updated with new features/fixes
- [ ] README.md updated if needed
- [ ] Documentation reflects current state
- [ ] No sensitive data in code (API keys, passwords)
- [ ] Build succeeds without warnings
- [ ] Application launches and basic functions work

## Version Numbering

We follow [Semantic Versioning](https://semver.org/):

- **MAJOR** (X.0.0): Breaking changes
- **MINOR** (0.X.0): New features, backward compatible
- **PATCH** (0.0.X): Bug fixes, backward compatible

Pre-release versions use suffixes:
- `1.0.0-alpha.1` - Alpha release
- `1.0.0-beta.1` - Beta release
- `1.0.0-rc.1` - Release candidate

## Distribution

### GitHub Releases

Primary distribution method. Users download:
- `Commanda-vX.X.X-win-x64.zip` - Main application
- `checksums.txt` - SHA256 checksums for verification

### Manual Distribution

After building locally, distribute:
- `artifacts/Commanda-vX.X.X-win-x64.zip`
- `artifacts/checksums.txt`

## Troubleshooting

### Build Failures

**Issue**: `dotnet restore` fails
- **Solution**: Check internet connection, clear NuGet cache: `dotnet nuget locals all --clear`

**Issue**: Tests fail
- **Solution**: Run tests individually to identify failing test: `dotnet test --filter "FullyQualifiedName~TestName"`

**Issue**: WPF build errors
- **Solution**: Ensure Windows SDK is installed, use Visual Studio or VS Build Tools

### Release Issues

**Issue**: GitHub Actions fails
- **Solution**: Check workflow logs, ensure secrets are configured

**Issue**: Tag push doesn't trigger release
- **Solution**: Ensure tag follows pattern `v*.*.*` (e.g., `v1.0.0`)

## Post-Release

After releasing:

1. **Verify release**:
   - Download ZIP from GitHub Releases
   - Extract and test executable
   - Verify checksum matches

2. **Update documentation**:
   - Update installation instructions if needed
   - Update screenshots if UI changed

3. **Announce**:
   - Post release notes to relevant channels
   - Update website/download links

4. **Monitor**:
   - Watch for bug reports
   - Monitor download statistics

## Support

For release-related questions:
- Check [CHANGELOG.md](../CHANGELOG.md) for version history
- Review [GitHub Issues](https://github.com/your-org/commanda/issues)
- Contact maintainers
