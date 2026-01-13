# Makefile for Commanda project
# Developer Experience optimizations

.PHONY: commit-security build test clean

# Commit security-related changes
commit-security:
	git add src/Commanda.Core/InputValidator.cs src/Commanda.Core/SecureStorage.cs tests/Commanda.Core.Tests/InputValidatorTests.cs
	git commit -m "feat: add input validation and secure API key storage"

# Build the project
build:
	dotnet build

# Run tests
test:
	dotnet test

# Clean build artifacts
clean:
	dotnet clean