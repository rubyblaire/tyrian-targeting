using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Bindings.ImGui;
using TyrianTargeting.Models;

namespace TyrianTargeting.Services;

public sealed class TargetMarkerOverlayService
{
    private readonly Configuration configuration;
    private readonly TargetCycleService targetCycleService;

    public TargetMarkerOverlayService(Configuration configuration, TargetCycleService targetCycleService)
    {
        this.configuration = configuration;
        this.targetCycleService = targetCycleService;
    }

    public void Draw()
    {
        if (!this.configuration.ShowTargetMarkersOutsideWindow)
            return;

        try
        {
            this.DrawCurrentTargetMarker();
            this.DrawCalledTargetMarkers();
            this.DrawSoftPreviewMarker();
        }
        catch (Exception ex)
        {
            PluginServices.Log.Verbose(ex, "Tyrian Targeting marker overlay failed while drawing.");
        }
    }

    private void DrawCurrentTargetMarker()
    {
        if (!this.configuration.ShowCurrentTargetMarker)
            return;

        var target = PluginServices.TargetManager.Target;
        if (target is null || !target.IsValid())
            return;

        this.DrawMarker(target.Position, MarkerKind.Current, this.GetCurrentColor(), this.configuration.MarkerSize);
    }

    private void DrawCalledTargetMarkers()
    {
        if (!this.configuration.ShowCalledTargetMarker || this.configuration.CalledTargetObjectId == 0)
            return;

        foreach (var target in this.targetCycleService.CachedTargets)
        {
            if (!target.IsCalledTarget)
                continue;

            this.DrawMarker(target.Position, MarkerKind.Called, this.GetCalledColor(), this.configuration.MarkerSize * 1.10f);
        }
    }

    private void DrawSoftPreviewMarker()
    {
        if (!this.configuration.ShowSoftTargetPreview)
            return;

        var preview = this.targetCycleService.SoftPreview;
        if (preview is null)
            return;

        if (PluginServices.TargetManager.Target?.GameObjectId == preview.ObjectId)
            return;

        this.DrawMarker(preview.Position, MarkerKind.Preview, this.GetPreviewColor(), this.configuration.MarkerSize * 0.82f);
    }

    private void DrawMarker(Vector3 worldPosition, MarkerKind kind, Vector4 color, float size)
    {
        var markerWorldPosition = worldPosition + new Vector3(0f, this.configuration.MarkerWorldYOffset, 0f);

        if (!PluginServices.GameGui.WorldToScreen(markerWorldPosition, out var screenPosition))
            return;

        screenPosition.Y -= this.configuration.MarkerScreenYOffset;

        var alpha = Math.Clamp(this.configuration.MarkerOpacity, 0.05f, 1.0f);
        color.W *= alpha;

        var drawList = ImGui.GetForegroundDrawList();
        var fill = ImGui.GetColorU32(color);
        var outline = ImGui.GetColorU32(new Vector4(0.02f, 0.01f, 0.01f, alpha));
        var glow = ImGui.GetColorU32(new Vector4(color.X, color.Y, color.Z, alpha * 0.22f));

        // Original Tyrian Targeting marker: diamond cap + downward chevron.
        // This intentionally does not bundle or reproduce Guild Wars 2 UI art.
        var half = size * 0.5f;
        var diamondHalf = size * 0.24f;
        var diamondCenter = screenPosition + new Vector2(0f, -size * 0.12f);

        var top = diamondCenter + new Vector2(0f, -diamondHalf);
        var right = diamondCenter + new Vector2(diamondHalf, 0f);
        var bottom = diamondCenter + new Vector2(0f, diamondHalf);
        var left = diamondCenter + new Vector2(-diamondHalf, 0f);

        var chevronTopLeft = screenPosition + new Vector2(-half * 0.78f, size * 0.10f);
        var chevronTopRight = screenPosition + new Vector2(half * 0.78f, size * 0.10f);
        var chevronTip = screenPosition + new Vector2(0f, half * 0.82f);

        drawList.AddCircleFilled(screenPosition, size * 0.50f, glow, 24);
        drawList.AddQuadFilled(top, right, bottom, left, fill);
        drawList.AddQuad(top, right, bottom, left, outline, 2.0f);
        drawList.AddTriangleFilled(chevronTopLeft, chevronTopRight, chevronTip, fill);
        drawList.AddTriangle(chevronTopLeft, chevronTopRight, chevronTip, outline, 2.0f);

        if (kind == MarkerKind.Called)
        {
            var ring = ImGui.GetColorU32(new Vector4(1.0f, 0.78f, 0.24f, alpha * 0.85f));
            drawList.AddCircle(screenPosition, size * 0.58f, ring, 28, 2.0f);
        }
        else if (kind == MarkerKind.Preview)
        {
            var faint = ImGui.GetColorU32(new Vector4(color.X, color.Y, color.Z, alpha * 0.50f));
            drawList.AddCircle(screenPosition, size * 0.48f, faint, 24, 1.5f);
        }
    }

    private Vector4 GetCurrentColor()
    {
        if (!this.configuration.MarkerUseThemeColor)
            return new Vector4(0.90f, 0.05f, 0.05f, 1.0f);

        return this.configuration.ActiveWindowTheme switch
        {
            WindowTheme.HeartOfThorns => new Vector4(0.22f, 0.86f, 0.32f, 1.0f),
            WindowTheme.PathOfFire => new Vector4(1.0f, 0.42f, 0.10f, 1.0f),
            WindowTheme.EndOfDragons => new Vector4(0.08f, 0.90f, 0.95f, 1.0f),
            WindowTheme.SecretsOfTheObscure => new Vector4(0.70f, 0.38f, 1.0f, 1.0f),
            WindowTheme.JanthirWilds => new Vector4(0.90f, 0.64f, 0.22f, 1.0f),
            _ => new Vector4(0.92f, 0.08f, 0.08f, 1.0f),
        };
    }

    private Vector4 GetCalledColor()
    {
        return new Vector4(1.0f, 0.76f, 0.22f, 1.0f);
    }

    private Vector4 GetPreviewColor()
    {
        var current = this.GetCurrentColor();
        return new Vector4(current.X, current.Y, current.Z, 0.62f);
    }

    private enum MarkerKind
    {
        Current,
        Called,
        Preview,
    }
}
