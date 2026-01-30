# ADR-016: MCP Core Tools Implementation

## Status
Accepted

## Context
Phase 1の実装として、CommandaのMCPサーバーにコアツールを追加する必要がありました。
基本設計書で定義されていた以下の機能が未実装でした：

- ApplicationControl: アプリケーションの起動・終了・一覧取得
- TextProcessing: テキストファイルの読み書き・検索・置換

これらのツールは、ユーザーの自然言語指示を実行可能な操作に変換するために必須です。

## Decision

### 実装方針
1. **TDD（テスト駆動開発）**: テスト先に作成し、実装はテストを通すことに集中
2. **安全性第一**: すべてのツールで入力検証と危険操作のブロック
3. **段階的実装**: ApplicationControl → TextProcessing → McpServer統合

### 技術的決定

#### ApplicationControl
- **プロセス管理**: `System.Diagnostics.Process` を使用
- **セキュリティ**: 危険なパターンマッチングでコマンドインジェクション防止
- **ブロックリスト**: システムツール（regedit, format等）を明示的にブロック
- **PID管理**: プロセスIDによる終了操作（名前ベースより信頼性が高い）

#### TextProcessing
- **サイズ制限**: 10MBのファイルサイズ制限（メモリ保護）
- **エンコーディング**: UTF-8/UTF-16/Shift-JIS/EUC-JP等に対応
- **バックアップ**: 破壊的操作前に自動バックアップ作成
- **パス保護**: Windows/System32等への書き込みを禁止

### セキュリティ対策

| 脅威 | 対策 |
|------|------|
| コマンドインジェクション | 危険なパターンの正規表現マッチング |
| システムファイル破壊 | ブロックパスリストによる書き込み禁止 |
| メモリ枯渇 | 10MBファイルサイズ制限 |
| 権限昇格 | システムプロセスの保護 |

## Consequences

### Positive
- ✅ 基本操作（ファイル・アプリ制御）が可能に
- ✅ セキュリティ要件を満たす実装
- ✅ 高いテストカバレッジ（36テスト、100%成功）
- ✅ 拡張性のある設計（新ツール追加が容易）

### Negative
- ⚠️ 正規表現パターンマッチングは完全ではない（回避可能）
- ⚠️ 大きなファイルの処理はメモリベース（ストリーミング未実装）
- ⚠️ プロセス起動はWindows依存（クロスプラットフォーム未対応）

## Implementation Details

### テスト戦略
```
ApplicationControlTests: 10テスト
├── LaunchApplicationAsync_ValidPath_ReturnsSuccess
├── LaunchApplicationAsync_InvalidPath_ReturnsError
├── LaunchApplicationAsync_BlockedApplication_ReturnsError
├── LaunchApplicationAsync_MissingPathParameter_ReturnsError
├── CloseApplicationAsync_RunningProcess_ReturnsSuccess
├── CloseApplicationAsync_NonExistentProcess_ReturnsError
├── CloseApplicationAsync_MissingParameters_ReturnsError
├── GetRunningApplicationsAsync_ReturnsList
├── LaunchApplicationAsync_WithWorkingDirectory_ReturnsSuccess
└── LaunchApplicationAsync_CancellationRequested_ThrowsOperationCanceledException

TextProcessingTests: 19テスト
├── ReadTextFileAsync_ValidFile_ReturnsContent
├── ReadTextFileAsync_NonExistentFile_ReturnsError
├── ReadTextFileAsync_LargeFile_ReturnsError
├── ReadTextFileAsync_MissingPathParameter_ReturnsError
├── WriteTextFileAsync_ValidContent_ReturnsSuccess
├── WriteTextFileAsync_DangerousPath_ReturnsError
├── WriteTextFileAsync_MissingParameters_ReturnsError
├── AppendToFileAsync_ValidContent_ReturnsSuccess
├── AppendToFileAsync_NewFile_CreatesFile
├── SearchInFileAsync_ExistingPattern_ReturnsMatches
├── SearchInFileAsync_RegexPattern_ReturnsMatches
├── SearchInFileAsync_NoMatches_ReturnsEmpty
├── ReplaceInFileAsync_ValidReplacement_ReturnsSuccess
├── ReplaceInFileAsync_RegexReplacement_ReturnsSuccess
├── ReplaceInFileAsync_DangerousPath_ReturnsError
├── ReplaceInFileAsync_CreatesBackup_ReturnsSuccess
├── WriteTextFileAsync_WithEncoding_ReturnsSuccess
├── ReadTextFileAsync_WithSpecificEncoding_ReturnsContent
└── SearchInFileAsync_CancellationRequested_ThrowsOperationCanceledException
```

### 統合テスト
McpServerに11の組み込みツールを登録：
- FileOperations: read_file, write_file, list_directory
- ApplicationControl: launch_application, close_application, get_running_applications
- TextProcessing: read_text_file, write_text_file, append_to_file, search_in_file, replace_in_file

## References
- Basic-Design-Document.md Section 3.4
- Detailed-Design-Document.md Section 2.3
- Security Architecture ADR-002

## Date
2026-01-30

## Author
Commanda Development Team
