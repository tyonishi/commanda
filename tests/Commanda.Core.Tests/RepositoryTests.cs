using Xunit;
using Microsoft.EntityFrameworkCore;
using Commanda.Core;
using System.Runtime.Versioning;

namespace Commanda.Core.Tests;

[SupportedOSPlatform("windows")]
public class RepositoryTests : IDisposable
{
    private readonly CommandaDbContext _context;
    private readonly Repository<ExecutionLog> _repository;

    public RepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CommandaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CommandaDbContext(options);
        _context.Database.EnsureCreated();
        _repository = new Repository<ExecutionLog>(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task AddAsync_AddsEntityToDatabase()
    {
        // Arrange
        var log = new ExecutionLog
        {
            TaskDescription = "Test task",
            Status = "Completed",
            Duration = TimeSpan.FromSeconds(1),
            StepsExecuted = 1
        };

        // Act
        var result = await _repository.AddAsync(log);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test task", result.TaskDescription);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsEntity_WhenEntityExists()
    {
        // Arrange
        var log = new ExecutionLog
        {
            TaskDescription = "Test task",
            Status = "Completed",
            Duration = TimeSpan.FromSeconds(1),
            StepsExecuted = 1
        };
        await _repository.AddAsync(log);

        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test task", result!.TaskDescription);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenEntityDoesNotExist()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindAsync_ReturnsMatchingEntities()
    {
        // Arrange
        await _repository.AddAsync(new ExecutionLog { TaskDescription = "Task 1", Status = "Completed", Duration = TimeSpan.FromSeconds(1), StepsExecuted = 1 });
        await _repository.AddAsync(new ExecutionLog { TaskDescription = "Task 2", Status = "Failed", Duration = TimeSpan.FromSeconds(2), StepsExecuted = 1 });

        // Act
        var results = await _repository.FindAsync(l => l.Status == "Completed");

        // Assert
        Assert.Single(results);
        Assert.Equal("Task 1", results.First().TaskDescription);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEntityInDatabase()
    {
        // Arrange
        var log = new ExecutionLog { TaskDescription = "Original", Status = "Running", Duration = TimeSpan.FromSeconds(1), StepsExecuted = 1 };
        await _repository.AddAsync(log);

        // Act
        log.Status = "Completed";
        await _repository.UpdateAsync(log);
        var updated = await _repository.GetByIdAsync(log.Id);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal("Completed", updated!.Status);
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntityFromDatabase()
    {
        // Arrange
        var log = new ExecutionLog { TaskDescription = "To delete", Status = "Running", Duration = TimeSpan.FromSeconds(1), StepsExecuted = 1 };
        await _repository.AddAsync(log);

        // Act
        await _repository.DeleteAsync(log);
        var result = await _repository.GetByIdAsync(log.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_WhenEntityExists()
    {
        // Arrange
        var log = new ExecutionLog { TaskDescription = "Exists test", Status = "Running", Duration = TimeSpan.FromSeconds(1), StepsExecuted = 1 };
        await _repository.AddAsync(log);

        // Act
        var exists = await _repository.ExistsAsync(l => l.TaskDescription == "Exists test");

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        // Arrange
        await _repository.AddAsync(new ExecutionLog { TaskDescription = "Task 1", Status = "Completed", Duration = TimeSpan.FromSeconds(1), StepsExecuted = 1 });
        await _repository.AddAsync(new ExecutionLog { TaskDescription = "Task 2", Status = "Completed", Duration = TimeSpan.FromSeconds(1), StepsExecuted = 1 });
        await _repository.AddAsync(new ExecutionLog { TaskDescription = "Task 3", Status = "Failed", Duration = TimeSpan.FromSeconds(1), StepsExecuted = 1 });

        // Act
        var count = await _repository.CountAsync(l => l.Status == "Completed");

        // Assert
        Assert.Equal(2, count);
    }
}