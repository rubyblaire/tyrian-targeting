using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System.Numerics;
using TyrianTargeting.Services;

namespace TyrianTargeting.UI;

public sealed class ControllerOverlayWindow : Window
{
    private static readonly Vector4 HeaderText = new(1.0f, 0.82f, 0.78f, 1.0f);

    private readonly Configuration configuration;
    private readonly TargetCycleService targetCycleService;

    public ControllerOverlayWindow(Configuration configuration, TargetCycleService targetCycleService)
        : base("Tyrian Controller Overlay###TyrianControllerOverlay")
    {
        this.configuration = configuration;
        this.targetCycleService = targetCycleService;
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(260, 112),
            MaximumSize = new Vector2(520, 240),
        };
        this.IsOpen = configuration.ShowControllerOverlay;
    }

    public override void Draw()
    {
        if (!this.configuration.ShowControllerOverlay)
        {
            this.IsOpen = false;
            return;
        }

        ImGui.TextColored(HeaderText, "Tyrian Controller");
        ImGui.Separator();

        var calledText = string.IsNullOrWhiteSpace(this.configuration.CalledTargetName)
            ? "Called: none"
            : $"Called: {this.configuration.CalledTargetName}";
        ImGui.TextWrapped(calledText);

        if (this.targetCycleService.SoftPreview is { } preview)
            ImGui.TextWrapped($"Preview: {preview.Name}");
        else
            ImGui.TextDisabled("Preview: none");

        ImGui.TextDisabled("Cross hotbar: /tt call  /tt target  /tt clearcall");

        if (ImGui.Button("Call", new Vector2(72f, 0f)))
            this.targetCycleService.SetCalledTargetFromCurrentTarget();

        ImGui.SameLine();
        if (ImGui.Button("Target", new Vector2(72f, 0f)))
            this.targetCycleService.TargetCalledTarget();

        ImGui.SameLine();
        if (ImGui.Button("Clear", new Vector2(72f, 0f)))
            this.targetCycleService.ClearCalledTarget();
    }

    public override void OnClose()
    {
        this.configuration.ShowControllerOverlay = false;
        this.configuration.Save();
    }
}
