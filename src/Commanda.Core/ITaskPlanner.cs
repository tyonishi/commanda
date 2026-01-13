namespace Commanda.Core;

/// <summary>
/// タスクプランナーのインターフェース
/// </summary>
public interface ITaskPlanner
{
    /// <summary>
    /// 実行計画を生成します
    /// </summary>
    /// <param name="context">エージェントコンテキスト</param>
    /// <param name="token">キャンセレーショントークン</param>
    /// <returns>実行計画</returns>
    Task<ExecutionPlan> GeneratePlanAsync(AgentContext context, CancellationToken token = default);
}