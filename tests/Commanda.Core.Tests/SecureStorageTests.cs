using NUnit.Framework;
using Commanda.Core;

namespace Commanda.Core.Tests;

[TestFixture]
public class SecureStorageTests
{
    private SecureStorage _secureStorage = null!;
    private string _testStoragePath = null!;

    [SetUp]
    public void Setup()
    {
        _testStoragePath = Path.Combine(Path.GetTempPath(), "CommandaTest", "secure_storage_test.dat");
        _secureStorage = new SecureStorage(_testStoragePath);

        // テスト前にストレージをクリア
        if (File.Exists(_testStoragePath))
        {
            File.Delete(_testStoragePath);
        }
    }

    [TearDown]
    public void TearDown()
    {
        // テスト後にストレージをクリア
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

    [Test]
    public async Task StoreApiKeyAsync_ValidKeyAndValue_StoresEncryptedData()
    {
        // Arrange
        const string key = "test_api_key";
        const string value = "sk-test123456789";

        // Act
        await _secureStorage.StoreApiKeyAsync(key, value);

        // Assert
        var retrieved = await _secureStorage.RetrieveApiKeyAsync(key);
        Assert.AreEqual(value, retrieved);
    }

    [Test]
    public async Task RetrieveApiKeyAsync_NonExistentKey_ReturnsNull()
    {
        // Act
        var result = await _secureStorage.RetrieveApiKeyAsync("nonexistent_key");

        // Assert
        Assert.IsNull(result);
    }

    [Test]
    public async Task StoreApiKeyAsync_EmptyKey_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentException>(
            async () => await _secureStorage.StoreApiKeyAsync("", "value"))!;
        Assert.AreEqual("key", exception.ParamName);
    }

    [Test]
    public async Task StoreApiKeyAsync_EmptyValue_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentException>(
            async () => await _secureStorage.StoreApiKeyAsync("key", ""))!;
        Assert.AreEqual("value", exception.ParamName);
    }

    [Test]
    public async Task StoreAndRetrieveApiKeyAsync_SpecialCharacters_PreservesData()
    {
        // Arrange
        const string key = "special_key";
        const string value = "sk-123!@#$%^&*()_+{}|:<>?[]\\;',./";

        // Act
        await _secureStorage.StoreApiKeyAsync(key, value);
        var retrieved = await _secureStorage.RetrieveApiKeyAsync(key);

        // Assert
        Assert.AreEqual(value, retrieved);
    }

    [Test]
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
        Assert.AreEqual(updatedValue, retrieved);
    }

    [Test]
    public async Task DeleteApiKeyAsync_ExistingKey_ReturnsTrue()
    {
        // Arrange
        const string key = "delete_key";
        const string value = "delete_value";
        await _secureStorage.StoreApiKeyAsync(key, value);

        // Act
        var result = await _secureStorage.DeleteApiKeyAsync(key);

        // Assert
        Assert.IsTrue(result);
        var retrieved = await _secureStorage.RetrieveApiKeyAsync(key);
        Assert.IsNull(retrieved);
    }

    [Test]
    public async Task DeleteApiKeyAsync_NonExistentKey_ReturnsFalse()
    {
        // Act
        var result = await _secureStorage.DeleteApiKeyAsync("nonexistent_key");

        // Assert
        Assert.IsFalse(result);
    }

    [Test]
    public async Task GetStoredKeysAsync_NoKeys_ReturnsEmptyList()
    {
        // Act
        var keys = await _secureStorage.GetStoredKeysAsync();

        // Assert
        Assert.IsEmpty(keys);
    }

    [Test]
    public async Task GetStoredKeysAsync_HasKeys_ReturnsKeyList()
    {
        // Arrange
        await _secureStorage.StoreApiKeyAsync("key1", "value1");
        await _secureStorage.StoreApiKeyAsync("key2", "value2");

        // Act
        var keys = await _secureStorage.GetStoredKeysAsync();

        // Assert
        Assert.AreEqual(2, keys.Count);
        CollectionAssert.Contains(keys, "key1");
        CollectionAssert.Contains(keys, "key2");
    }

    [Test]
    public async Task ClearStorageAsync_RemovesAllData()
    {
        // Arrange
        await _secureStorage.StoreApiKeyAsync("key1", "value1");
        await _secureStorage.StoreApiKeyAsync("key2", "value2");

        // Act
        await _secureStorage.ClearStorageAsync();

        // Assert
        var keys = await _secureStorage.GetStoredKeysAsync();
        Assert.IsEmpty(keys);
    }

    [Test]
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
        Assert.AreNotEqual(value, encryptedValue);
        // しかし、SecureStorage経由では正しく復号化される
        var retrieved = await _secureStorage.RetrieveApiKeyAsync(key);
        Assert.AreEqual(value, retrieved);
    }
}