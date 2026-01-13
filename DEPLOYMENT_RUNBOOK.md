# Deployment Runbook for Commanda

## Prerequisites
- .NET 8.0 SDK installed
- Windows x64 machine
- GitHub repository access

## Deployment Steps
1. Ensure all changes are committed and pushed to the main branch.
2. GitHub Actions CI/CD pipeline will automatically trigger on push to main/develop.
3. Pipeline performs:
   - Restore dependencies
   - Build in Release mode
   - Run unit and integration tests
   - Create self-contained executable
   - Upload build artifacts
4. Download the executable from GitHub Actions artifacts or Releases.
5. Extract and run Commanda.exe on target Windows machine.

## Rollback Procedures
1. If new deployment has issues, identify the problem from logs or user feedback.
2. Download the previous stable version from GitHub Releases.
3. Stop the current application process.
4. Replace the executable with the previous version.
5. Restart the application.
6. Monitor for resolution.

## Monitoring
- Check GitHub Actions logs for build/test failures.
- Application logs (if configured with Serilog).
- User feedback for runtime issues.

## Health Checks
- Application should start without errors.
- Basic UI responsiveness.