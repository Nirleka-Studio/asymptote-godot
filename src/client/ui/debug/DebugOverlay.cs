using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

namespace Asymptote.Client.UI.Debug;

/// <summary>
/// Autoload. F3 toggles, F3+F6 opens the settings (choose panels/lines, saved to user://).
/// </summary>
public partial class DebugOverlay : CanvasLayer
{
    const string ConfigPath = "user://debug_overlay.cfg";

    public static DebugOverlay? Instance { get; private set; }
    public static readonly DebugContext Context = new();

    public static Node3D? Target
    {
        set => Context.Target = value;
    }

    public static Camera3D? Camera
    {
        set => Context.Camera = value;
    }

    static readonly Font MonoFont = new SystemFont
    {
        FontNames = new[] { "Consolas", "Courier New", "Monaco", "monospace" }
    };

    public void Register(IDebugPanel panel, string title, DebugSide side = DebugSide.Left,
        int order = 0, float refreshHz = 10f)
        => RegisterInternal(panel, title, side, order, refreshHz);

    /// <summary>Quick lambda stat. Call after the overlay autoload is ready.</summary>
    public static void AddStat<T>(string panel, string label, Func<T> getter, DebugSide side = DebugSide.Left,
        int order = 0, float refreshHz = 10f, Color? color = null)
        => Instance?.AddStatInternal(panel, side, order, refreshHz, w => w.Line(label, getter(), color), order);

    sealed class Entry
    {
        public string Title = "";
        public DebugSide Side;
        public int Order;
        public double Interval;
        public double Accum = 1e9; // force an update on the next frame
        public bool Enabled = true;
        public IDebugPanel Panel = null!;
        public Control Root = null!;
        public RichTextLabel Body = null!;
        public string Last = "";
        public readonly PanelWriter W = new();
    }

    sealed class StatPanel : IDebugPanel
    {
        public readonly List<(int Order, Action<PanelWriter> Fn)> Stats = new();

        public void Update(DebugContext ctx, PanelWriter w)
        {
            foreach (var (_, fn) in Stats) fn(w);
        }
    }

    readonly List<Entry> _entries = new();
    readonly Dictionary<string, StatPanel> _statPanels = new();
    readonly Dictionary<string, Entry> _statEntries = new();
    readonly HashSet<string> _hiddenPanels = new();
    readonly Dictionary<string, HashSet<string>> _hiddenLines = new();

    VBoxContainer _left = null!, _right = null!;
    PanelContainer? _settings;
    Tree _tree = null!;
    Input.MouseModeEnum _prevMouse;
    bool _f3Held, _combo;

    HashSet<string> LinesOf(string title)
    {
        if (!_hiddenLines.TryGetValue(title, out var s)) _hiddenLines[title] = s = new();
        return s;
    }

    public override void _Ready()
    {
        Instance = this;
        Layer = 128;
        ProcessMode = ProcessModeEnum.Always;

        _left = MakeColumn(Control.LayoutPreset.TopLeft);
        _right = MakeColumn(Control.LayoutPreset.TopRight);
        _right.GrowHorizontal = Control.GrowDirection.Begin;

        AddChild(_left);
        AddChild(_right);

        LoadConfig();
        Discover();
        Visible = false;
        SetProcess(false);
    }

    public override void _Input(InputEvent ev)
    {
        if (ev is not InputEventKey k || k.Echo) return;

        if (_settings is { Visible: true } && k.Pressed && k.Keycode == Key.Escape)
        {
            CloseSettings();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (k.Keycode == Key.F3)
        {
            if (k.Pressed)
            {
                _f3Held = true;
                _combo = false;
            }
            else
            {
                if (_f3Held && !_combo) ToggleVisible();
                _f3Held = false;
            }

            GetViewport().SetInputAsHandled();
            return;
        }

        if (_f3Held && k.Pressed)
        {
            _combo = true;
            if (k.Keycode == Key.F6) ToggleSettings();
            GetViewport().SetInputAsHandled();
        }
    }

    void ToggleVisible()
    {
        Visible = !Visible;
        SetProcess(Visible);
        if (!Visible) CloseSettings();
    }

    public override void _Process(double delta)
    {
        Context.Delta = delta;
        Context.BeginFrame();

        foreach (var e in _entries)
        {
            if (!e.Enabled) continue;
            e.Accum += delta;
            if (e.Accum < e.Interval) continue;
            e.Accum = 0;

            try
            {
                e.Panel.Update(Context, e.W);
                e.W.Flush();
            }
            catch (Exception ex)
            {
                e.W.Sb.Clear().Append("error: ").Append(ex.Message.Replace("[", "[lb]"));
            }

            var sb = e.W.Sb;
            if (!sb.Equals(e.Last.AsSpan()))
            {
                e.Last = sb.ToString();
                e.Body.Text = e.Last;
                e.Root.Visible = e.Last.Length > 0;
            }
        }
    }

    // Discovery
    const BindingFlags Flags = BindingFlags.Static | BindingFlags.Public |
                               BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    void Discover()
    {
        foreach (var type in typeof(DebugOverlay).Assembly.GetTypes())
        {
            var pa = type.GetCustomAttribute<DebugPanelAttribute>();
            if (pa != null && !type.IsAbstract && typeof(IDebugPanel).IsAssignableFrom(type))
                RegisterInternal((IDebugPanel)Activator.CreateInstance(type)!, pa.Title, pa.Side, pa.Order,
                    pa.RefreshHz);

            foreach (var m in type.GetMethods(Flags)) TryAddStat(m, m);
            foreach (var p in type.GetProperties(Flags))
                if (p.GetMethod is { } getter)
                    TryAddStat(p, getter);
        }

        foreach (var sp in _statPanels.Values)
            sp.Stats.Sort((a, b) => a.Order.CompareTo(b.Order));
    }

    void TryAddStat(MemberInfo member, MethodInfo method)
    {
        var sa = member.GetCustomAttribute<DebugStatAttribute>();
        if (sa == null || method.ReturnType == typeof(void) || method.GetParameters().Length != 0) return;

        Color? color = sa.ColorHex is { } hex ? Color.FromHtml(hex) : null;

        // Compile a typed delegate once => no reflection and no boxing per refresh.
        var make = typeof(DebugOverlay)
            .GetMethod(nameof(MakeWriter), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(method.ReturnType);
        var fn = (Action<PanelWriter>)make.Invoke(null, new object?[] { sa.Label, method, color })!;
        AddStatInternal(sa.Panel, sa.Side, sa.Order, sa.RefreshHz, fn, sa.Order);
    }

    static Action<PanelWriter> MakeWriter<T>(string label, MethodInfo m, Color? color)
    {
        var f = (Func<T>)m.CreateDelegate(typeof(Func<T>));
        return w => w.Line(label, f(), color);
    }

    void AddStatInternal(string panel, DebugSide side, int order, float hz, Action<PanelWriter> fn, int statOrder)
    {
        if (!_statPanels.TryGetValue(panel, out var sp))
        {
            sp = new StatPanel();
            _statPanels[panel] = sp;
            _statEntries[panel] = RegisterInternal(sp, panel, side, order, hz);
        }
        else
        {
            // a panel refreshes as fast as its fastest stat
            var e = _statEntries[panel];
            e.Interval = Math.Min(e.Interval, hz <= 0 ? 0 : 1.0 / hz);
        }

        sp.Stats.Add((statOrder, fn));
    }

    //

    static VBoxContainer MakeColumn(Control.LayoutPreset preset)
    {
        var col = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        col.AddThemeConstantOverride("separation", 4);
        col.SetAnchorsAndOffsetsPreset(preset, Control.LayoutPresetMode.Minsize, 6);
        if (preset == Control.LayoutPreset.TopRight)
        {
            col.GrowHorizontal = Control.GrowDirection.Begin;
        }

        return col;
    }

    // Reference height for 1.0x scale (720p)
    private const float ReferenceHeight = 720f;

    private float GetScaleFactor()
    {
        var viewport = GetViewport();
        if (viewport == null) return 1.0f;

        float screenHeight = viewport.GetVisibleRect().Size.Y;
        // Prevent UI from shrinking below 1.0x on small screens
        return Mathf.Max(1.0f, screenHeight / ReferenceHeight);
    }

    Entry RegisterInternal(IDebugPanel panel, string title, DebugSide side, int order, float hz)
    {
        bool right = side == DebugSide.Right;
        float s = GetScaleFactor(); // Dynamic UI Scale Factor

        // Scaled design measurements
        int titleFontSize = Mathf.RoundToInt(14 * s);
        int bodyFontSize = Mathf.RoundToInt(13 * s);
        int borderWidth = Mathf.Max(2, Mathf.RoundToInt(3 * s));
        int marginH = Mathf.RoundToInt(8 * s);
        int marginV = Mathf.RoundToInt(4 * s);

        // Accent color for side border
        float hue = Mathf.Abs(title.GetHashCode() % 360) / 360f;
        Color categoryColor = Color.FromHsv(hue, 0.85f, 1.0f);

        var root = new VBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = right ? Control.SizeFlags.ShrinkEnd : Control.SizeFlags.ShrinkBegin
        };
        root.AddThemeConstantOverride("separation", 0);

        // 1. TITLE HEADER (Dark Grey)
        var headerBox = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        var headerStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.08f, 0.10f, 0.85f),
            ContentMarginLeft = marginH,
            ContentMarginRight = marginH,
            ContentMarginTop = marginV,
            ContentMarginBottom = marginV,
            BorderColor = categoryColor
        };
        if (right) headerStyle.BorderWidthRight = borderWidth;
        else headerStyle.BorderWidthLeft = borderWidth;

        headerBox.AddThemeStyleboxOverride("panel", headerStyle);

        var headerLabel = new Label
        {
            Text = title,
            HorizontalAlignment = HorizontalAlignment.Left,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        headerLabel.AddThemeFontOverride("font", MonoFont);
        headerLabel.AddThemeFontSizeOverride("font_size", titleFontSize);
        headerLabel.AddThemeColorOverride("font_color", Colors.White);
        headerBox.AddChild(headerLabel);

        // 2. BODY CONTENT (Medium Grey)
        var bodyBox = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        var bodyStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.22f, 0.22f, 0.25f, 0.70f),
            ContentMarginLeft = marginH,
            ContentMarginRight = marginH,
            ContentMarginTop = marginV,
            ContentMarginBottom = marginV,
            BorderColor = categoryColor
        };
        if (right) bodyStyle.BorderWidthRight = borderWidth;
        else bodyStyle.BorderWidthLeft = borderWidth;

        bodyBox.AddThemeStyleboxOverride("panel", bodyStyle);

        var body = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            ScrollActive = false,
            AutowrapMode = TextServer.AutowrapMode.Off,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        body.AddThemeFontOverride("normal_font", MonoFont);
        body.AddThemeFontSizeOverride("normal_font_size", bodyFontSize);
        body.AddThemeColorOverride("default_color", Colors.White);
        bodyBox.AddChild(body);

        root.AddChild(headerBox);
        root.AddChild(bodyBox);

        var e = new Entry
        {
            Title = title, Side = side, Order = order,
            Interval = hz <= 0 ? 0 : 1.0 / hz,
            Panel = panel, Root = root, Body = body,
            Enabled = !_hiddenPanels.Contains(title),
        };
        e.W.Hidden = LinesOf(title);
        e.Root.Visible = e.Enabled;

        _entries.Add(e);
        (right ? _right : _left).AddChild(root);
        Resort();
        return e;
    }

    void Resort()
    {
        foreach (var side in new[] { DebugSide.Left, DebugSide.Right })
        {
            var col = side == DebugSide.Left ? _left : _right;
            int i = 0;
            foreach (var e in _entries.Where(e => e.Side == side).OrderBy(e => e.Order))
                col.MoveChild(e.Root, i++);
        }
    }

    void ToggleSettings()
    {
        if (!Visible)
        {
            Visible = true;
            SetProcess(true);
        }

        if (_settings is { Visible: true }) CloseSettings();
        else OpenSettings();
    }

    void BuildSettings()
    {
        _settings = new PanelContainer { Visible = false };
        _settings.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        _settings.GrowHorizontal = Control.GrowDirection.Both;
        _settings.GrowVertical = Control.GrowDirection.Both;

        var box = new VBoxContainer();
        box.AddChild(new Label { Text = "Debug overlay: choose what to show" });

        _tree = new Tree { HideRoot = true, CustomMinimumSize = new Vector2(380, 440) };
        _tree.ItemEdited += OnTreeEdited;
        box.AddChild(_tree);

        var close = new Button { Text = "Close (Esc)" };
        close.Pressed += CloseSettings;
        box.AddChild(close);

        _settings.AddChild(box);
        AddChild(_settings);
    }

    void OpenSettings()
    {
        if (_settings == null) BuildSettings();

        // Run every panel once, ignoring filters, so hidden lines/panels are still listed.
        foreach (var e in _entries)
        {
            e.W.IgnoreFilter = true;
            e.W.Sb.Clear();
            try
            {
                e.Panel.Update(Context, e.W);
            }
            catch
            {
                // ignore
            }

            e.W.IgnoreFilter = false;
            e.W.Sb.Clear();
            e.Accum = 1e9;
        }

        _tree.Clear();
        var root = _tree.CreateItem();
        foreach (var e in _entries.OrderBy(e => e.Side).ThenBy(e => e.Order))
        {
            var pi = _tree.CreateItem(root);
            pi.SetCellMode(0, TreeItem.TreeCellMode.Check);
            pi.SetText(0, $"{e.Title}  ({e.Side})");
            pi.SetEditable(0, true);
            pi.SetChecked(0, e.Enabled);
            pi.SetMetadata(0, e.Title);

            foreach (var label in e.W.Seen)
            {
                var li = _tree.CreateItem(pi);
                li.SetCellMode(0, TreeItem.TreeCellMode.Check);
                li.SetText(0, label);
                li.SetEditable(0, true);
                li.SetChecked(0, !LinesOf(e.Title).Contains(label));
                li.SetMetadata(0, label);
            }
        }

        _prevMouse = Input.MouseMode;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _settings!.Visible = true;
    }

    void CloseSettings()
    {
        if (_settings is not { Visible: true }) return;
        _settings.Visible = false;
        Input.MouseMode = _prevMouse;
    }

    void OnTreeEdited()
    {
        var it = _tree.GetEdited();
        if (it is null) return;

        bool on = it.IsChecked(0);
        string meta = it.GetMetadata(0).AsString();
        var parent = it.GetParent();

        if (parent.GetParent() is null) // parent is the hidden root => this is a panel row
        {
            if (on) _hiddenPanels.Remove(meta);
            else _hiddenPanels.Add(meta);
            foreach (var e in _entries.Where(x => x.Title == meta))
            {
                e.Enabled = on;
                e.Root.Visible = on && e.Last.Length > 0;
                e.Accum = 1e9;
            }
        }
        else // a line row
        {
            string panel = parent.GetMetadata(0).AsString();
            if (on) LinesOf(panel).Remove(meta);
            else LinesOf(panel).Add(meta);
            foreach (var e in _entries.Where(x => x.Title == panel)) e.Accum = 1e9;
        }

        SaveConfig();
    }

    void LoadConfig()
    {
        var cfg = new ConfigFile();
        if (cfg.Load(ConfigPath) != Error.Ok) return;

        foreach (var t in cfg.GetValue("overlay", "hidden_panels", Array.Empty<string>()).AsStringArray())
            _hiddenPanels.Add(t);

        foreach (var entry in cfg.GetValue("overlay", "hidden_lines", Array.Empty<string>()).AsStringArray())
        {
            int i = entry.IndexOf('|');
            if (i > 0) LinesOf(entry[..i]).Add(entry[(i + 1)..]);
        }
    }

    void SaveConfig()
    {
        var cfg = new ConfigFile();
        cfg.SetValue("overlay", "hidden_panels", _hiddenPanels.ToArray());
        cfg.SetValue("overlay", "hidden_lines",
            _hiddenLines.SelectMany(kv => kv.Value.Select(l => $"{kv.Key}|{l}")).ToArray());
        cfg.Save(ConfigPath);
    }
}