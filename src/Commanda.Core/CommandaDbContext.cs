using Microsoft.EntityFrameworkCore;

namespace Commanda.Core
{
    /// <summary>
    /// Commandaデータベースコンテキスト
    /// </summary>
    public class CommandaDbContext : DbContext
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="options">オプション</param>
        public CommandaDbContext(DbContextOptions<CommandaDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// 実行ログ
        /// </summary>
        public DbSet<ExecutionLog> ExecutionLogs { get; set; } = null!;

        /// <summary>
        /// タスク履歴
        /// </summary>
        public DbSet<TaskHistory> TaskHistories { get; set; } = null!;

        /// <summary>
        /// 拡張機能
        /// </summary>
        public DbSet<ExtensionInfo> Extensions { get; set; } = null!;

        /// <summary>
        /// LLMプロバイダー
        /// </summary>
        public DbSet<LlmProviderConfig> LlmProviders { get; set; } = null!;

        /// <summary>
        /// モデル作成時の設定
        /// </summary>
        /// <param name="modelBuilder">モデルビルダー</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ExecutionLog
            modelBuilder.Entity<ExecutionLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Timestamp).IsRequired();
                entity.Property(e => e.TaskDescription).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Result).HasMaxLength(4000);
                entity.Property(e => e.Duration).IsRequired();
                entity.Property(e => e.StepsExecuted).IsRequired();
                entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            });

            // TaskHistory
            modelBuilder.Entity<TaskHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.SessionId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.UserInput).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.ExecutionPlan).IsRequired();
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.FinalResult).HasMaxLength(4000);
            });

            // ExtensionInfo
            modelBuilder.Entity<ExtensionInfo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Version).IsRequired().HasMaxLength(50);
                entity.Property(e => e.AssemblyPath).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.IsEnabled).HasDefaultValue(true);
                entity.Property(e => e.InstalledAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // LlmProviderConfig
            modelBuilder.Entity<LlmProviderConfig>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ProviderType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ApiKey).IsRequired(); // 暗号化済み
                entity.Property(e => e.BaseUri).HasMaxLength(500);
                entity.Property(e => e.ModelName).HasMaxLength(100);
                entity.Property(e => e.IsDefault).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }
    }
}