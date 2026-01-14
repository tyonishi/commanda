using Commanda.Core;
using Microsoft.AspNetCore.Mvc;

namespace Commanda.WebApi.Controllers;

/// <summary>
/// 設定管理APIコントローラー
/// </summary>
[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly ISettingsManager _settingsManager;
    private readonly ILlmProviderManager _llmManager;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="settingsManager">設定マネージャー</param>
    /// <param name="llmManager">LLMプロバイダーマネージャー</param>
    public SettingsController(ISettingsManager settingsManager, ILlmProviderManager llmManager)
    {
        _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
        _llmManager = llmManager ?? throw new ArgumentNullException(nameof(llmManager));
    }

    /// <summary>
    /// LLM設定を取得
    /// </summary>
    /// <returns>LLM設定</returns>
    [HttpGet("llm")]
    public async Task<ActionResult<LlmSettings>> GetLlmSettings()
    {
        try
        {
            var settings = await _settingsManager.LoadLlmSettingsAsync();
            return Ok(settings);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"設定の取得に失敗しました: {ex.Message}");
        }
    }

    /// <summary>
    /// LLMプロバイダーを追加
    /// </summary>
    /// <param name="config">プロバイダー設定</param>
    /// <returns>追加されたプロバイダー</returns>
    [HttpPost("llm/providers")]
    public async Task<ActionResult<LlmProviderConfig>> AddProvider([FromBody] LlmProviderConfig config)
    {
        if (config == null)
        {
            return BadRequest("プロバイダー設定が指定されていません");
        }

        try
        {
            var added = await _settingsManager.AddProviderAsync(config);
            return CreatedAtAction(nameof(GetProvider), new { name = added.Name }, added);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"プロバイダーの追加に失敗しました: {ex.Message}");
        }
    }

    /// <summary>
    /// LLMプロバイダーを取得
    /// </summary>
    /// <param name="name">プロバイダー名</param>
    /// <returns>プロバイダー設定</returns>
    [HttpGet("llm/providers/{name}")]
    public async Task<ActionResult<LlmProviderConfig>> GetProvider(string name)
    {
        try
        {
            var settings = await _settingsManager.LoadLlmSettingsAsync();
            var provider = settings.GetProvider(name);
            if (provider == null)
            {
                return NotFound($"プロバイダー '{name}' が見つかりません");
            }
            return Ok(provider);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"プロバイダーの取得に失敗しました: {ex.Message}");
        }
    }

    /// <summary>
    /// LLMプロバイダーを削除
    /// </summary>
    /// <param name="name">プロバイダー名</param>
    /// <returns>削除結果</returns>
    [HttpDelete("llm/providers/{name}")]
    public async Task<IActionResult> RemoveProvider(string name)
    {
        try
        {
            var removed = await _settingsManager.RemoveProviderAsync(name);
            return removed ? NoContent() : NotFound($"プロバイダー '{name}' が見つかりません");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"プロバイダーの削除に失敗しました: {ex.Message}");
        }
    }

    /// <summary>
    /// LLMプロバイダーをテスト
    /// </summary>
    /// <param name="name">プロバイダー名</param>
    /// <returns>テスト結果</returns>
    [HttpPost("llm/providers/{name}/test")]
    public async Task<ActionResult<bool>> TestProvider(string name)
    {
        try
        {
            var settings = await _settingsManager.LoadLlmSettingsAsync();
            var provider = settings.GetProvider(name);
            if (provider == null)
            {
                return NotFound($"プロバイダー '{name}' が見つかりません");
            }

            var result = await _llmManager.TestProviderAsync(provider);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"プロバイダーのテストに失敗しました: {ex.Message}");
        }
    }
}