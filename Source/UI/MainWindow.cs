using Dalamud.Interface.Windowing;
using Dalamud.Game.ClientState.Keys;
using System.Diagnostics;
using Dalamud.Bindings.ImGui;
using System.Numerics;
using TyrianTargeting.Models;
using TyrianTargeting.Services;

namespace TyrianTargeting.UI;

public sealed class MainWindow : Window
{
    private const int TyrianColorPushCount = 24;
    private const int TyrianStyleVarPushCount = 4;

    private static readonly Vector4 ObsidianBlack = new(0.035f, 0.035f, 0.045f, 1.0f);
    private static readonly Vector4 ObsidianPanel = new(0.055f, 0.055f, 0.070f, 1.0f);
    private static readonly Vector4 TyrianRed = new(0.72f, 0.08f, 0.08f, 1.0f);
    private static readonly Vector4 TyrianRedHover = new(0.88f, 0.12f, 0.12f, 1.0f);
    private static readonly Vector4 TyrianRedActive = new(0.55f, 0.03f, 0.03f, 1.0f);
    private static readonly Vector4 SoftText = new(0.92f, 0.88f, 0.84f, 1.0f);
    private static readonly Vector4 MutedText = new(0.68f, 0.62f, 0.58f, 1.0f);
    private static readonly Vector4 HeaderText = new(1.0f, 0.82f, 0.78f, 1.0f);
    private static readonly (string Label, int Key)[] TargetCalledKeyOptions =
    {
        ("R", 0x52),
        ("F", 0x46),
        ("G", 0x47),
        ("V", 0x56),
        ("B", 0x42),
        ("Y", 0x59),
        ("Mouse 4", 0x05),
        ("Mouse 5", 0x06),
        ("F6", 0x75),
        ("F7", 0x76),
        ("F8", 0x77),
        ("F9", 0x78),
        ("F10", 0x79),
        ("F11", 0x7A),
        ("F12", 0x7B),
    };


    private readonly Configuration configuration;
    private readonly TargetCycleService targetCycleService;
    private readonly Action<bool>? setControllerOverlayOpen;
    private string exclusionDraft = string.Empty;

    public MainWindow(Configuration configuration, TargetCycleService targetCycleService, Action<bool>? setControllerOverlayOpen = null)
        : base("Tyrian Targeting###TyrianTargetingMainWindow")
    {
        this.configuration = configuration;
        this.targetCycleService = targetCycleService;
        this.setControllerOverlayOpen = setControllerOverlayOpen;
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(560, 520),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public override void PreDraw()
    {
        // Push the theme before Dalamud begins the window so the native title bar
        // uses the obsidian title colors too. Pushing inside Draw() is too late
        // for TitleBg / TitleBgActive.
        PushTyrianTheme(this.configuration);
    }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("TyrianTargetingTabs"))
        {
            if (ImGui.BeginTabItem("Targeting"))
            {
                this.DrawTargetingTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Markers"))
            {
                this.DrawMarkersTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Advanced"))
            {
                this.DrawAdvancedTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    public override void PostDraw()
    {
        PopTyrianTheme();
    }

    private readonly record struct ThemePalette(
        Vector4 ObsidianBlack,
        Vector4 ObsidianPanel,
        Vector4 Accent,
        Vector4 AccentHover,
        Vector4 AccentActive,
        Vector4 FrameHover,
        Vector4 FrameActive,
        Vector4 HeaderText);

    private static ThemePalette GetThemePalette(WindowTheme theme)
    {
        return theme switch
        {
            WindowTheme.HeartOfThorns => new ThemePalette(
                ObsidianBlack,
                new Vector4(0.035f, 0.070f, 0.045f, 1.0f),
                new Vector4(0.14f, 0.58f, 0.22f, 1.0f),
                new Vector4(0.22f, 0.78f, 0.32f, 1.0f),
                new Vector4(0.08f, 0.36f, 0.14f, 1.0f),
                new Vector4(0.05f, 0.18f, 0.08f, 1.0f),
                new Vector4(0.08f, 0.28f, 0.12f, 1.0f),
                new Vector4(0.82f, 1.0f, 0.78f, 1.0f)),

            WindowTheme.PathOfFire => new ThemePalette(
                ObsidianBlack,
                new Vector4(0.075f, 0.050f, 0.030f, 1.0f),
                new Vector4(0.85f, 0.34f, 0.08f, 1.0f),
                new Vector4(1.0f, 0.48f, 0.14f, 1.0f),
                new Vector4(0.58f, 0.18f, 0.04f, 1.0f),
                new Vector4(0.24f, 0.10f, 0.04f, 1.0f),
                new Vector4(0.38f, 0.14f, 0.04f, 1.0f),
                new Vector4(1.0f, 0.88f, 0.70f, 1.0f)),

            WindowTheme.EndOfDragons => new ThemePalette(
                ObsidianBlack,
                new Vector4(0.035f, 0.065f, 0.070f, 1.0f),
                new Vector4(0.00f, 0.62f, 0.72f, 1.0f),
                new Vector4(0.12f, 0.86f, 0.95f, 1.0f),
                new Vector4(0.00f, 0.36f, 0.42f, 1.0f),
                new Vector4(0.02f, 0.16f, 0.18f, 1.0f),
                new Vector4(0.02f, 0.28f, 0.31f, 1.0f),
                new Vector4(0.72f, 1.0f, 1.0f, 1.0f)),

            WindowTheme.SecretsOfTheObscure => new ThemePalette(
                ObsidianBlack,
                new Vector4(0.060f, 0.045f, 0.080f, 1.0f),
                new Vector4(0.48f, 0.22f, 0.86f, 1.0f),
                new Vector4(0.68f, 0.36f, 1.0f, 1.0f),
                new Vector4(0.30f, 0.12f, 0.56f, 1.0f),
                new Vector4(0.14f, 0.06f, 0.22f, 1.0f),
                new Vector4(0.22f, 0.08f, 0.35f, 1.0f),
                new Vector4(0.90f, 0.78f, 1.0f, 1.0f)),

            WindowTheme.JanthirWilds => new ThemePalette(
                ObsidianBlack,
                new Vector4(0.060f, 0.055f, 0.040f, 1.0f),
                new Vector4(0.66f, 0.45f, 0.18f, 1.0f),
                new Vector4(0.86f, 0.62f, 0.26f, 1.0f),
                new Vector4(0.42f, 0.28f, 0.10f, 1.0f),
                new Vector4(0.18f, 0.13f, 0.06f, 1.0f),
                new Vector4(0.30f, 0.20f, 0.08f, 1.0f),
                new Vector4(1.0f, 0.92f, 0.72f, 1.0f)),

            _ => new ThemePalette(
                ObsidianBlack,
                ObsidianPanel,
                TyrianRed,
                TyrianRedHover,
                TyrianRedActive,
                new Vector4(0.22f, 0.06f, 0.06f, 1.0f),
                new Vector4(0.35f, 0.04f, 0.04f, 1.0f),
                HeaderText),
        };
    }

    private static string GetThemeDisplayName(WindowTheme theme)
    {
        return theme switch
        {
            WindowTheme.CoreTyria => "Core Tyria",
            WindowTheme.HeartOfThorns => "Heart of Thorns",
            WindowTheme.PathOfFire => "Path of Fire",
            WindowTheme.EndOfDragons => "End of Dragons",
            WindowTheme.SecretsOfTheObscure => "Secrets of the Obscure",
            WindowTheme.JanthirWilds => "Janthir Wilds",
            _ => theme.ToString(),
        };
    }


    private static Vector4 Mix(Vector4 a, Vector4 b, float amount)
    {
        return new Vector4(
            a.X + ((b.X - a.X) * amount),
            a.Y + ((b.Y - a.Y) * amount),
            a.Z + ((b.Z - a.Z) * amount),
            1.0f);
    }

    private static Vector4 GetInputBg(ThemePalette palette)
    {
        return Mix(palette.ObsidianPanel, palette.AccentActive, 0.35f);
    }

    private static Vector4 GetInputHoverBg(ThemePalette palette)
    {
        return Mix(palette.ObsidianPanel, palette.Accent, 0.45f);
    }

    private static Vector4 GetInputActiveBg(ThemePalette palette)
    {
        return Mix(palette.ObsidianPanel, palette.AccentHover, 0.55f);
    }

    private static void PushTyrianTheme(Configuration configuration)
    {
        var palette = GetThemePalette(configuration.ActiveWindowTheme);

        ImGui.PushStyleColor(ImGuiCol.WindowBg, palette.ObsidianBlack);
        ImGui.PushStyleColor(ImGuiCol.ChildBg, palette.ObsidianPanel);
        ImGui.PushStyleColor(ImGuiCol.PopupBg, palette.ObsidianBlack);
        ImGui.PushStyleColor(ImGuiCol.Border, palette.AccentActive);
        ImGui.PushStyleColor(ImGuiCol.Separator, palette.AccentActive);
        ImGui.PushStyleColor(ImGuiCol.MenuBarBg, palette.ObsidianBlack);

        ImGui.PushStyleColor(ImGuiCol.TitleBg, palette.ObsidianBlack);
        ImGui.PushStyleColor(ImGuiCol.TitleBgActive, palette.ObsidianBlack);
        ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, palette.ObsidianBlack);

        ImGui.PushStyleColor(ImGuiCol.Tab, palette.AccentActive);
        ImGui.PushStyleColor(ImGuiCol.TabHovered, palette.AccentHover);
        ImGui.PushStyleColor(ImGuiCol.TabActive, palette.Accent);

        ImGui.PushStyleColor(ImGuiCol.Header, palette.AccentActive);
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, palette.AccentHover);
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, palette.Accent);

        ImGui.PushStyleColor(ImGuiCol.Button, palette.Accent);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, palette.AccentHover);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, palette.AccentActive);

        ImGui.PushStyleColor(ImGuiCol.CheckMark, palette.AccentHover);
        ImGui.PushStyleColor(ImGuiCol.FrameBg, GetInputBg(palette));
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, GetInputHoverBg(palette));
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, GetInputActiveBg(palette));

        ImGui.PushStyleColor(ImGuiCol.Text, SoftText);
        ImGui.PushStyleColor(ImGuiCol.TextDisabled, MutedText);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 8f);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 5f);
        ImGui.PushStyleVar(ImGuiStyleVar.GrabRounding, 5f);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 7f);
    }

    private static void PopTyrianTheme()
    {
        ImGui.PopStyleVar(TyrianStyleVarPushCount);
        ImGui.PopStyleColor(TyrianColorPushCount);
    }

    private void DrawHeader()
    {
        var palette = GetThemePalette(this.configuration.ActiveWindowTheme);
        ImGui.PushStyleColor(ImGuiCol.ChildBg, palette.ObsidianPanel);

        try
        {
            if (ImGui.BeginChild("TyrianTargetingHeader", new Vector2(0, 54), true))
            {
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);
                ImGui.TextUnformatted("Tyrian Targeting");
                ImGui.TextColored(palette.HeaderText, "GW2-inspired manual tab targeting for FFXIV. No auto-targeting, no combat automation.");
            }

            ImGui.EndChild();
        }
        finally
        {
            ImGui.PopStyleColor(1);
        }
    }


    private void DrawAdvancedTab()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(18f, 8f));
        try
        {
            if (ImGui.Button("Discord"))
                OpenUrl("https://discord.gg/Dr836dmbqh");

            ImGui.SameLine();
            if (ImGui.Button("Ko-fi"))
                OpenUrl("https://ko-fi.com/rubyblaire");
        }
        finally
        {
            ImGui.PopStyleVar(1);
        }

        ImGui.Separator();

        if (ImGui.CollapsingHeader("Theme"))
            this.DrawThemeTab();

        if (ImGui.CollapsingHeader("Cache"))
            this.DrawCacheTab();

        if (ImGui.CollapsingHeader("Exclusions"))
            this.DrawExclusionsTab();

        if (ImGui.CollapsingHeader("Debug"))
            this.DrawDebugTab();
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            PluginServices.Log.Warning(ex, "Failed to open URL: {Url}", url);
            ImGui.SetClipboardText(url);
        }
    }

    private void DrawProfilesTab()
    {
        var profile = (int)this.configuration.ActiveProfile;
        var profiles = Enum.GetValues<TargetingProfile>();
        var names = profiles.Select(GetProfileDisplayName).ToArray();

        if (ImGui.Combo("Targeting Preset", ref profile, names, names.Length))
        {
            this.configuration.ApplyProfile(profiles[profile]);
            this.configuration.Save();
            this.targetCycleService.ClearCache();
        }
    }

    private static string GetProfileDisplayName(TargetingProfile profile)
    {
        return profile switch
        {
            TargetingProfile.RaidTrial => "Raid / Trial",
            TargetingProfile.FateOverworld => "Fate / Overworld",
            TargetingProfile.SafeMode => "Safe Mode",
            _ => profile.ToString(),
        };
    }

    private void DrawThemeTab()
    {
        var current = (int)this.configuration.ActiveWindowTheme;
        var themes = Enum.GetValues<WindowTheme>();
        var names = themes.Select(GetThemeDisplayName).ToArray();

        if (ImGui.Combo("Window Theme", ref current, names, names.Length))
        {
            this.configuration.ActiveWindowTheme = themes[current];
            this.configuration.Save();
        }

        var palette = GetThemePalette(this.configuration.ActiveWindowTheme);
        ImGui.Spacing();
        ImGui.TextColored(palette.HeaderText, $"Active Theme: {GetThemeDisplayName(this.configuration.ActiveWindowTheme)}");
        ImGui.Spacing();
        ImGui.PushStyleColor(ImGuiCol.ChildBg, palette.ObsidianPanel);
        try
        {
            if (ImGui.BeginChild("ThemePreview", new Vector2(0, 92), true))
            {
                ImGui.TextUnformatted("Theme Preview");
                ImGui.PushStyleColor(ImGuiCol.Button, palette.Accent);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, palette.AccentHover);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, palette.AccentActive);
                try
                {
                    ImGui.Button("Sample Button");
                }
                finally
                {
                    ImGui.PopStyleColor(3);
                }

                var sample = true;
                ImGui.Checkbox("Sample checkbox", ref sample);
            }

            ImGui.EndChild();
        }
        finally
        {
            ImGui.PopStyleColor(1);
        }
    }

    private void DrawTargetingTab()
    {
        ImGui.TextColored(HeaderText, "Profiles");
        this.DrawProfilesTab();

        ImGui.Separator();

        this.Checkbox("Enable TAB targeting", nameof(this.configuration.EnableTabTargeting), this.configuration.EnableTabTargeting, v => this.configuration.EnableTabTargeting = v);
        this.Checkbox("Shift+TAB targets previous", nameof(this.configuration.ShiftTabTargetsPrevious), this.configuration.ShiftTabTargetsPrevious, v => this.configuration.ShiftTabTargetsPrevious = v);
        this.Checkbox("Suppress native TAB input", nameof(this.configuration.SuppressNativeTabInput), this.configuration.SuppressNativeTabInput, v => this.configuration.SuppressNativeTabInput = v);
        this.Checkbox("Pause while typing in text fields", nameof(this.configuration.PauseTabTargetingWhileTyping), this.configuration.PauseTabTargetingWhileTyping, v => this.configuration.PauseTabTargetingWhileTyping = v);

        ImGui.Separator();

        var sortMode = (int)this.configuration.SortMode;
        var sortNames = Enum.GetNames<TargetSortMode>();
        if (ImGui.Combo("Sort Mode", ref sortMode, sortNames, sortNames.Length))
        {
            this.configuration.SortMode = (TargetSortMode)sortMode;
            this.configuration.ActiveProfile = TargetingProfile.Custom;
            this.configuration.Save();
            this.targetCycleService.ClearCache();
        }

        this.Checkbox("Prefer camera-centered enemies", nameof(this.configuration.PreferCameraCenter), this.configuration.PreferCameraCenter, v => this.configuration.PreferCameraCenter = v);
        this.Checkbox("Prefer enemies in combat", nameof(this.configuration.PreferCombatTargets), this.configuration.PreferCombatTargets, v => this.configuration.PreferCombatTargets = v);
        this.Checkbox("Prefer called target", nameof(this.configuration.PreferCalledTarget), this.configuration.PreferCalledTarget, v => this.configuration.PreferCalledTarget = v);

        ImGui.Separator();
        ImGui.TextColored(HeaderText, "Filters");

        this.Checkbox("Hostile only", nameof(this.configuration.HostileOnly), this.configuration.HostileOnly, v => this.configuration.HostileOnly = v);
        this.Checkbox("Alive only", nameof(this.configuration.AliveOnly), this.configuration.AliveOnly, v => this.configuration.AliveOnly = v);
        this.Checkbox("Targetable only", nameof(this.configuration.TargetableOnly), this.configuration.TargetableOnly, v => this.configuration.TargetableOnly = v);
        this.Checkbox("Only enemies in camera cone", nameof(this.configuration.InCameraConeOnly), this.configuration.InCameraConeOnly, v => this.configuration.InCameraConeOnly = v);
        this.Checkbox("Only enemies in combat", nameof(this.configuration.InCombatOnly), this.configuration.InCombatOnly, v => this.configuration.InCombatOnly = v);
        this.Checkbox("Ignore targets behind player", nameof(this.configuration.IgnoreTargetsBehindPlayer), this.configuration.IgnoreTargetsBehindPlayer, v => this.configuration.IgnoreTargetsBehindPlayer = v);
        this.Checkbox("Ignore pets/minions", nameof(this.configuration.IgnorePetsAndMinions), this.configuration.IgnorePetsAndMinions, v => this.configuration.IgnorePetsAndMinions = v);
        this.Checkbox("Ignore training dummies", nameof(this.configuration.IgnoreTrainingDummies), this.configuration.IgnoreTrainingDummies, v => this.configuration.IgnoreTrainingDummies = v);

        var useDistance = this.configuration.UseDistanceLimit;
        if (ImGui.Checkbox("Use distance limit", ref useDistance))
        {
            this.configuration.UseDistanceLimit = useDistance;
            this.configuration.ActiveProfile = TargetingProfile.Custom;
            this.configuration.Save();
            this.targetCycleService.ClearCache();
        }

        var maxDistance = this.configuration.MaxDistance;
        if (ImGui.SliderFloat("Max Distance", ref maxDistance, 5f, 100f, "%.0f yalms"))
        {
            this.configuration.MaxDistance = maxDistance;
            this.configuration.ActiveProfile = TargetingProfile.Custom;
            this.configuration.Save();
            this.targetCycleService.ClearCache();
        }

        var minHp = this.configuration.MinimumHpPercent;
        if (ImGui.SliderFloat("Minimum HP %", ref minHp, 0f, 100f, "%.0f%%"))
        {
            this.configuration.MinimumHpPercent = Math.Min(minHp, this.configuration.MaximumHpPercent);
            this.configuration.ActiveProfile = TargetingProfile.Custom;
            this.configuration.Save();
            this.targetCycleService.ClearCache();
        }

        var maxHp = this.configuration.MaximumHpPercent;
        if (ImGui.SliderFloat("Maximum HP %", ref maxHp, 0f, 100f, "%.0f%%"))
        {
            this.configuration.MaximumHpPercent = Math.Max(maxHp, this.configuration.MinimumHpPercent);
            this.configuration.ActiveProfile = TargetingProfile.Custom;
            this.configuration.Save();
            this.targetCycleService.ClearCache();
        }

        ImGui.Separator();
        ImGui.TextColored(HeaderText, "Called Target");
        this.DrawCalledTargetTab();
    }

    private void DrawCacheTab()
    {
        var cacheLifetime = this.configuration.CacheLifetimeMs;
        if (ImGui.SliderInt("Cycle Cache", ref cacheLifetime, 250, 5000, "%d ms"))
        {
            this.configuration.CacheLifetimeMs = cacheLifetime;
            this.configuration.ActiveProfile = TargetingProfile.Custom;
            this.configuration.Save();
        }

        this.Checkbox("Keep current target in cycle list", nameof(this.configuration.KeepCurrentTargetInCycle), this.configuration.KeepCurrentTargetInCycle, v => this.configuration.KeepCurrentTargetInCycle = v);
        this.Checkbox("Rebuild when current target dies", nameof(this.configuration.RebuildCacheWhenCurrentTargetDies), this.configuration.RebuildCacheWhenCurrentTargetDies, v => this.configuration.RebuildCacheWhenCurrentTargetDies = v);
        this.Checkbox("Rebuild on large camera move", nameof(this.configuration.RebuildCacheOnLargeCameraMove), this.configuration.RebuildCacheOnLargeCameraMove, v => this.configuration.RebuildCacheOnLargeCameraMove = v);
        this.Checkbox("Rebuild on large player move", nameof(this.configuration.RebuildCacheOnLargePlayerMove), this.configuration.RebuildCacheOnLargePlayerMove, v => this.configuration.RebuildCacheOnLargePlayerMove = v);

        ImGui.Separator();
        ImGui.TextUnformatted($"Cache age: {this.targetCycleService.CacheAgeText}");
        ImGui.TextUnformatted($"Cached targets: {this.targetCycleService.CachedTargets.Count}");

        if (ImGui.Button("Refresh Target List"))
            this.targetCycleService.RebuildCacheForDebug();

        ImGui.SameLine();
        if (ImGui.Button("Clear Target Cache"))
            this.targetCycleService.ClearCache();
    }

    private void DrawCalledTargetTab()
    {
        ImGui.TextUnformatted(string.IsNullOrWhiteSpace(this.configuration.CalledTargetName)
            ? "Called Target: none"
            : $"Called Target: {this.configuration.CalledTargetName} ({this.configuration.CalledTargetObjectId})");

        this.Checkbox("Show called target marker", nameof(this.configuration.ShowCalledTargetMarker), this.configuration.ShowCalledTargetMarker, v => this.configuration.ShowCalledTargetMarker = v);
        this.Checkbox("Show soft target preview", nameof(this.configuration.ShowSoftTargetPreview), this.configuration.ShowSoftTargetPreview, v => this.configuration.ShowSoftTargetPreview = v);

        if (this.targetCycleService.SoftPreview is { } preview)
            ImGui.TextColored(HeaderText, $"Soft Preview: {preview.Name}");
        else
            ImGui.TextDisabled("Soft Preview: none");

        ImGui.Spacing();

        this.DrawControllerSupportBlock();

        ImGui.Spacing();

        ImGui.TextColored(HeaderText, "Called Target Keybinds");
        if (this.configuration.ControllerPresetHidesKeyboardHelp)
            ImGui.TextDisabled("Controller Mode is active. Keyboard binds are optional; cross-hotbar macros are the recommended controller workflow.");
        this.Checkbox("Enable called target keybinds", nameof(this.configuration.EnableCalledTargetKeybinds), this.configuration.EnableCalledTargetKeybinds, v => this.configuration.EnableCalledTargetKeybinds = v);
        this.Checkbox("Enable target-called key", nameof(this.configuration.TargetCalledWithT), this.configuration.TargetCalledWithT, v => this.configuration.TargetCalledWithT = v);
        this.DrawTargetCalledKeyCombo();
        this.Checkbox("Ctrl+TAB sets called target", nameof(this.configuration.SetCalledWithCtrlTab), this.configuration.SetCalledWithCtrlTab, v => this.configuration.SetCalledWithCtrlTab = v);
        this.Checkbox("Ctrl+Shift+TAB clears called target", nameof(this.configuration.ClearCalledWithCtrlShiftTab), this.configuration.ClearCalledWithCtrlShiftTab, v => this.configuration.ClearCalledWithCtrlShiftTab = v);
        this.Checkbox("Suppress called target key input", nameof(this.configuration.SuppressCalledTargetKeyInput), this.configuration.SuppressCalledTargetKeyInput, v => this.configuration.SuppressCalledTargetKeyInput = v);

        ImGui.Spacing();

        if (this.configuration.EnableControllerMode)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(18f, 10f));
            try
            {
                if (ImGui.Button("Set Called From Current Target", new Vector2(0, 0)))
                    this.targetCycleService.SetCalledTargetFromCurrentTarget();

                if (ImGui.Button("Target Called", new Vector2(0, 0)))
                    this.targetCycleService.TargetCalledTarget();

                if (ImGui.Button("Clear Called", new Vector2(0, 0)))
                    this.targetCycleService.ClearCalledTarget();
            }
            finally
            {
                ImGui.PopStyleVar(1);
            }
        }
        else
        {
            if (ImGui.Button("Set Called From Current Target"))
                this.targetCycleService.SetCalledTargetFromCurrentTarget();

            ImGui.SameLine();
            if (ImGui.Button("Target Called"))
                this.targetCycleService.TargetCalledTarget();

            ImGui.SameLine();
            if (ImGui.Button("Clear Called"))
                this.targetCycleService.ClearCalledTarget();
        }
    }

    private void DrawControllerSupportBlock()
    {
        ImGui.TextColored(HeaderText, "Controller Support");

        if (ImGui.Button("Apply Controller Quick Setup"))
            this.ApplyControllerQuickSetupPreset();

        ImGui.SameLine();
        if (ImGui.Button("Copy All Core Macros"))
            ImGui.SetClipboardText("/micon \"Target Forward\"\n/tt call\n\n/micon \"Target\"\n/tt target\n\n/micon \"Clear Target\"\n/tt clearcall");

        ImGui.TextWrapped("Best controller support is handled through FFXIV's own cross hotbar. Create the macros below, place them on your cross hotbar, then use Tyrian Targeting without keyboard binds.");

        this.Checkbox("Enable controller-friendly mode", nameof(this.configuration.EnableControllerMode), this.configuration.EnableControllerMode, v => this.configuration.EnableControllerMode = v);
        this.Checkbox("Show controller quick setup", nameof(this.configuration.ShowControllerQuickSetup), this.configuration.ShowControllerQuickSetup, v => this.configuration.ShowControllerQuickSetup = v);
        this.Checkbox("Show controller macro help", nameof(this.configuration.ShowControllerMacroHelp), this.configuration.ShowControllerMacroHelp, v => this.configuration.ShowControllerMacroHelp = v);

        var showOverlay = this.configuration.ShowControllerOverlay;
        if (ImGui.Checkbox("Show compact controller overlay", ref showOverlay))
        {
            this.configuration.ShowControllerOverlay = showOverlay;
            this.configuration.Save();
            this.setControllerOverlayOpen?.Invoke(showOverlay);
        }

        if (this.configuration.ShowControllerQuickSetup)
            this.DrawControllerQuickSetupChecklist();

        if (!this.configuration.ShowControllerMacroHelp)
            return;

        ImGui.Separator();
        ImGui.TextColored(HeaderText, "Cross Hotbar Macros");
        this.DrawControllerMacroRow("Call Current Target", "/micon \"Target Forward\"\n/tt call", "Sets your current valid enemy as the called target.");
        this.DrawControllerMacroRow("Target Called", "/micon \"Target\"\n/tt target", "Targets the saved called target.");
        this.DrawControllerMacroRow("Clear Called", "/micon \"Clear Target\"\n/tt clearcall", "Clears the saved called target.");
        this.DrawControllerMacroRow("Cycle Next", "/micon \"Target Forward\"\n/tt next", "Optional: cycles forward through Tyrian's target list.");
        this.DrawControllerMacroRow("Cycle Previous", "/micon \"Target Back\"\n/tt prev", "Optional: cycles backward through Tyrian's target list.");

        ImGui.TextDisabled("Tip: use /tt setup later to reopen this guide quickly.");
    }

    public void OpenControllerQuickSetup()
    {
        this.IsOpen = true;
        this.ApplyControllerQuickSetupPreset();
    }

    private void ApplyControllerQuickSetupPreset()
    {
        this.configuration.EnableControllerMode = true;
        this.configuration.ShowControllerQuickSetup = true;
        this.configuration.ShowControllerMacroHelp = true;
        this.configuration.ShowControllerOverlay = true;
        this.configuration.PreferCalledTarget = true;
        this.configuration.ShowCalledTargetMarker = true;
        this.configuration.ShowSoftTargetPreview = true;
        this.configuration.ControllerPresetHidesKeyboardHelp = true;
        this.configuration.Save();
        this.setControllerOverlayOpen?.Invoke(true);
    }

    private void DrawControllerQuickSetupChecklist()
    {
        ImGui.Separator();
        ImGui.TextColored(HeaderText, "Controller Quick Setup");
        ImGui.BulletText("1. Click Copy All Core Macros or copy each macro below.");
        ImGui.BulletText("2. In FFXIV, create macros for Call, Target Called, and Clear Called.");
        ImGui.BulletText("3. Drag those macros onto your cross hotbar.");
        ImGui.BulletText("4. Optional: add Next and Previous if you want full controller cycling.");
        ImGui.BulletText("5. Leave the compact overlay on if you want a small status readout while playing.");
    }

    private void DrawControllerMacroRow(string label, string macroText, string description)
    {
        ImGui.PushID(label);
        try
        {
            if (ImGui.Button($"Copy##{label}", new Vector2(72f, 0f)))
                ImGui.SetClipboardText(macroText);

            ImGui.SameLine();
            ImGui.TextUnformatted(label);
            ImGui.SameLine();
            ImGui.TextDisabled(description);
        }
        finally
        {
            ImGui.PopID();
        }
    }

    private void DrawTargetCalledKeyCombo()
    {
        var currentIndex = Array.FindIndex(TargetCalledKeyOptions, option => option.Key == this.configuration.TargetCalledKeyVirtualKey);
        if (currentIndex < 0)
            currentIndex = 0;

        var labels = TargetCalledKeyOptions.Select(option => option.Label).ToArray();

        if (ImGui.Combo("Target Called Key", ref currentIndex, labels, labels.Length))
        {
            this.configuration.TargetCalledKeyVirtualKey = TargetCalledKeyOptions[currentIndex].Key;
            this.configuration.Save();
        }

        ImGui.TextDisabled("Ctrl+TAB sets called target. Ctrl+Shift+TAB clears it. This key only targets the saved called target. T is intentionally omitted.");
    }


    private void DrawMarkersTab()
    {
        this.Checkbox("Show enemy markers outside the settings window", nameof(this.configuration.ShowTargetMarkersOutsideWindow), this.configuration.ShowTargetMarkersOutsideWindow, v => this.configuration.ShowTargetMarkersOutsideWindow = v);
        this.Checkbox("Show current target marker", nameof(this.configuration.ShowCurrentTargetMarker), this.configuration.ShowCurrentTargetMarker, v => this.configuration.ShowCurrentTargetMarker = v);
        this.Checkbox("Show called target marker", nameof(this.configuration.ShowCalledTargetMarker), this.configuration.ShowCalledTargetMarker, v => this.configuration.ShowCalledTargetMarker = v);
        this.Checkbox("Show soft target preview marker", nameof(this.configuration.ShowSoftTargetPreview), this.configuration.ShowSoftTargetPreview, v => this.configuration.ShowSoftTargetPreview = v);
        this.Checkbox("Use active theme color", nameof(this.configuration.MarkerUseThemeColor), this.configuration.MarkerUseThemeColor, v => this.configuration.MarkerUseThemeColor = v);

        ImGui.Separator();

        var markerSize = this.configuration.MarkerSize;
        if (ImGui.SliderFloat("Marker Size", ref markerSize, 18f, 96f, "%.0f px"))
        {
            this.configuration.MarkerSize = markerSize;
            this.configuration.Save();
        }

        var worldOffset = this.configuration.MarkerWorldYOffset;
        if (ImGui.SliderFloat("World Height Offset", ref worldOffset, 0.5f, 6.0f, "%.1f yalms"))
        {
            this.configuration.MarkerWorldYOffset = worldOffset;
            this.configuration.Save();
        }

        var screenOffset = this.configuration.MarkerScreenYOffset;
        if (ImGui.SliderFloat("Screen Y Offset", ref screenOffset, -60f, 80f, "%.0f px"))
        {
            this.configuration.MarkerScreenYOffset = screenOffset;
            this.configuration.Save();
        }

        var opacity = this.configuration.MarkerOpacity;
        if (ImGui.SliderFloat("Marker Opacity", ref opacity, 0.10f, 1.0f, "%.2f"))
        {
            this.configuration.MarkerOpacity = opacity;
            this.configuration.Save();
        }

        ImGui.Separator();
        ImGui.TextColored(HeaderText, "Marker Legend");
        ImGui.BulletText("Current target: filled Tyrian chevron/diamond.");
        ImGui.BulletText("Called target: larger marker with a gold ring.");
        ImGui.BulletText("Soft preview: smaller, faint marker for what TAB will likely choose next.");
    }

    private void DrawExclusionsTab()
    {
        ImGui.SetNextItemWidth(Math.Max(220f, ImGui.GetContentRegionAvail().X - 132f));
        ImGui.InputTextWithHint("##ExclusionNameContains", "Name contains...", ref this.exclusionDraft, 80);
        ImGui.SameLine();
        if (ImGui.Button("Add Exclusion") && !string.IsNullOrWhiteSpace(this.exclusionDraft))
        {
            this.configuration.ExcludedNameContains.Add(this.exclusionDraft.Trim());
            this.exclusionDraft = string.Empty;
            this.configuration.Save();
            this.targetCycleService.ClearCache();
        }

        ImGui.Spacing();

        for (var i = 0; i < this.configuration.ExcludedNameContains.Count; i++)
        {
            ImGui.PushID(i);
            ImGui.TextUnformatted(this.configuration.ExcludedNameContains[i]);
            ImGui.SameLine();
            if (ImGui.Button("Remove"))
            {
                this.configuration.ExcludedNameContains.RemoveAt(i);
                this.configuration.Save();
                this.targetCycleService.ClearCache();
                ImGui.PopID();
                break;
            }

            ImGui.PopID();
        }
    }

    private void DrawDebugTab()
    {
        ImGui.TextUnformatted("Target Debug Window");
        ImGui.Separator();

        if (ImGui.Button("Refresh Target List"))
            this.targetCycleService.RebuildCacheForDebug();

        ImGui.SameLine();
        if (ImGui.Button("Copy Debug Report"))
            ImGui.SetClipboardText(this.targetCycleService.BuildDebugReport());

        ImGui.SameLine();
        if (ImGui.Button("Target Top Candidate"))
            this.targetCycleService.TargetNearest();

        ImGui.Spacing();

        var cached = this.targetCycleService.CachedTargets;
        if (cached.Count == 0)
        {
            ImGui.TextWrapped("No cached targets yet. Press Refresh Target List near enemies. If this stays empty, check filters like Hostile Only, Targetable Only, In Combat Only, and Max Distance.");
            return;
        }

        if (ImGui.BeginTable("TargetDebugTable", 9))
        {
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Distance");
            ImGui.TableSetupColumn("Camera");
            ImGui.TableSetupColumn("HP");
            ImGui.TableSetupColumn("Score");
            ImGui.TableSetupColumn("Hostile");
            ImGui.TableSetupColumn("Targetable");
            ImGui.TableSetupColumn("Combat");
            ImGui.TableSetupColumn("Flags");
            ImGui.TableHeadersRow();

            foreach (var target in cached)
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.TextUnformatted(target.Name);
                ImGui.TableSetColumnIndex(1);
                ImGui.TextUnformatted(target.Distance.ToString("0.0"));
                ImGui.TableSetColumnIndex(2);
                ImGui.TextUnformatted(target.CameraScore.ToString("0.00"));
                ImGui.TableSetColumnIndex(3);
                ImGui.TextUnformatted(target.HpPercent.ToString("0.0"));
                ImGui.TableSetColumnIndex(4);
                ImGui.TextUnformatted(target.FinalScore.ToString("0.0"));
                ImGui.TableSetColumnIndex(5);
                ImGui.TextUnformatted(target.IsHostile ? "yes" : "no");
                ImGui.TableSetColumnIndex(6);
                ImGui.TextUnformatted(target.IsTargetable ? "yes" : "no");
                ImGui.TableSetColumnIndex(7);
                ImGui.TextUnformatted(target.IsInCombat ? "yes" : "no");
                ImGui.TableSetColumnIndex(8);
                ImGui.TextUnformatted((target.IsCalledTarget ? "Called " : string.Empty) + (target.IsSoftPreview ? "Preview" : string.Empty));
            }

            ImGui.EndTable();
        }
    }

    private void Checkbox(string label, string id, bool currentValue, Action<bool> setter)
    {
        var value = currentValue;
        if (ImGui.Checkbox($"{label}##{id}", ref value))
        {
            setter(value);
            this.configuration.ActiveProfile = TargetingProfile.Custom;
            this.configuration.Save();
            this.targetCycleService.ClearCache();
        }
    }
}
