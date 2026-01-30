using Commanda.Core;
using Commanda.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.Versioning;

namespace Commanda.WebApi.Controllers;

[SupportedOSPlatform("windows")]

/// <summary>
/// 拡張機能管理APIコントローラー
/// </summary>
[ApiController]
[Route("api/extensions")]
public class ExtensionsController : ControllerBase
{
    private readonly IExtensionManager _extensionManager;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="extensionManager">拡張機能マネージャー</param>
    public ExtensionsController(IExtensionManager extensionManager)
    {
        _extensionManager = extensionManager ?? throw new ArgumentNullException(nameof(extensionManager));
    }

    /// <summary>
    /// 拡張機能一覧を取得
    /// </summary>
    /// <returns>拡張機能一覧</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExtensionInfo>>> GetExtensions()
    {
        try
        {
            var extensions = await _extensionManager.GetLoadedExtensionsAsync();
            return Ok(extensions.Select(e => new ExtensionInfo
            {
                Name = e.Name,
                Version = e.Version
            }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"拡張機能一覧の取得に失敗しました: {ex.Message}");
        }
    }

    /// <summary>
    /// 拡張機能を再読み込み
    /// </summary>
    /// <returns>再読み込み結果</returns>
    [HttpPost("reload")]
    public async Task<IActionResult> ReloadExtensions()
    {
        try
        {
            await _extensionManager.ReloadExtensionsAsync();
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"拡張機能の再読み込みに失敗しました: {ex.Message}");
        }
    }

    /// <summary>
    /// 拡張機能を有効化/無効化
    /// </summary>
    /// <param name="name">拡張機能名</param>
    /// <param name="enabled">有効化フラグ</param>
    /// <returns>設定結果</returns>
    [HttpPost("{name}/enable")]
    public async Task<IActionResult> SetExtensionEnabled(string name, [FromBody] bool enabled)
    {
        try
        {
            var success = await _extensionManager.SetExtensionEnabledAsync(name, enabled);
            return success ? Ok() : NotFound($"拡張機能 '{name}' が見つかりません");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"拡張機能の設定に失敗しました: {ex.Message}");
        }
    }

    /// <summary>
    /// 拡張機能の詳細情報を取得
    /// </summary>
    /// <param name="name">拡張機能名</param>
    /// <returns>拡張機能詳細</returns>
    [HttpGet("{name}")]
    public async Task<ActionResult<ExtensionInfo>> GetExtension(string name)
    {
        try
        {
            var extensions = await _extensionManager.GetLoadedExtensionsAsync();
            var extension = extensions.FirstOrDefault(e => e.Name == name);

            if (extension == null)
            {
                return NotFound($"拡張機能 '{name}' が見つかりません");
            }

            return Ok(new ExtensionInfo
            {
                Name = extension.Name,
                Version = extension.Version,
                AssemblyPath = extension.AssemblyPath,
                IsEnabled = extension.IsEnabled,
                InstalledAt = extension.InstalledAt,
                LastUsed = extension.LastUsed
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"拡張機能詳細の取得に失敗しました: {ex.Message}");
        }
    }
}