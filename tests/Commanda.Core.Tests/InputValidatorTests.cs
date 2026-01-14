using NUnit.Framework;
using Commanda.Core;

namespace Commanda.Core.Tests;

[TestFixture]
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
        Assert.IsTrue(result.IsValid);
        Assert.IsNull(result.ErrorMessage);
    }

    [Fact]
    public void ValidateUserInput_EmptyInput_ReturnsInvalid()
    {
        // Arrange
        var input = "";

        // Act
        var result = _validator.ValidateUserInput(input);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("入力が空です", result.ErrorMessage);
    }

    [Fact]
    public void ValidateUserInput_DangerousCommand_ReturnsInvalid()
    {
        // Arrange
        var input = "Please delete all files";

        // Act
        var result = _validator.ValidateUserInput(input);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("危険なコマンドが含まれています", result.ErrorMessage);
    }

    [Fact]
    public void ValidateUserInput_TooLongInput_ReturnsInvalid()
    {
        // Arrange
        var input = new string('a', 10001);

        // Act
        var result = _validator.ValidateUserInput(input);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("入力が長すぎます（最大10000文字）", result.ErrorMessage);
    }

    [Fact]
    public void ValidateUserInput_SqlInjectionPattern_ReturnsWarning()
    {
        // Arrange
        var input = "SELECT * FROM users WHERE id = 1";

        // Act
        var result = _validator.ValidateUserInput(input);

        // Assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(1, result.Warnings.Count());
        Assert.AreEqual("SQLインジェクションの疑いがあります", result.Warnings[0]);
    }

    [Fact]
    public void ValidateFilePath_ValidPath_ReturnsValid()
    {
        // Arrange
        var path = "C:\\temp\\Fact.txt";

        // Act
        var result = _validator.ValidateFilePath(path);

        // Assert
        Assert.IsTrue(result.IsValid);
        Assert.IsNull(result.ErrorMessage);
    }

    [Fact]
    public void ValidateFilePath_PathTraversal_ReturnsInvalid()
    {
        // Arrange
        var path = "../../../windows/system32/cmd.exe";

        // Act
        var result = _validator.ValidateFilePath(path);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("パストラバーサル攻撃の可能性があります", result.ErrorMessage);
    }

    [Fact]
    public void ValidateFilePath_SystemPath_ReturnsInvalid()
    {
        // Arrange
        var path = "C:\\Windows\\System32\\cmd.exe";

        // Act
        var result = _validator.ValidateFilePath(path);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("危険なファイルパスです", result.ErrorMessage);
    }
}
