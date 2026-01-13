# AGENTS.md - Commanda プロジェクト固有ルール

このドキュメントは、Commandaプロジェクトの開発におけるプロジェクト固有のルールとガイドラインを定義します。グローバルルール（ルートディレクトリのAGENTS.md）は全て適用されます。

## プロジェクト概要

Commandaは、LLMとMCPサーバーを組み合わせたデスクトップAIエージェントです。安全性を最優先としつつ、高い拡張性とユーザビリティを実現します。

## 開発原則

### 1. 安全性第一
- **ローカル実行原則**: LLM解析のみクラウド、ローカル操作は厳格に制御
- **最小権限**: 拡張機能は必要な最小限の権限のみ付与
- **入力検証**: 全てのユーザー入力と外部入力を厳格に検証

### 2. 拡張性重視
- **プラグインアーキテクチャ**: MEFによる動的拡張
- **標準プロトコル**: MCP (Model Context Protocol) 準拠
- **API安定性**: 破壊的変更は慎重に検討

### 3. ユーザビリティ優先
- **直感的なUI**: チャット形式の自然なインターフェース
- **フィードバック即時性**: 操作結果のリアルタイム表示
- **エラー回復性**: わかりやすいエラーメッセージと回復手段

## コーディング標準

### 言語別ルール

#### C#
- **命名規則**:
  - クラス/インターフェース: PascalCase
  - メソッド/プロパティ: PascalCase
  - フィールド: _camelCase (private), PascalCase (public)
  - パラメータ: camelCase
  - ローカル変数: camelCase

- **ファイル構成**:
  ```csharp
  // ファイル先頭
  using System;
  using System.Linq;

  namespace Commanda.Core
  {
      // クラス定義
      public class ExampleClass
      {
          // フィールド
          private readonly ILogger<ExampleClass> _logger;

          // コンストラクタ
          public ExampleClass(ILogger<ExampleClass> logger)
          {
              _logger = logger ?? throw new ArgumentNullException(nameof(logger));
          }

          // メソッド
          public async Task<string> ProcessAsync(string input)
          {
              // 実装
              return await Task.FromResult(input.ToUpper());
          }
      }
  }
  ```

#### XAML
- **命名規則**: PascalCase for x:Name, camelCase for properties
- **構造化**: 論理的なグループ化と適切なインデント
- **リソース**: 再利用可能なスタイルとテンプレートの使用

### アーキテクチャルパターン

#### MVVMパターン
```csharp
// ViewModelの例
public class MainViewModel : INotifyPropertyChanged
{
    public string CurrentMessage { get; set; }
    public ObservableCollection<MessageViewModel> Messages { get; }

    public DelegateCommand SendCommand { get; }

    public MainViewModel()
    {
        Messages = new ObservableCollection<MessageViewModel>();
        SendCommand = new DelegateCommand(SendMessage, CanSendMessage);
    }

    private void SendMessage()
    {
        // 実装
    }

    private bool CanSendMessage()
    {
        return !string.IsNullOrWhiteSpace(CurrentMessage);
    }
}
```

#### 依存性注入
- **Constructor Injection**: 必須パラメータ
- **Property Injection**: オプション依存関係
- **Service Locator**: 避ける（テストしにくいため）

#### 非同期プログラミング
- **async/await**: 全てのI/O操作で使用
- **ConfigureAwait(false)**: ライブラリコードで使用
- **CancellationToken**: キャンセル可能な操作で使用

### セキュリティコーディング

#### APIキー管理
```csharp
public class SecureStorage
{
    // Windows Data Protection API使用
    public async Task StoreApiKeyAsync(string key, string value)
    {
        var encrypted = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(value),
            null,
            DataProtectionScope.CurrentUser);

        await File.WriteAllTextAsync(key, Convert.ToBase64String(encrypted));
    }

    public async Task<string> RetrieveApiKeyAsync(string key)
    {
        var encrypted = Convert.FromBase64String(await File.ReadAllTextAsync(key));
        var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(decrypted);
    }
}
```

#### 入力検証
```csharp
public class InputValidator
{
    public ValidationResult ValidateUserInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return ValidationResult.Invalid("入力が空です");

        if (input.Length > 1000)
            return ValidationResult.Invalid("入力が長すぎます");

        // 危険なパターンのチェック
        if (ContainsDangerousPatterns(input))
            return ValidationResult.Invalid("危険なコマンドが含まれています");

        return ValidationResult.Valid();
    }
}
```

## プロジェクト構造

```
commanda/
├── src/
│   ├── Commanda/               # メインアプリケーション (WPF)
│   ├── Commanda.Core/          # コアロジック
│   ├── Commanda.Mcp/           # MCPサーバー実装
│   └── Commanda.Extensions/    # 拡張機能基盤
├── tests/
│   ├── Commanda.UnitTests/     # ユニットテスト
│   └── Commanda.IntegrationTests/ # 統合テスト
├── docs/                       # ドキュメント
│   ├── ADR/                   # アーキテクチャ決定記録
│   ├── Basic-Design-Document.md
│   ├── Detailed-Design-Document.md
│   ├── coding-guidelines.md
│   └── best-practices.md
├── tools/                      # ビルド・デプロイスクリプト
└── samples/                    # サンプル拡張機能
```

## ブランチ戦略

### ブランチ命名規則
- `main`: リリース可能な安定版
- `develop`: 開発統合ブランチ
- `feature/xxx`: 新機能開発
- `bugfix/xxx`: バグ修正
- `hotfix/xxx`: 緊急修正
- `release/x.x.x`: リリース準備

### コミットメッセージ
```
type(scope): description

[optional body]

[optional footer]
```

**Type:**
- `feat`: 新機能
- `fix`: バグ修正
- `docs`: ドキュメント
- `style`: スタイル修正
- `refactor`: リファクタリング
- `test`: テスト
- `chore`: その他

**例:**
```
feat(auth): add EntraID authentication support

- Implement OAuth 2.0 flow
- Add token caching
- Update UI for login process

Closes #123
```

## テスト戦略

### テスト分類
- **Unit Tests**: 各メソッドの機能テスト
- **Integration Tests**: コンポーネント間連携テスト
- **UI Tests**: WPF UI動作テスト
- **End-to-End Tests**: 完全なユーザーシナリオテスト

### カバレッジ目標
- **Unit Tests**: 80%以上
- **Integration Tests**: 主要パスカバー
- **UI Tests**: 主要UI操作カバー

### テスト命名規則
```csharp
[TestFixture]
public class AgentOrchestratorTests
{
    [Test]
    public async Task ExecuteTaskAsync_ValidInput_ReturnsSuccess()
    {
        // Arrange, Act, Assert
    }

    [Test]
    public async Task ExecuteTaskAsync_InvalidInput_ThrowsException()
    {
        // Arrange, Act, Assert
    }
}
```

## コードレビュー基準

### 必須チェック項目
- [ ] セキュリティ脆弱性の有無
- [ ] 入力検証の実装
- [ ] エラーハンドリングの適切さ
- [ ] テストカバレッジ
- [ ] パフォーマンスへの影響
- [ ] アーキテクチャ準拠

### 推奨事項
- [ ] コードの可読性と保守性
- [ ] 適切なコメントとドキュメント
- [ ] 命名規則の遵守
- [ ] デザインパターンの適切な使用

## CI/CD

### ビルドパイプライン
- **トリガー**: PR作成時、main/developプッシュ時
- **ビルド**: .NET 8.0, Windows x64
- **テスト**: 全テストスイート実行
- **分析**: CodeQLセキュリティスキャン
- **パッケージング**: 自己完結型実行ファイル

### リリースプロセス
1. **開発完了**: developブランチで機能統合
2. **リリースブランチ**: release/x.x.x作成
3. **QAテスト**: 統合テスト実行
4. **ドキュメント更新**: READMEとCHANGELOG更新
5. **タグ付け**: git tag v1.0.0
6. **GitHub Release**: バイナリ添付

## 拡張機能開発ガイドライン

### 拡張機能構造
```csharp
[Export(typeof(IMcpExtension))]
public class MyExtension : IMcpExtension
{
    public string Name => "My Custom Extension";
    public string Version => "1.0.0";

    public IEnumerable<McpServerToolType> ToolTypes => new[]
    {
        typeof(MyTools)
    };

    public Task InitializeAsync(IServiceProvider services)
    {
        // 初期化処理
        return Task.CompletedTask;
    }
}

[McpServerToolType]
public static class MyTools
{
    [McpServerTool, Description("カスタムツールの説明")]
    public static string MyTool(string parameter)
    {
        // ツール実装
        return $"Result: {parameter}";
    }
}
```

### 拡張機能パッケージング
- **NuGetパッケージ**: 拡張機能をNuGetとして配布
- **依存関係**: 最小限の依存関係のみ
- **ドキュメント**: READMEと使用例の提供
- **バージョン管理**: SemVer準拠

## 監視とログ

### ログレベル
- **Trace**: 詳細なデバッグ情報
- **Debug**: 開発時デバッグ情報
- **Information**: 一般的な運用情報
- **Warning**: 潜在的な問題
- **Error**: エラー状態
- **Critical**: 即時対応が必要な重大エラー

### ログ構造化
```csharp
_logger.LogInformation(
    "Task execution completed {TaskId} in {Duration}ms with result {Result}",
    taskId, duration, result);
```

### メトリクス収集
- **実行時間**: 各操作の所要時間
- **成功率**: 操作の成功/失敗率
- **リソース使用量**: CPU/メモリ使用率
- **エラー率**: 各種エラーの発生頻度

## コミュニティと貢献

### 貢献者ガイドライン
1. **Issue作成**: 明確な問題記述と再現手順
2. **PR作成**: 詳細な説明とテスト
3. **ドキュメント**: 変更に伴うドキュメント更新
4. **テスト**: 新機能のテスト追加

### コミュニティ標準
- **敬意あるコミュニケーション**: 全ての貢献者に対して敬意を払う
- **建設的なフィードバック**: 改善のための提案
- **多様性の尊重**: 様々な背景の貢献者を歓迎

## 更新履歴

- **2024-01-XX**: 初回作成
- **継続更新**: プロジェクトの進化に伴い更新

---

このドキュメントはプロジェクトの成長とともに更新されます。疑問点があれば[Issues](https://github.com/your-org/commanda/issues)で質問してください。