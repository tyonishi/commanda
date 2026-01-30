#!/bin/bash
# Git Commit Script for Commanda

cd /c/Users/tyonishi/source/repos/commanda

echo "=========================================="
echo "Commanda Git Commit Automation Script"
echo "=========================================="
echo ""

# Step 1: Core interfaces and models
echo "Step 1/5: Core interfaces and models..."
git add src/Commanda.Core/IMcpServer.cs src/Commanda.Core/ISettingsManager.cs src/Commanda.Core/Models.cs src/Commanda.Extensions/IExtensionManager.cs src/Commanda.Extensions/ExtensionManager.cs src/Commanda.Core/SettingsManager.cs
git commit -m "feat(core): add missing interfaces and properties for WebApi support" -m "- Add AssemblyPath, IsEnabled, InstalledAt, LastUsed to IMcpExtension" -m "- Add SetExtensionEnabledAsync to IExtensionManager" -m "- Create ISettingsManager interface with LlmSettings class" -m "- Implement SettingsManager for configuration management" -m "- Update ExtensionInfo model with LastUsed property"
if [ $? -ne 0 ]; then
    echo "ERROR: Step 1 failed"
    exit 1
fi
echo "Step 1 completed successfully"
echo ""

# Step 2: WebApi integration
echo "Step 2/5: WebApi integration..."
git add Commanda.sln src/Commanda.WebApi/Commanda.WebApi.csproj src/Commanda.WebApi/Controllers/ExtensionsController.cs src/Commanda.WebApi/Controllers/SettingsController.cs src/Commanda.WebApi/Program.cs
git commit -m "feat(webapi): integrate WebApi project and fix target framework" -m "- Add Commanda.WebApi project to solution" -m "- Fix TargetFramework to net8.0-windows for compatibility" -m "- Add missing using directives and SupportedOSPlatform attributes"
if [ $? -ne 0 ]; then
    echo "ERROR: Step 2 failed"
    exit 1
fi
echo "Step 2 completed successfully"
echo ""

# Step 3: Test fixes
echo "Step 3/5: Test fixes..."
git add tests/
git commit -m "fix(tests): add platform attributes and fix build errors" -m "- Add [SupportedOSPlatform(windows)] to all test classes" -m "- Fix CS1503: use Count() instead of Count property" -m "- Fix CA1416 platform compatibility warnings" -m "- Update TestMefExtension to implement new IMcpExtension properties"
if [ $? -ne 0 ]; then
    echo "ERROR: Step 3 failed"
    exit 1
fi
echo "Step 3 completed successfully"
echo ""

# Step 4: Build infrastructure
echo "Step 4/5: Build infrastructure..."
git add tools/ .github/ tests/Commanda.UI.Tests/Commanda.UI.Tests.csproj
git commit -m "chore(build): add release automation and fix architecture" -m "- Create build-release.ps1 script for automated packaging" -m "- Add GitHub Actions release workflow" -m "- Update CI workflow with proper publish step" -m "- Fix MSB3270: align PlatformTarget to x64 for UI.Tests" -m "- Add UI.Tests project to solution"
if [ $? -ne 0 ]; then
    echo "ERROR: Step 4 failed"
    exit 1
fi
echo "Step 4 completed successfully"
echo ""

# Step 5: Documentation
echo "Step 5/5: Documentation..."
git add docs/RELEASE.md README.md
git commit -m "docs: add release guide and update build instructions" -m "- Create comprehensive release documentation" -m "- Update README with build script usage" -m "- Add release checklist and versioning guidelines"
if [ $? -ne 0 ]; then
    echo "ERROR: Step 5 failed"
    exit 1
fi
echo "Step 5 completed successfully"
echo ""

echo "=========================================="
echo "All 5 commits completed successfully!"
echo "=========================================="
echo ""
echo "Recent commits:"
git log --oneline -5
