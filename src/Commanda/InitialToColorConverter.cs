using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Commanda;

/// <summary>
/// イニシャルを色に変換するコンバーター
/// </summary>
public class InitialToColorConverter : IValueConverter
{
    private static readonly Color[] Colors = new[]
    {
        Color.FromRgb(100, 149, 237), // CornflowerBlue
        Color.FromRgb(60, 179, 113),   // MediumSeaGreen
        Color.FromRgb(255, 165, 0),    // Orange
        Color.FromRgb(147, 112, 219),  // MediumPurple
        Color.FromRgb(255, 99, 71),    // Tomato
        Color.FromRgb(70, 130, 180),   // SteelBlue
        Color.FromRgb(107, 142, 35),   // OliveDrab
        Color.FromRgb(112, 128, 144)   // SlateGray
    };

    /// <summary>
    /// イニシャルを色に変換します
    /// </summary>
    /// <param name="value">イニシャル</param>
    /// <param name="targetType">ターゲットタイプ</param>
    /// <param name="parameter">パラメータ</param>
    /// <param name="culture">カルチャ</param>
    /// <returns>色</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string initial && !string.IsNullOrEmpty(initial))
        {
            var firstChar = initial[0];
            var colorIndex = Math.Abs(firstChar) % Colors.Length;
            return new SolidColorBrush(Colors[colorIndex]);
        }

        return new SolidColorBrush(Colors[0]);
    }

    /// <summary>
    /// 逆変換（未実装）
    /// </summary>
    /// <param name="value">値</param>
    /// <param name="targetType">ターゲットタイプ</param>
    /// <param name="parameter">パラメータ</param>
    /// <param name="culture">カルチャ</param>
    /// <returns>未実装</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}