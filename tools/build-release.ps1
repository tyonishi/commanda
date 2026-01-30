# Commanda Build and Release Script
# This script builds the solution, runs tests, and creates a release package

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SkipTests = $false,
    [switch]$CreateInstaller = $false
)

$ErrorActionPreference = "Stop"
$SolutionDir = Split-Path -Parent $PSScriptRoot
$SolutionPath = Join-Path $SolutionDir "Commanda.sln"
$MainProject = Join-Path $SolutionDir "src\Commanda\Commanda.csproj"
$OutputDir = Join-Path $SolutionDir "artifacts"
$PublishDir = Join-Path $OutputDir "publish"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Commanda Build and Release Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration"
Write-Host "Runtime: $Runtime"
Write-Host "Solution: $SolutionPath"
Write-Host ""

# Step 1: Clean
Write-Host "Step 1: Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path $OutputDir) {
    Remove-Item -Path $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
New-Item -ItemType Directory -Path $PublishDir -Force | Out-Null

# Step 2: Restore packages
Write-Host "Step 2: Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore "$SolutionPath" --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Error "Package restore failed!"
    exit 1
}
Write-Host "  Packages restored successfully" -ForegroundColor Green

# Step 3: Build solution
Write-Host "Step 3: Building solution ($Configuration)..." -ForegroundColor Yellow
dotnet build "$SolutionPath" -c $Configuration --no-restore --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed!"
    exit 1
}
Write-Host "  Build succeeded" -ForegroundColor Green

# Step 4: Run tests
if (-not $SkipTests) {
    Write-Host "Step 4: Running tests..." -ForegroundColor Yellow
    dotnet test "$SolutionPath" -c $Configuration --no-build --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Tests failed!"
        exit 1
    }
    Write-Host "  All tests passed" -ForegroundColor Green
} else {
    Write-Host "Step 4: Skipping tests (SkipTests flag set)" -ForegroundColor Yellow
}

# Step 5: Publish self-contained executable
Write-Host "Step 5: Publishing self-contained executable..." -ForegroundColor Yellow
dotnet publish "$MainProject" -c $Configuration -r $Runtime --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    --output "$PublishDir" `
    --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Error "Publish failed!"
    exit 1
}

# Verify executable was created
$Executable = Join-Path $PublishDir "Commanda.exe"
if (-not (Test-Path $Executable)) {
    Write-Error "Executable not found at $Executable"
    exit 1
}

$FileInfo = Get-Item $Executable
Write-Host "  Published successfully" -ForegroundColor Green
Write-Host "  Executable: $($FileInfo.FullName)" -ForegroundColor Gray
Write-Host "  Size: $([math]::Round($FileInfo.Length / 1MB, 2)) MB" -ForegroundColor Gray

# Step 6: Create release package
Write-Host "Step 6: Creating release package..." -ForegroundColor Yellow
$Version = (Get-Item $Executable).VersionInfo.FileVersion
if ([string]::IsNullOrEmpty($Version)) {
    $Version = "1.0.0"
}

$ZipFileName = "Commanda-v$Version-$Runtime.zip"
$ZipPath = Join-Path $OutputDir $ZipFileName

# Create ZIP archive
Compress-Archive -Path "$PublishDir\*" -DestinationPath $ZipPath -Force
Write-Host "  Release package created: $ZipFileName" -ForegroundColor Green
Write-Host "  Location: $ZipPath" -ForegroundColor Gray

# Step 7: Generate checksums
Write-Host "Step 7: Generating checksums..." -ForegroundColor Yellow
$ChecksumFile = Join-Path $OutputDir "checksums.txt"
$Hash = Get-FileHash -Path $ZipPath -Algorithm SHA256
"$($Hash.Hash)  $ZipFileName" | Out-File -FilePath $ChecksumFile -Encoding UTF8
Write-Host "  SHA256: $($Hash.Hash)" -ForegroundColor Gray
Write-Host "  Checksums saved to: $ChecksumFile" -ForegroundColor Gray

# Step 8: Copy additional files
Write-Host "Step 8: Copying documentation..." -ForegroundColor Yellow
$DocsDir = Join-Path $OutputDir "docs"
New-Item -ItemType Directory -Path $DocsDir -Force | Out-Null

$Readme = Join-Path $SolutionDir "README.md"
$Changelog = Join-Path $SolutionDir "CHANGELOG.md"
$License = Join-Path $SolutionDir "LICENSE"

if (Test-Path $Readme) {
    Copy-Item $Readme $DocsDir
}
if (Test-Path $Changelog) {
    Copy-Item $Changelog $DocsDir
}
if (Test-Path $License) {
    Copy-Item $License $DocsDir
}
Write-Host "  Documentation copied" -ForegroundColor Green

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Build Complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Version: $Version"
Write-Host "Output Directory: $OutputDir"
Write-Host ""
Write-Host "Artifacts:" -ForegroundColor Yellow
Get-ChildItem $OutputDir -File | ForEach-Object {
    $Size = if ($_.Length -gt 1MB) { 
        "$([math]::Round($_.Length / 1MB, 2)) MB" 
    } else { 
        "$([math]::Round($_.Length / 1KB, 2)) KB" 
    }
    Write-Host "  - $($_.Name) ($Size)" -ForegroundColor Gray
}
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Test the executable: $Executable" -ForegroundColor Gray
Write-Host "  2. Review the release package: $ZipPath" -ForegroundColor Gray
Write-Host "  3. Create GitHub release with the ZIP file" -ForegroundColor Gray
Write-Host "  4. Tag the release: git tag v$Version" -ForegroundColor Gray
