using Godot;
using System.Text;

namespace Asymptote.Client.UI;

public partial class EngineDebugText : CanvasLayer
{
    private Label _debugLabel = null!;
    private string _cachedHwid = string.Empty;
    private string _cachedGpuName = string.Empty;
    private string _cachedGodotVersion = string.Empty;
    private string _cachedOsName = string.Empty;
    private string _cachedGameVersion = string.Empty;

    private readonly StringBuilder _sb = new(512);

    private double _updateTimer = 0.0;
    private const double UpdateInterval = 0.2;

    public override void _Ready()
    {
        // Keep active for Debug exports OR custom staging/playtest builds
        if (!OS.IsDebugBuild() && !OS.HasFeature("staging"))
        {
            QueueFree();
            return;
        }

        Layer = 128;

        _cachedHwid = generateHwid();
        _cachedGpuName = RenderingServer.GetVideoAdapterName();
        _cachedGodotVersion = Engine.GetVersionInfo()["string"].AsString();
        _cachedOsName = OS.GetName();
        _cachedGameVersion = ProjectSettings.GetSetting(
            "application/config/version",
            "UNSET_VERSION_NUMBER"
        ).AsString();

        buildUI();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.F3)
        {
            Visible = !Visible;
        }
    }

    public override void _Process(double delta)
    {
        if (_debugLabel == null || !IsInstanceValid(_debugLabel) || !Visible)
            return;

        _updateTimer += delta;
        if (_updateTimer >= UpdateInterval)
        {
            _updateTimer = 0.0;
            updateDebugText();
        }
    }

    //

    private void buildUI()
    {
        var margin = new MarginContainer();
        margin.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.MouseFilter = Control.MouseFilterEnum.Ignore;

        _debugLabel = new Label();
        _debugLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _debugLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f, 0.9f));
        _debugLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        _debugLabel.AddThemeConstantOverride("outline_size", 4);
        _debugLabel.AddThemeFontSizeOverride("font_size", 13);

        margin.AddChild(_debugLabel);
        AddChild(margin);
    }

    private void updateDebugText()
    {
        double fps = Engine.GetFramesPerSecond();
        double processMs = Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000.0;
        double physicsMs = Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000.0;
        double drawCalls = Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame);
        double staticMemMB = Performance.GetMonitor(Performance.Monitor.MemoryStatic) / (1024.0 * 1024.0);
        double vramMB = Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed) / (1024.0 * 1024.0);

        string currentScene = GetTree().CurrentScene?.SceneFilePath ?? "Unknown Scene";

        _sb.Clear();
        _sb.AppendLine(
            $"Asymptote {_cachedGameVersion} | Godot {_cachedGodotVersion} | OS: {_cachedOsName} | HWID: {_cachedHwid}");
        _sb.AppendLine(
            $"GPU: {_cachedGpuName} | FPS: {fps:F1} | Process: {processMs:F2} ms | Physics: {physicsMs:F2} ms | Draw Calls: {drawCalls} | Static Mem: {staticMemMB:F1} MB | VRAM: {vramMB:F1} MB");
        _sb.AppendLine($"Scene: {currentScene}");

        _debugLabel.Text = _sb.ToString();
    }

    private static string generateHwid()
    {
        string uniqueId = OS.GetUniqueId();
        string processor = OS.GetProcessorName();
        string gpu = RenderingServer.GetVideoAdapterName();

        if (string.IsNullOrEmpty(uniqueId))
            uniqueId = System.Environment.MachineName;

        string rawHardwareString = $"{uniqueId}|{processor}|{gpu}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawHardwareString));

        string hex = System.Convert.ToHexString(hashBytes).ToLowerInvariant();
        return $"{hex[..8]}-{hex[8..12]}-{hex[12..16]}-{hex[16..20]}-{hex[20..32]}";
    }
}