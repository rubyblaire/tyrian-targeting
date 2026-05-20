using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using Dalamud.Bindings.ImGui;

namespace TyrianTargeting.Services;

public sealed class KeybindService : IDisposable
{
    private static readonly VirtualKey ControlKey = (VirtualKey)0x11;
    private static readonly VirtualKey LeftControlKey = (VirtualKey)0xA2;
    private static readonly VirtualKey RightControlKey = (VirtualKey)0xA3;

    private readonly Configuration configuration;
    private readonly TargetCycleService targetCycleService;

    private bool previousTabDown;
    private bool previousTargetCalledKeyDown;

    public KeybindService(Configuration configuration, TargetCycleService targetCycleService)
    {
        this.configuration = configuration;
        this.targetCycleService = targetCycleService;

        PluginServices.Framework.Update += this.OnFrameworkUpdate;
    }

    public void Dispose()
    {
        PluginServices.Framework.Update -= this.OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (this.configuration.PauseTabTargetingWhileTyping && ImGui.GetIO().WantTextInput)
        {
            this.previousTabDown = false;
            this.previousTargetCalledKeyDown = false;
            return;
        }

        this.HandleTabKeybinds();
        this.HandleTargetCalledKeybind();
    }

    private void HandleTabKeybinds()
    {
        var tabDown = PluginServices.KeyState[VirtualKey.TAB];
        var tabPressedThisFrame = tabDown && !this.previousTabDown;
        this.previousTabDown = tabDown;

        if (!tabPressedThisFrame)
            return;

        var ctrlDown = IsControlDown();
        var shiftDown = IsShiftDown();
        var handled = false;

        // Called target combos intentionally run before normal TAB cycling.
        if (this.configuration.EnableCalledTargetKeybinds && ctrlDown && shiftDown && this.configuration.ClearCalledWithCtrlShiftTab)
        {
            this.targetCycleService.ClearCalledTarget();
            handled = true;
        }
        else if (this.configuration.EnableCalledTargetKeybinds && ctrlDown && this.configuration.SetCalledWithCtrlTab)
        {
            this.targetCycleService.SetCalledTargetFromCurrentTarget();
            handled = true;
        }
        else if (this.configuration.EnableTabTargeting)
        {
            if (shiftDown && this.configuration.ShiftTabTargetsPrevious)
                this.targetCycleService.TargetPrevious();
            else
                this.targetCycleService.TargetNext();

            handled = true;
        }

        if (handled && (this.configuration.SuppressNativeTabInput || this.configuration.SuppressCalledTargetKeyInput))
            SuppressKey(VirtualKey.TAB);
    }

    private void HandleTargetCalledKeybind()
    {
        if (!this.configuration.EnableCalledTargetKeybinds || !this.configuration.TargetCalledWithT)
        {
            this.previousTargetCalledKeyDown = false;
            return;
        }

        var targetCalledKey = (VirtualKey)this.configuration.TargetCalledKeyVirtualKey;

        // TAB is reserved for the TAB / Shift+TAB / Ctrl+TAB targeting flow.
        // Do not let the configurable called-target bind steal it.
        if (targetCalledKey == VirtualKey.TAB)
        {
            this.previousTargetCalledKeyDown = false;
            return;
        }

        var keyDown = PluginServices.KeyState[targetCalledKey];
        var pressedThisFrame = keyDown && !this.previousTargetCalledKeyDown;
        this.previousTargetCalledKeyDown = keyDown;

        if (!pressedThisFrame)
            return;

        var ctrlDown = IsControlDown();
        var shiftDown = IsShiftDown();

        if (ctrlDown || shiftDown)
            return;

        this.targetCycleService.TargetCalledTarget();

        if (this.configuration.SuppressCalledTargetKeyInput)
            SuppressKey(targetCalledKey);
    }

    private static bool IsShiftDown()
    {
        return PluginServices.KeyState[VirtualKey.SHIFT]
               || PluginServices.KeyState[VirtualKey.LSHIFT]
               || PluginServices.KeyState[VirtualKey.RSHIFT];
    }

    private static bool IsControlDown()
    {
        return PluginServices.KeyState[ControlKey]
               || PluginServices.KeyState[LeftControlKey]
               || PluginServices.KeyState[RightControlKey];
    }

    private static void SuppressKey(VirtualKey key)
    {
        try
        {
            PluginServices.KeyState[key] = false;
            PluginServices.KeyState.SetRawValue(key, 0);
        }
        catch (Exception ex)
        {
            PluginServices.Log.Debug("Could not suppress key input: {Message}", ex.Message);
        }
    }
}
