using System.Windows;
using System.Windows.Controls;

namespace Commanda;

/// <summary>
/// Markdownテキストを表示するコントロール
/// </summary>
public class MarkdownTextBlock : TextBlock
{
    /// <summary>
    /// MarkdownTextプロパティ
    /// </summary>
    public static readonly DependencyProperty MarkdownTextProperty =
        DependencyProperty.Register(
            "MarkdownText",
            typeof(string),
            typeof(MarkdownTextBlock),
            new PropertyMetadata(string.Empty, OnTextChanged));

    /// <summary>
    /// 表示するMarkdownテキスト
    /// </summary>
    public string MarkdownText
    {
        get => (string)GetValue(MarkdownTextProperty);
        set => SetValue(MarkdownTextProperty, value);
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (MarkdownTextBlock)d;
        var text = (string)e.NewValue;

        // 基本的なMarkdown変換（簡易版）
        var formattedText = ConvertMarkdownToText(text);
        control.SetValue(TextBlock.TextProperty, formattedText);
    }

    private static string ConvertMarkdownToText(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return string.Empty;

        // 簡易的なMarkdown変換
        var text = markdown;

        // **bold** を削除（太字表示はサポートしない）
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*\*(.*?)\*\*", "$1");

        // *italic* を削除（斜体表示はサポートしない）
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*(.*?)\*", "$1");

        // リンク [text](url) を text のみに
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\[([^\]]+)\]\([^\)]+\)", "$1");

        // ```code``` を削除
        text = System.Text.RegularExpressions.Regex.Replace(text, @"```([^`]+)```", "$1");

        // `code` を削除
        text = System.Text.RegularExpressions.Regex.Replace(text, @"`([^`]+)`", "$1");

        return text;
    }
}