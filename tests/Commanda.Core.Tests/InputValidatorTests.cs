using Xunit;
using Commanda.Core;

namespace Commanda.Core.Tests;

public class InputValidatorTests
{
    private readonly InputValidator _validator;

    public InputValidatorTests()
    {
        _validator = new InputValidator();
    }

    [Fact]
    public void ValidateUserInput_ValidInput_ReturnsValid()
    {
        // Arrange
        var input = "Hello, create a test file";

        // Act
        var result = _validator.ValidateUserInput(input);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ValidateUserInput_EmptyInput_ReturnsInvalid()
    {
        // Arrange
        var input = "";

        // Act
        var result = _validator.ValidateUserInput(input);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("入力が空です", result.ErrorMessage);
    }

    [Fact]
    public void ValidateUserInput_DangerousCommand_ReturnsInvalid()
    {
        // Arrange
        var input = "Please delete all files";

        // Act
        var result = _validator.ValidateUserInput(input);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("危険なコマンドが含まれています", result.ErrorMessage);
    }

    [Fact]
    public void ValidateUserInput_TooLongInput_ReturnsInvalid()
    {
        // Arrange
        var input = new string('a', 10001);

        // Act
        var result = _validator.ValidateUserInput(input);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("入力が長すぎます（最大10000文字）", result.ErrorMessage);
    }

    [Fact]
    public void ValidateUserInput_SqlInjectionPattern_ReturnsWarning()
    {
        // Arrange
        var input = "SELECT * FROM users WHERE id = 1";

        // Act
        var result = _validator.ValidateUserInput(input);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(1, result.Warnings.Count());
        Assert.Equal("SQLインジェクションの疑いがあります", result.Warnings[0]);
    }

    [Fact]
    public void ValidateFilePath_ValidPath_ReturnsValid()
    {
        // Arrange
        var path = "C:\\temp\\Fact.txt";

        // Act
        var result = _validator.ValidateFilePath(path);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ValidateFilePath_PathTraversal_ReturnsInvalid()
    {
        // Arrange
        var path = "../../../windows/system32/cmd.exe";

        // Act
        var result = _validator.ValidateFilePath(path);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("パストラバーサル攻撃の可能性があります", result.ErrorMessage);
    }

    [Fact]
    public void ValidateFilePath_SystemPath_ReturnsInvalid()
    {
        // Arrange
        var path = "C:\\Windows\\System32\\cmd.exe";

        // Act
        var result = _validator.ValidateFilePath(path);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("危険なファイルパスです", result.ErrorMessage);
    }
}
