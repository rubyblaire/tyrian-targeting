using TyrianTargeting.Models;

namespace TyrianTargeting.Services;

public sealed class TargetSelectionService
{
    public void Select(TargetCandidate candidate)
    {
        try
        {
            var gameObject = PluginServices.ObjectTable.SearchById(candidate.ObjectId);
            if (gameObject is null || !gameObject.IsValid())
            {
                PluginServices.ChatGui.Print($"Tyrian Targeting: target is no longer available: {candidate.Name}.");
                return;
            }

            if (!gameObject.IsTargetable)
            {
                PluginServices.ChatGui.Print($"Tyrian Targeting: target is no longer targetable: {candidate.Name}.");
                return;
            }

            PluginServices.TargetManager.Target = gameObject;
        }
        catch (Exception ex)
        {
            PluginServices.Log.Error(ex, $"Tyrian Targeting failed to select target {candidate.Name} ({candidate.ObjectId}).");
            PluginServices.ChatGui.Print("Tyrian Targeting: failed to select target. Check /xllog for details.");
        }
    }
}
