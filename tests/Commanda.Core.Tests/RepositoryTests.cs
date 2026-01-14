using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Commanda.Core;

namespace Commanda.Core.Tests;

[TestFixture]
public class RepositoryTests
{
    private CommandaDbContext _context = null!;
    private Repository<ExecutionLog> _repository = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<CommandaDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        _context = new CommandaDbContext(options);
        _context.Database.EnsureCreated();
        _repository = new Repository<ExecutionLog>(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
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
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual("Test task", result.TaskDescription);
    }

    [Test]
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
        Assert.IsNotNull(result);
        Assert.AreEqual("Test task", result!.TaskDescription);
    }

    [Test]
    public async Task GetByIdAsync_ReturnsNull_WhenEntityDoesNotExist()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        Assert.IsNull(result);
    }

    [Test]
    public async Task FindAsync_ReturnsMatchingEntities()
    {
        // Arrange
        await _repository.AddAsync(new ExecutionLog { TaskDescription = "Task 1", Status = "Completed", Duration = TimeSpan.FromSeconds(1), StepsExecuted = 1 });
        await _repository.AddAsync(new ExecutionLog { TaskDescription = "Task 2", Status = "Failed", Duration = TimeSpan.FromSeconds(2), StepsExecuted = 1 });

        // Act
        var results = await _repository.FindAsync(l => l.Status == "Completed");

        // Assert
        Assert.AreEqual(1, results.Count());
        Assert.AreEqual("Task 1", results.First().TaskDescription);
    }

    [Test]
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
        Assert.IsNotNull(updated);
        Assert.AreEqual("Completed", updated!.Status);
    }

    [Test]
    public async Task DeleteAsync_RemovesEntityFromDatabase()
    {
        // Arrange
        var log = new ExecutionLog { TaskDescription = "To delete", Status = "Running", Duration = TimeSpan.FromSeconds(1), StepsExecuted = 1 };
        await _repository.AddAsync(log);

        // Act
        await _repository.DeleteAsync(log);
        var result = await _repository.GetByIdAsync(log.Id);

        // Assert
        Assert.IsNull(result);
    }

    [Test]
    public async Task ExistsAsync_ReturnsTrue_WhenEntityExists()
    {
        // Arrange
        var log = new ExecutionLog { TaskDescription = "Exists test", Status = "Running", Duration = TimeSpan.FromSeconds(1), StepsExecuted = 1 };
        await _repository.AddAsync(log);

        // Act
        var exists = await _repository.ExistsAsync(l => l.TaskDescription == "Exists test");

        // Assert
        Assert.IsTrue(exists);
    }

    [Test]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        // Arrange
        await _repository.AddAsync(new ExecutionLog { TaskDescription = "Task 1", Status = "Completed", Duration = TimeSpan.FromSeconds(1), StepsExecuted = 1 });
        await _repository.AddAsync(new ExecutionLog { TaskDescription = "Task 2", Status = "Completed", Duration = TimeSpan.FromSeconds(1), StepsExecuted = 1 });
        await _repository.AddAsync(new ExecutionLog { TaskDescription = "Task 3", Status = "Failed", Duration = TimeSpan.FromSeconds(1), StepsExecuted = 1 });

        // Act
        var count = await _repository.CountAsync(l => l.Status == "Completed");

        // Assert
        Assert.AreEqual(2, count);
    }
}