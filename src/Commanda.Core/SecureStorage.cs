using System.Security.Cryptography;
using System.Text;

namespace Commanda.Core;

/// <summary>
/// 安全なストレージクラス（APIキーなどの機密情報を管理）
/// </summary>
public class SecureStorage
{
    private readonly string _storagePath;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="storagePath">ストレージファイルのパス（オプション）</param>
    public SecureStorage(string? storagePath = null)
    {
        _storagePath = storagePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Commanda",
            "secure_storage.dat");
    }

    /// <summary>
    /// APIキーを安全に保存します
    /// </summary>
    /// <param name="key">キー名</param>
    /// <param name="value">保存する値</param>
    /// <returns>保存処理のタスク</returns>
    public async Task StoreApiKeyAsync(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("キーは空にできません", nameof(key));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("値は空にできません", nameof(value));
        }

        try
        {
            // 値を暗号化
            var encryptedValue = ProtectData(value);

            // ストレージディレクトリを作成
            var directory = Path.GetDirectoryName(_storagePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 既存のデータを読み込み
            var storage = await LoadStorageAsync();

            // 新しいデータを追加/更新
            storage[key] = encryptedValue;

            // 保存
            await SaveStorageAsync(storage);
        }
        catch (Exception ex)
        {
            throw new SecureStorageException($"APIキーの保存に失敗しました: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// APIキーを安全に取得します
    /// </summary>
    /// <param name="key">キー名</param>
    /// <returns>取得した値（見つからない場合はnull）</returns>
    public async Task<string?> RetrieveApiKeyAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("キーは空にできません", nameof(key));
        }

        try
        {
            var storage = await LoadStorageAsync();

            if (storage.TryGetValue(key, out var encryptedValue))
            {
                return UnprotectData(encryptedValue);
            }

            return null;
        }
        catch (Exception ex)
        {
            throw new SecureStorageException($"APIキーの取得に失敗しました: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// APIキーを削除します
    /// </summary>
    /// <param name="key">キー名</param>
    /// <returns>削除が成功したかどうか</returns>
    public async Task<bool> DeleteApiKeyAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("キーは空にできません", nameof(key));
        }

        try
        {
            var storage = await LoadStorageAsync();

            if (storage.Remove(key))
            {
                await SaveStorageAsync(storage);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            throw new SecureStorageException($"APIキーの削除に失敗しました: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 保存されているすべてのキー名を取得します
    /// </summary>
    /// <returns>キー名のリスト</returns>
    public async Task<List<string>> GetStoredKeysAsync()
    {
        try
        {
            var storage = await LoadStorageAsync();
            return storage.Keys.ToList();
        }
        catch (Exception ex)
        {
            throw new SecureStorageException($"キーの取得に失敗しました: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// ストレージをクリアします
    /// </summary>
    /// <returns>クリア処理のタスク</returns>
    public async Task ClearStorageAsync()
    {
        try
        {
            if (File.Exists(_storagePath))
            {
                await Task.Run(() => File.Delete(_storagePath));
            }
        }
        catch (Exception ex)
        {
            throw new SecureStorageException($"ストレージのクリアに失敗しました: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// データを暗号化します
    /// </summary>
    /// <param name="data">暗号化するデータ</param>
    /// <returns>暗号化されたデータ</returns>
    private string ProtectData(string data)
    {
        // 簡易的な暗号化実装（本番環境ではWindows Data Protection APIを使用）
        var bytes = Encoding.UTF8.GetBytes(data);
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)(bytes[i] ^ 0xAA); // 単純なXOR暗号化
        }
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// データを復号化します
    /// </summary>
    /// <param name="protectedData">暗号化されたデータ</param>
    /// <returns>復号化されたデータ</returns>
    private string UnprotectData(string protectedData)
    {
        // 簡易的な復号化実装（本番環境ではWindows Data Protection APIを使用）
        var bytes = Convert.FromBase64String(protectedData);
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)(bytes[i] ^ 0xAA); // 単純なXOR復号化
        }
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// ストレージファイルを読み込みます
    /// </summary>
    /// <returns>ストレージデータ</returns>
    private async Task<Dictionary<string, string>> LoadStorageAsync()
    {
        if (!File.Exists(_storagePath))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_storagePath);
            var storage = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return storage ?? new Dictionary<string, string>();
        }
        catch
        {
            // ファイルが破損している場合は空のストレージを返す
            return new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// ストレージファイルを保存します
    /// </summary>
    /// <param name="storage">ストレージデータ</param>
    /// <returns>保存処理のタスク</returns>
    private async Task SaveStorageAsync(Dictionary<string, string> storage)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(storage, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = false
        });

        // テンポラリファイルに書き込んでから移動（原子性確保）
        var tempPath = _storagePath + ".tmp";
        await File.WriteAllTextAsync(tempPath, json);

        if (File.Exists(_storagePath))
        {
            File.Replace(tempPath, _storagePath, null);
        }
        else
        {
            File.Move(tempPath, _storagePath);
        }
    }
}

/// <summary>
/// 安全なストレージの例外クラス
/// </summary>
public class SecureStorageException : Exception
{
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="message">エラーメッセージ</param>
    public SecureStorageException(string message) : base(message)
    {
    }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="message">エラーメッセージ</param>
    /// <param name="innerException">内部例外</param>
    public SecureStorageException(string message, Exception innerException) : base(message, innerException)
    {
    }
}