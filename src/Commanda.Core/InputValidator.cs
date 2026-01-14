using System.Security;
using System.Text.RegularExpressions;

namespace Commanda.Core;

/// <summary>
/// 入力検証の結果を表すクラス
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// 検証が成功したかどうか
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// エラーメッセージ
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 警告メッセージのリスト
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// 有効な結果を作成します
    /// </summary>
    /// <returns>検証結果</returns>
    public static ValidationResult Valid()
    {
        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// 無効な結果を作成します
    /// </summary>
    /// <param name="message">エラーメッセージ</param>
    /// <returns>検証結果</returns>
    public static ValidationResult Invalid(string message)
    {
        return new ValidationResult { IsValid = false, ErrorMessage = message };
    }
}

/// <summary>
/// 入力検証クラス
/// </summary>
public class InputValidator
{
    // 危険なコマンドパターンの正規表現
    private static readonly Regex DangerousPatterns = new Regex(
        @"(?i)(?:\b(?:rm|del|delete|format|shutdown|reboot|halt|poweroff|kill|taskkill|net\s+stop|sc\s+stop|reg\s+delete)\b|(?:\|\s*(?:rm|del|delete|format|shutdown)))",
        RegexOptions.Compiled);

    // 危険なファイルパスパターンの正規表現
    private static readonly Regex DangerousFilePaths = new Regex(
        @"(?i)(?:\.\.|[/\\](?:windows|system32|program\s+files|users|documents\s+and\s+settings|all\s+users))",
        RegexOptions.Compiled);

    // SQLインジェクションのパターン
    private static readonly Regex SqlInjectionPatterns = new Regex(
        @"(?i)(?:\b(?:select|insert|update|delete|drop|create|alter|exec|execute)\b.*)",
        RegexOptions.Compiled);

    /// <summary>
    /// ユーザー入力を検証します
    /// </summary>
    /// <param name="input">検証する入力</param>
    /// <returns>検証結果</returns>
    public ValidationResult ValidateUserInput(string input)
    {
        var result = new ValidationResult();

        // 基本的なチェック
        if (string.IsNullOrWhiteSpace(input))
        {
            return ValidationResult.Invalid("入力が空です");
        }

        if (input.Length > 10000)
        {
            return ValidationResult.Invalid("入力が長すぎます（最大10000文字）");
        }

        // 危険なパターンのチェック
        if (ContainsDangerousPatterns(input))
        {
            return ValidationResult.Invalid("危険なコマンドが含まれています");
        }

        // SQLインジェクションのチェック
        if (ContainsSqlInjectionPatterns(input))
        {
            result.Warnings.Add("SQLインジェクションの疑いがあります");
        }

        // ファイルパスのチェック
        if (ContainsDangerousFilePaths(input))
        {
            result.Warnings.Add("危険なファイルパスが含まれています");
        }

        // 特殊文字のチェック
        if (ContainsExcessiveSpecialCharacters(input))
        {
            result.Warnings.Add("特殊文字が多すぎます");
        }

        result.IsValid = true;
        return result;
    }

    /// <summary>
    /// ファイルパスを検証します
    /// </summary>
    /// <param name="path">検証するパス</param>
    /// <returns>検証結果</returns>
    public ValidationResult ValidateFilePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return ValidationResult.Invalid("ファイルパスが空です");
        }

        if (path.Length > 260) // WindowsのMAX_PATH
        {
            return ValidationResult.Invalid("ファイルパスが長すぎます");
        }

        // パストラバーサルのチェック
        if (path.Contains("..") || path.Contains("\\..") || path.Contains("/.."))
        {
            return ValidationResult.Invalid("パストラバーサル攻撃の可能性があります");
        }

        // 危険なパスのチェック
        if (DangerousFilePaths.IsMatch(path))
        {
            return ValidationResult.Invalid("危険なファイルパスです");
        }

        return ValidationResult.Valid();
    }

    /// <summary>
    /// ツール引数を検証します
    /// </summary>
    /// <param name="toolName">ツール名</param>
    /// <param name="arguments">引数</param>
    /// <returns>検証結果</returns>
    public ValidationResult ValidateToolArguments(string toolName, Dictionary<string, object> arguments)
    {
        var result = new ValidationResult();

        // ツール固有の検証
        switch (toolName.ToLowerInvariant())
        {
            case "read_file":
            case "list_directory":
                if (arguments.TryGetValue("path", out var pathObj1) && pathObj1 is string filePath)
                {
                    var pathValidation = ValidateFilePath(filePath);
                    if (!pathValidation.IsValid)
                    {
                        return pathValidation;
                    }
                }
                else
                {
                    return ValidationResult.Invalid("pathパラメータが必要です");
                }
                break;

            case "write_file":
                if (arguments.TryGetValue("path", out var pathObj2) && pathObj2 is string writePath)
                {
                    var pathValidation = ValidateFilePath(writePath);
                    if (!pathValidation.IsValid)
                    {
                        return pathValidation;
                    }
                }
                else
                {
                    return ValidationResult.Invalid("pathパラメータが必要です");
                }

                if (arguments.TryGetValue("content", out var contentObj) && contentObj is string content)
                {
                    if (content.Length > 1000000) // 1MB制限
                    {
                        return ValidationResult.Invalid("コンテンツが大きすぎます（最大1MB）");
                    }
                }
                else
                {
                    return ValidationResult.Invalid("contentパラメータが必要です");
                }
                break;
        }

        result.IsValid = true;
        return result;
    }

    /// <summary>
    /// 危険なパターンが含まれているかをチェックします
    /// </summary>
    /// <param name="input">チェックする入力</param>
    /// <returns>危険なパターンが含まれているかどうか</returns>
    private bool ContainsDangerousPatterns(string input)
    {
        return DangerousPatterns.IsMatch(input);
    }

    /// <summary>
    /// SQLインジェクションのパターンが含まれているかをチェックします
    /// </summary>
    /// <param name="input">チェックする入力</param>
    /// <returns>SQLインジェクションのパターンが含まれているかどうか</returns>
    private bool ContainsSqlInjectionPatterns(string input)
    {
        return SqlInjectionPatterns.IsMatch(input);
    }

    /// <summary>
    /// 危険なファイルパスが含まれているかをチェックします
    /// </summary>
    /// <param name="input">チェックする入力</param>
    /// <returns>危険なファイルパスが含まれているかどうか</returns>
    private bool ContainsDangerousFilePaths(string input)
    {
        return DangerousFilePaths.IsMatch(input);
    }

    /// <summary>
    /// 特殊文字が多すぎるかをチェックします
    /// </summary>
    /// <param name="input">チェックする入力</param>
    /// <returns>特殊文字が多すぎるかどうか</returns>
    private bool ContainsExcessiveSpecialCharacters(string input)
    {
        var specialCharCount = input.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));
        return specialCharCount > input.Length * 0.3; // 30%以上の特殊文字
    }
}