using Xunit;
using Moq;
using Commanda.Core;
using System.ComponentModel;

namespace Commanda.UI.Tests;

public class HistoryViewModelTests
{
    private readonly Mock<IRepository<ExecutionLog>> _repositoryMock;
    private readonly HistoryViewModel _viewModel;

    public HistoryViewModelTests()
    {
        _repositoryMock = new Mock<IRepository<ExecutionLog>>();
        _viewModel = new HistoryViewModel(_repositoryMock.Object);
    }

    [Fact]
    public void Constructor_InitializesExecutionLogsCollection()
    {
        // Assert
        Assert.NotNull(_viewModel.ExecutionLogs);
        Assert.Empty(_viewModel.ExecutionLogs);
    }

    [Fact]
    public void Constructor_InitializesCommands()
    {
        // Assert
        Assert.NotNull(_viewModel.RefreshCommand);
        Assert.NotNull(_viewModel.ClearHistoryCommand);
        Assert.NotNull(_viewModel.ExportHistoryCommand);
    }

    [Fact]
    public void SelectedLog_SetValue_RaisesPropertyChanged()
    {
        // Arrange
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(HistoryViewModel.SelectedLog))
                propertyChangedRaised = true;
        };

        var log = new ExecutionLog { Id = 1, TaskDescription = "Test" };

        // Act
        _viewModel.SelectedLog = log;

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.Equal(log, _viewModel.SelectedLog);
    }

    [Fact]
    public void SearchText_SetValue_RaisesPropertyChanged()
    {
        // Arrange
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(HistoryViewModel.SearchText))
                propertyChangedRaised = true;
        };

        // Act
        _viewModel.SearchText = "test query";

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.Equal("test query", _viewModel.SearchText);
    }

    [Fact]
    public void StartDate_SetValue_RaisesPropertyChanged()
    {
        // Arrange
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(HistoryViewModel.StartDate))
                propertyChangedRaised = true;
        };

        var date = DateTime.Now.AddDays(-7);

        // Act
        _viewModel.StartDate = date;

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.Equal(date, _viewModel.StartDate);
    }

    [Fact]
    public void EndDate_SetValue_RaisesPropertyChanged()
    {
        // Arrange
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(HistoryViewModel.EndDate))
                propertyChangedRaised = true;
        };

        var date = DateTime.Now;

        // Act
        _viewModel.EndDate = date;

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.Equal(date, _viewModel.EndDate);
    }

    [Fact]
    public void StatusFilter_SetValue_RaisesPropertyChanged()
    {
        // Arrange
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(HistoryViewModel.StatusFilter))
                propertyChangedRaised = true;
        };

        // Act
        _viewModel.StatusFilter = "Success";

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.Equal("Success", _viewModel.StatusFilter);
    }

    [Fact]
    public void RefreshCommand_Executes_LoadsExecutionLogs()
    {
        // Arrange
        var logs = new List<ExecutionLog>
        {
            new ExecutionLog { Id = 1, TaskDescription = "Task 1", Status = "Success" },
            new ExecutionLog { Id = 2, TaskDescription = "Task 2", Status = "Failed" }
        };

        _repositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(logs);

        // Act
        _viewModel.RefreshCommand.Execute(null);

        // Assert
        Assert.Equal(2, _viewModel.ExecutionLogs.Count);
    }

    [Fact]
    public void ClearHistoryCommand_Executes_ClearsExecutionLogs()
    {
        // Arrange
        var logs = new List<ExecutionLog>
        {
            new ExecutionLog { Id = 1, TaskDescription = "Task 1" }
        };

        _repositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(logs);

        _viewModel.RefreshCommand.Execute(null);
        Assert.Single(_viewModel.ExecutionLogs);

        // Act
        _viewModel.ClearHistoryCommand.Execute(null);

        // Assert
        Assert.Empty(_viewModel.ExecutionLogs);
    }

    [Fact]
    public void FilterLogs_WithSearchText_FiltersCorrectly()
    {
        // Arrange
        var logs = new List<ExecutionLog>
        {
            new ExecutionLog { Id = 1, TaskDescription = "Open notepad", Status = "Success" },
            new ExecutionLog { Id = 2, TaskDescription = "Write file", Status = "Success" },
            new ExecutionLog { Id = 3, TaskDescription = "Close app", Status = "Failed" }
        };

        _repositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(logs);

        _viewModel.RefreshCommand.Execute(null);

        // Act
        _viewModel.SearchText = "notepad";

        // Assert
        Assert.Single(_viewModel.FilteredLogs);
        Assert.Contains(_viewModel.FilteredLogs, l => l.TaskDescription == "Open notepad");
    }

    [Fact]
    public void FilterLogs_WithStatusFilter_FiltersCorrectly()
    {
        // Arrange
        var logs = new List<ExecutionLog>
        {
            new ExecutionLog { Id = 1, TaskDescription = "Task 1", Status = "Success" },
            new ExecutionLog { Id = 2, TaskDescription = "Task 2", Status = "Failed" },
            new ExecutionLog { Id = 3, TaskDescription = "Task 3", Status = "Success" }
        };

        _repositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(logs);

        _viewModel.RefreshCommand.Execute(null);

        // Act
        _viewModel.StatusFilter = "Success";

        // Assert
        Assert.Equal(2, _viewModel.FilteredLogs.Count);
        Assert.All(_viewModel.FilteredLogs, l => Assert.Equal("Success", l.Status));
    }

    [Fact]
    public void FilterLogs_WithDateRange_FiltersCorrectly()
    {
        // Arrange
        var now = DateTime.Now;
        var logs = new List<ExecutionLog>
        {
            new ExecutionLog { Id = 1, TaskDescription = "Task 1", Timestamp = now.AddDays(-1) },
            new ExecutionLog { Id = 2, TaskDescription = "Task 2", Timestamp = now.AddDays(-10) },
            new ExecutionLog { Id = 3, TaskDescription = "Task 3", Timestamp = now.AddDays(-5) }
        };

        _repositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(logs);

        _viewModel.RefreshCommand.Execute(null);

        // Act
        _viewModel.StartDate = now.AddDays(-7);
        _viewModel.EndDate = now;

        // Assert
        Assert.Equal(2, _viewModel.FilteredLogs.Count);
    }

    [Fact]
    public void TotalLogsCount_ReturnsCorrectCount()
    {
        // Arrange
        var logs = new List<ExecutionLog>
        {
            new ExecutionLog { Id = 1, TaskDescription = "Task 1" },
            new ExecutionLog { Id = 2, TaskDescription = "Task 2" }
        };

        _repositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(logs);

        // Act
        _viewModel.RefreshCommand.Execute(null);

        // Assert
        Assert.Equal(2, _viewModel.TotalLogsCount);
    }

    [Fact]
    public void StatusMessage_SetValue_RaisesPropertyChanged()
    {
        // Arrange
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(HistoryViewModel.StatusMessage))
                propertyChangedRaised = true;
        };

        // Act
        _viewModel.StatusMessage = "履歴を読み込みました";

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.Equal("履歴を読み込みました", _viewModel.StatusMessage);
    }
}
