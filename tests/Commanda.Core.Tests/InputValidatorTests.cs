using NUnit.Framework;
using Commanda.Core;

namespace Commanda.Core.Tests;

[TestFixture]
public class InputValidatorTests
{
    private InputValidator _validator = null!;

    [SetUp]
    public void Setup()
    {
        _validator = new InputValidator();
    }

    [Test]
    public void ValidateUserInput_ValidInput_ReturnsValid()
    {
        // Arrange
        var input = "Hello, create a test file";

        // Act
        var result = _validator.ValidateUserInput(input);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void ValidateUserInput_EmptyInput_ReturnsInvalid()
    {
        // Arrange
        var input = "";

        // Act
        var result = _validator.ValidateUserInput(input);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("入力が空です"));
    }

    [Test]
    public void ValidateUserInput_DangerousCommand_ReturnsInvalid()
    {
        // Arrange
        var input = "Please delete all files";

        // Act
        var result = _validator.ValidateUserInput(input);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("危険なコマンドが含まれています"));
    }

    [Test]
    public void ValidateUserInput_TooLongInput_ReturnsInvalid()
    {
        // Arrange
        var input = new string('a', 10001);

        // Act
        var result = _validator.ValidateUserInput(input);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("入力が長すぎます（最大10000文字）"));
    }

    [Test]
    public void ValidateUserInput_SqlInjectionPattern_ReturnsWarning()
    {
        // Arrange
        var input = "SELECT * FROM users WHERE id = 1";

        // Act
        var result = _validator.ValidateUserInput(input);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Warnings, Has.Count.EqualTo(1));
        Assert.That(result.Warnings[0], Is.EqualTo("SQLインジェクションの疑いがあります"));
    }

    [Test]
    public void ValidateFilePath_ValidPath_ReturnsValid()
    {
        // Arrange
        var path = "C:\\temp\\test.txt";

        // Act
        var result = _validator.ValidateFilePath(path);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void ValidateFilePath_PathTraversal_ReturnsInvalid()
    {
        // Arrange
        var path = "../../../windows/system32/cmd.exe";

        // Act
        var result = _validator.ValidateFilePath(path);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("パストラバーサル攻撃の可能性があります"));
    }

    [Test]
    public void ValidateFilePath_SystemPath_ReturnsInvalid()
    {
        // Arrange
        var path = "C:\\Windows\\System32\\cmd.exe";

        // Act
        var result = _validator.ValidateFilePath(path);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("危険なファイルパスです"));
    }
}
