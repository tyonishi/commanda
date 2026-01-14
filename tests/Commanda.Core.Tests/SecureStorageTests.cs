using Xunit;
using Commanda.Core;

namespace Commanda.Core.Tests;

public class SecureStorageTests : IDisposable
{
    private readonly SecureStorage _secureStorage;
    private readonly string _testStoragePath;

    public SecureStorageTests()
    {
        _testStoragePath = Path.Combine(Path.GetTempPath(), "CommandaTest", $"secure_storage_test_{Guid.NewGuid()}.dat");
        _secureStorage = new SecureStorage(_testStoragePath);
    }

    public void Dispose()
    {
        // テスト後にストレージをクリア
        try
        {
            if (File.Exists(_testStoragePath))
            {
                File.Delete(_testStoragePath);
            }

            // ディレクトリも削除
            var directory = Path.GetDirectoryName(_testStoragePath);
            if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
            {
                Directory.Delete(directory);
            }
        }
        catch
        {
            // クリーンアップエラーは無視
        }
    }

    [Fact]
    public async Task StoreApiKeyAsync_ValidKeyAndValue_StoresEncryptedData()
    {
        // Arrange
        const string key = "test_api_key";
        const string value = "sk-test123456789";

        // Act
        await _secureStorage.StoreApiKeyAsync(key, value);

        // Assert
        var retrieved = await _secureStorage.RetrieveApiKeyAsync(key);
        Assert.Equal(value, retrieved);
    }

    [Fact]
    public async Task RetrieveApiKeyAsync_NonExistentKey_ReturnsNull()
    {
        // Act
        var result = await _secureStorage.RetrieveApiKeyAsync("nonexistent_key");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task StoreApiKeyAsync_EmptyKey_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _secureStorage.StoreApiKeyAsync("", "value"));
        Assert.Equal("key", exception.ParamName);
    }

    [Fact]
    public async Task StoreApiKeyAsync_EmptyValue_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _secureStorage.StoreApiKeyAsync("key", ""));
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public async Task StoreAndRetrieveApiKeyAsync_SpecialCharacters_PreservesData()
    {
        // Arrange
        const string key = "special_key";
        const string value = "sk-123!@#$%^&*()_+{}|:<>?[]\\;',./";

        // Act
        await _secureStorage.StoreApiKeyAsync(key, value);
        var retrieved = await _secureStorage.RetrieveApiKeyAsync(key);

        // Assert
        Assert.Equal(value, retrieved);
    }

    [Fact]
    public async Task UpdateApiKeyAsync_ExistingKey_UpdatesValue()
    {
        // Arrange
        const string key = "update_key";
        const string originalValue = "original_value";
        const string updatedValue = "updated_value";

        // Act
        await _secureStorage.StoreApiKeyAsync(key, originalValue);
        await _secureStorage.StoreApiKeyAsync(key, updatedValue);
        var retrieved = await _secureStorage.RetrieveApiKeyAsync(key);

        // Assert
        Assert.Equal(updatedValue, retrieved);
    }

    [Fact]
    public async Task DeleteApiKeyAsync_ExistingKey_ReturnsTrue()
    {
        // Arrange
        const string key = "delete_key";
        const string value = "delete_value";
        await _secureStorage.StoreApiKeyAsync(key, value);

        // Act
        var result = await _secureStorage.DeleteApiKeyAsync(key);

        // Assert
        Assert.True(result);
        var retrieved = await _secureStorage.RetrieveApiKeyAsync(key);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task DeleteApiKeyAsync_NonExistentKey_ReturnsFalse()
    {
        // Act
        var result = await _secureStorage.DeleteApiKeyAsync("nonexistent_key");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetStoredKeysAsync_NoKeys_ReturnsEmptyList()
    {
        // Act
        var keys = await _secureStorage.GetStoredKeysAsync();

        // Assert
        Assert.Empty(keys);
    }

    [Fact]
    public async Task GetStoredKeysAsync_HasKeys_ReturnsKeyList()
    {
        // Arrange
        await _secureStorage.StoreApiKeyAsync("key1", "value1");
        await _secureStorage.StoreApiKeyAsync("key2", "value2");

        // Act
        var keys = await _secureStorage.GetStoredKeysAsync();

        // Assert
        Assert.Equal(2, keys.Count);
        Assert.Contains("key1", keys);
        Assert.Contains("key2", keys);
    }

    [Fact]
    public async Task ClearStorageAsync_RemovesAllData()
    {
        // Arrange
        await _secureStorage.StoreApiKeyAsync("key1", "value1");
        await _secureStorage.StoreApiKeyAsync("key2", "value2");

        // Act
        await _secureStorage.ClearStorageAsync();

        // Assert
        var keys = await _secureStorage.GetStoredKeysAsync();
        Assert.Empty(keys);
    }

    [Fact]
    public async Task EncryptionIsProperlyApplied_DataCannotBeReadWithoutDecryption()
    {
        // Arrange
        const string key = "encryption_test";
        const string value = "secret_data_123";

        // Act
        await _secureStorage.StoreApiKeyAsync(key, value);

        // 暗号化されたデータを直接読み取り
        var json = await File.ReadAllTextAsync(_testStoragePath);
        var storage = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        var encryptedValue = storage![key];

        // Assert
        // 暗号化されたデータは元の値と異なるはず
        Assert.NotEqual(value, encryptedValue);
        // しかし、SecureStorage経由では正しく復号化される
        var retrieved = await _secureStorage.RetrieveApiKeyAsync(key);
        Assert.Equal(value, retrieved);
    }
}