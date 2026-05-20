using System.Text;
using Dalamud.Game.ClientState.Objects.Enums;
using TyrianTargeting.Models;

namespace TyrianTargeting.Services;

public sealed class TargetCycleService
{
    private readonly Configuration configuration;
    private readonly TargetScannerService scanner;
    private readonly TargetSortService sorter;
    private readonly TargetSelectionService selection;
    private readonly CalledTargetService calledTargets;
    private readonly SoftTargetPreviewService softPreview;

    private readonly List<TargetCandidate> cachedTargets = new();
    private DateTime cacheBuiltAt = DateTime.MinValue;
    private int currentIndex = -1;
    private DateTime lastCalledTargetRejectAt = DateTime.MinValue;

    public TargetCycleService(Configuration configuration)
    {
        this.configuration = configuration;
        this.scanner = new TargetScannerService(configuration);
        this.sorter = new TargetSortService(configuration);
        this.selection = new TargetSelectionService();
        this.calledTargets = new CalledTargetService(configuration);
        this.softPreview = new SoftTargetPreviewService(configuration);
    }

    public IReadOnlyList<TargetCandidate> CachedTargets => this.cachedTargets;
    public TargetCandidate? SoftPreview => this.softPreview.CurrentPreview;
    public string CacheAgeText => this.cacheBuiltAt == DateTime.MinValue ? "never" : $"{(DateTime.UtcNow - this.cacheBuiltAt).TotalMilliseconds:0} ms";

    public void TargetNext()
    {
        this.EnsureCache();
        if (!this.HasTargets())
            return;

        this.currentIndex = (this.currentIndex + 1) % this.cachedTargets.Count;
        this.selection.Select(this.cachedTargets[this.currentIndex]);
    }

    public void TargetPrevious()
    {
        this.EnsureCache();
        if (!this.HasTargets())
            return;

        this.currentIndex--;
        if (this.currentIndex < 0)
            this.currentIndex = this.cachedTargets.Count - 1;

        this.selection.Select(this.cachedTargets[this.currentIndex]);
    }

    public void TargetNearest()
    {
        this.RebuildCache();
        if (!this.HasTargets())
            return;

        this.currentIndex = 0;
        this.selection.Select(this.cachedTargets[0]);
    }

    public void SetCalledTargetFromCurrentCycle()
    {
        this.EnsureCache();
        if (!this.HasTargets())
            return;

        var index = this.currentIndex >= 0 && this.currentIndex < this.cachedTargets.Count ? this.currentIndex : 0;
        this.calledTargets.Set(this.cachedTargets[index]);
        this.RebuildCache();
    }

    public void SetCalledTargetFromCurrentTarget()
    {
        try
        {
            var target = PluginServices.TargetManager.Target;
            if (target is null || !target.IsValid() || target.GameObjectId == 0)
            {
                this.PrintCalledTargetReject("Tyrian Targeting: no valid current target to call.");
                return;
            }

            var name = target.Name.TextValue;
            if (string.IsNullOrWhiteSpace(name))
                name = "Unknown Target";

            if (this.IsFriendlyOrUnsupportedCalledTarget(target))
            {
                this.PrintCalledTargetReject($"Tyrian Targeting: cannot set called target to friendly target: {name}.");
                return;
            }

            this.RebuildCache();

            var candidate = this.cachedTargets.FirstOrDefault(t => t.ObjectId == target.GameObjectId);
            if (candidate is null)
            {
                this.PrintCalledTargetReject($"Tyrian Targeting: cannot set called target to {name}. It is not a valid enemy under the current filters.");
                return;
            }

            this.calledTargets.Set(candidate);
            this.RebuildCache();
        }
        catch (Exception ex)
        {
            PluginServices.Log.Error(ex, "Tyrian Targeting failed to set called target from current target.");
            PluginServices.ChatGui.Print("Tyrian Targeting: failed to set called target. Check /xllog for details.");
        }
    }

    public void TargetCalledTarget()
    {
        this.EnsureCache();
        if (!this.HasTargets())
            return;

        var called = this.cachedTargets.FirstOrDefault(t => t.IsCalledTarget);
        if (called is null)
        {
            PluginServices.ChatGui.Print("Tyrian Targeting: no called target found in the current target list.");
            return;
        }

        this.currentIndex = this.cachedTargets.IndexOf(called);
        this.selection.Select(called);
    }

    public void ClearCalledTarget()
    {
        this.calledTargets.Clear();
        this.RebuildCache();
    }

    public void ClearCache()
    {
        this.cachedTargets.Clear();
        this.softPreview.Clear();
        this.currentIndex = -1;
        this.cacheBuiltAt = DateTime.MinValue;
    }

    public void RebuildCacheForDebug()
    {
        this.RebuildCache();
    }

    public string BuildDebugReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Tyrian Targeting Debug Report");
        sb.AppendLine($"Profile: {this.configuration.ActiveProfile}");
        sb.AppendLine($"Sort Mode: {this.configuration.SortMode}");
        sb.AppendLine($"Cache Age: {this.CacheAgeText}");
        sb.AppendLine($"Cached Targets: {this.cachedTargets.Count}");
        sb.AppendLine();

        foreach (var target in this.cachedTargets)
        {
            sb.AppendLine($"{target.Name} | ID={target.ObjectId} | Dist={target.Distance:0.0} | Cam={target.CameraScore:0.00} | HP={target.HpPercent:0.0} | Score={target.FinalScore:0.0} | Hostile={target.IsHostile} | Targetable={target.IsTargetable} | Combat={target.IsInCombat}");
        }

        return sb.ToString();
    }

    private bool IsFriendlyOrUnsupportedCalledTarget(Dalamud.Game.ClientState.Objects.Types.IGameObject target)
    {
        // In normal PvE, called targets are enemy BattleNpc objects only.
        // This intentionally rejects friendly players, NPCs, minions, retainers, mounts,
        // event objects, and anything else that is not a normal enemy bucket.
        if (target.ObjectKind != ObjectKind.BattleNpc)
        {
            // If we ever support PvP targeting later, this is the only exception we should consider.
            if (!(PluginServices.ClientState.IsPvP && target.ObjectKind == ObjectKind.Pc))
                return true;
        }

        if (target.ObjectKind == ObjectKind.Pc && !PluginServices.ClientState.IsPvP)
            return true;

        if (target.OwnerId != 0 && target.OwnerId != 0xE0000000)
            return true;

        // The rebuilt cache below still has the final say, so allied battle NPCs, dummies,
        // exclusions, range filters, or anything filtered out by user settings cannot be called.
        return false;
    }

    private void PrintCalledTargetReject(string message)
    {
        var now = DateTime.UtcNow;
        if ((now - this.lastCalledTargetRejectAt).TotalMilliseconds < 1250)
            return;

        this.lastCalledTargetRejectAt = now;
        PluginServices.ChatGui.Print(message);
    }

    private bool HasTargets()
    {
        if (this.cachedTargets.Count != 0)
            return true;

        PluginServices.ChatGui.Print("Tyrian Targeting: no valid targets found. Check range, filters, and targetability.");
        return false;
    }

    private void EnsureCache()
    {
        var ageMs = (DateTime.UtcNow - this.cacheBuiltAt).TotalMilliseconds;
        if (this.cachedTargets.Count == 0 || ageMs > this.configuration.CacheLifetimeMs)
            this.RebuildCache();
    }

    private void RebuildCache()
    {
        this.cachedTargets.Clear();
        this.cachedTargets.AddRange(this.scanner.Scan());
        this.calledTargets.MarkCalledTargets(this.cachedTargets);

        var sorted = this.sorter.Sort(this.cachedTargets);
        this.cachedTargets.Clear();
        this.cachedTargets.AddRange(sorted);
        this.softPreview.UpdatePreview(this.cachedTargets);

        this.cacheBuiltAt = DateTime.UtcNow;
        this.currentIndex = -1;
    }
}
