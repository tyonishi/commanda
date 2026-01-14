using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Commanda.Core;

namespace Commanda.Core.Tests;

public class CommandaDbContextTests : IDisposable
{
    private readonly CommandaDbContext _context;

    public CommandaDbContextTests()
    {
        var options = new DbContextOptionsBuilder<CommandaDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        _context = new CommandaDbContext(options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task CanAddAndRetrieveExecutionLog()
    {
        // Arrange
        var log = new ExecutionLog
        {
            Timestamp = DateTime.UtcNow,
            TaskDescription = "Test task",
            Status = "Completed",
            Result = "Success",
            Duration = TimeSpan.FromSeconds(1),
            StepsExecuted = 1
        };

        // Act
        _context.ExecutionLogs.Add(log);
        await _context.SaveChangesAsync();

        var retrieved = await _context.ExecutionLogs.FirstOrDefaultAsync(l => l.TaskDescription == "Test task");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("Test task", retrieved!.TaskDescription);
        Assert.Equal("Completed", retrieved!.Status);
    }

    [Fact]
    public async Task CanAddAndRetrieveTaskHistory()
    {
        // Arrange
        var history = new TaskHistory
        {
            SessionId = "session123",
            UserInput = "Test input",
            ExecutionPlan = "{}",
            Status = "Completed",
            FinalResult = "Result"
        };

        // Act
        _context.TaskHistories.Add(history);
        await _context.SaveChangesAsync();

        var retrieved = await _context.TaskHistories.FirstOrDefaultAsync(h => h.SessionId == "session123");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("Test input", retrieved!.UserInput);
        Assert.Equal("Completed", retrieved!.Status);
    }

    [Fact]
    public async Task CanAddAndRetrieveExtensionInfo()
    {
        // Arrange
        var extension = new ExtensionInfo
        {
            Name = "TestExtension",
            Version = "1.0.0",
            AssemblyPath = "/path/to/extension.dll",
            IsEnabled = true
        };

        // Act
        _context.Extensions.Add(extension);
        await _context.SaveChangesAsync();

        var retrieved = await _context.Extensions.FirstOrDefaultAsync(e => e.Name == "TestExtension");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("TestExtension", retrieved!.Name);
        Assert.Equal("1.0.0", retrieved!.Version);
        Assert.True(retrieved!.IsEnabled);
    }

    [Fact]
    public async Task CanAddAndRetrieveLlmProviderConfig()
    {
        // Arrange
        var provider = new LlmProviderConfig
        {
            Name = "TestProvider",
            ProviderType = "OpenAI",
            ApiKey = "encrypted_key",
            BaseUri = "https://api.openai.com",
            ModelName = "gpt-3.5-turbo",
            IsDefault = true
        };

        // Act
        _context.LlmProviders.Add(provider);
        await _context.SaveChangesAsync();

        var retrieved = await _context.LlmProviders.FirstOrDefaultAsync(p => p.Name == "TestProvider");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("TestProvider", retrieved!.Name);
        Assert.Equal("OpenAI", retrieved!.ProviderType);
        Assert.True(retrieved!.IsDefault);
    }

    [Fact]
    public async Task ExtensionNameMustBeUnique()
    {
        // Arrange
        var extension1 = new ExtensionInfo
        {
            Name = "DuplicateName",
            Version = "1.0.0",
            AssemblyPath = "/path1.dll"
        };

        var extension2 = new ExtensionInfo
        {
            Name = "DuplicateName",
            Version = "2.0.0",
            AssemblyPath = "/path2.dll"
        };

        // Act & Assert
        _context.Extensions.Add(extension1);
        _context.Extensions.Add(extension2);

        await Assert.ThrowsAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }
}