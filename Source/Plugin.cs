using Dalamud.Game.Command;
using Dalamud.Plugin;
using TyrianTargeting.Services;
using TyrianTargeting.UI;

namespace TyrianTargeting;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/tt";

    public string Name => "Tyrian Targeting";

    private readonly WindowSystemService windowSystem;
    private readonly Configuration configuration;
    private readonly TargetCycleService targetCycleService;
    private readonly KeybindService keybindService;
    private readonly TargetMarkerOverlayService targetMarkerOverlayService;
    private readonly ControllerOverlayWindow controllerOverlayWindow;
    private readonly MainWindow mainWindow;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        PluginServices.PluginInterface = pluginInterface;
        pluginInterface.Create<PluginServices>();

        this.configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        this.configuration.Initialize(pluginInterface);

        this.targetCycleService = new TargetCycleService(this.configuration);
        this.keybindService = new KeybindService(this.configuration, this.targetCycleService);
        this.targetMarkerOverlayService = new TargetMarkerOverlayService(this.configuration, this.targetCycleService);
        this.controllerOverlayWindow = new ControllerOverlayWindow(this.configuration, this.targetCycleService);
        this.windowSystem = new WindowSystemService("TyrianTargeting");
        this.mainWindow = new MainWindow(this.configuration, this.targetCycleService, isOpen => this.controllerOverlayWindow.IsOpen = isOpen);
        this.windowSystem.AddWindow(this.mainWindow);
        this.windowSystem.AddWindow(this.controllerOverlayWindow);

        PluginServices.CommandManager.AddHandler(CommandName, new CommandInfo(this.OnCommand)
        {
            HelpMessage = "Open Tyrian Targeting. Use /tt help for commands."
        });

        PluginServices.PluginInterface.UiBuilder.Draw += this.windowSystem.Draw;
        PluginServices.PluginInterface.UiBuilder.Draw += this.targetMarkerOverlayService.Draw;
        PluginServices.PluginInterface.UiBuilder.OpenConfigUi += this.ToggleMainWindow;
        PluginServices.PluginInterface.UiBuilder.OpenMainUi += this.ToggleMainWindow;
    }

    public void Dispose()
    {
        PluginServices.PluginInterface.UiBuilder.Draw -= this.windowSystem.Draw;
        PluginServices.PluginInterface.UiBuilder.Draw -= this.targetMarkerOverlayService.Draw;
        PluginServices.PluginInterface.UiBuilder.OpenConfigUi -= this.ToggleMainWindow;
        PluginServices.PluginInterface.UiBuilder.OpenMainUi -= this.ToggleMainWindow;

        PluginServices.CommandManager.RemoveHandler(CommandName);
        this.keybindService.Dispose();
        this.windowSystem.Dispose();
    }

    private void ToggleMainWindow()
    {
        this.mainWindow.IsOpen = !this.mainWindow.IsOpen;
    }

    private void OnCommand(string command, string args)
    {
        var trimmed = args.Trim().ToLowerInvariant();

        switch (trimmed)
        {
            case "":
                this.ToggleMainWindow();
                break;

            case "help":
                PluginServices.ChatGui.Print("Tyrian Targeting: /tt, /tt setup, /tt next, /tt prev, /tt nearest, /tt call, /tt target, /tt clearcall, /tt overlay, /tt controller on, /tt controller off, /tt clearcache.");
                break;

            case "setup":
            case "quicksetup":
            case "controller":
            case "controllerhelp":
            case "controller setup":
            case "gamepad":
                this.mainWindow.OpenControllerQuickSetup();
                this.controllerOverlayWindow.IsOpen = this.configuration.ShowControllerOverlay;
                PluginServices.ChatGui.Print("Tyrian Targeting: controller quick setup opened. Copy the macros and place them on your cross hotbar.");
                break;

            case "controller on":
            case "gamepad on":
                this.configuration.EnableControllerMode = true;
                this.configuration.ShowControllerQuickSetup = true;
                this.configuration.ShowControllerMacroHelp = true;
                this.configuration.ShowControllerOverlay = true;
                this.configuration.PreferCalledTarget = true;
                this.configuration.ShowCalledTargetMarker = true;
                this.configuration.ShowSoftTargetPreview = true;
                this.configuration.Save();
                this.controllerOverlayWindow.IsOpen = true;
                PluginServices.ChatGui.Print("Tyrian Targeting: controller-friendly mode enabled. Use /tt setup for macro help.");
                break;

            case "controller off":
            case "gamepad off":
                this.configuration.EnableControllerMode = false;
                this.configuration.ShowControllerOverlay = false;
                this.configuration.Save();
                this.controllerOverlayWindow.IsOpen = false;
                PluginServices.ChatGui.Print("Tyrian Targeting: controller-friendly mode disabled.");
                break;

            case "overlay":
            case "controller overlay":
                this.configuration.ShowControllerOverlay = !this.configuration.ShowControllerOverlay;
                this.configuration.Save();
                this.controllerOverlayWindow.IsOpen = this.configuration.ShowControllerOverlay;
                PluginServices.ChatGui.Print(this.configuration.ShowControllerOverlay
                    ? "Tyrian Targeting: controller overlay shown."
                    : "Tyrian Targeting: controller overlay hidden.");
                break;

            case "next":
                this.targetCycleService.TargetNext();
                break;

            case "prev":
            case "previous":
                this.targetCycleService.TargetPrevious();
                break;

            case "nearest":
                this.targetCycleService.TargetNearest();
                break;

            case "call":
            case "set":
            case "mark":
            case "focus":
            case "setcall":
                this.targetCycleService.SetCalledTargetFromCurrentTarget();
                break;

            case "target":
            case "goto":
            case "attack":
            case "targetcall":
            case "called":
                this.targetCycleService.TargetCalledTarget();
                break;

            case "clearcall":
            case "uncall":
            case "unmark":
            case "clearcalled":
                this.targetCycleService.ClearCalledTarget();
                break;

            case "refresh":
                this.targetCycleService.RebuildCacheForDebug();
                PluginServices.ChatGui.Print("Tyrian Targeting: target cache rebuilt.");
                break;

            case "clearcache":
            case "clear":
                this.targetCycleService.ClearCache();
                PluginServices.ChatGui.Print("Tyrian Targeting: target cycle cache cleared.");
                break;

            default:
                PluginServices.ChatGui.Print("Tyrian Targeting: unknown command. Try /tt help or /tt setup.");
                break;
        }
    }
}
