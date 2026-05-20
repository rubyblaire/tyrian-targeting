using TyrianTargeting.Models;

namespace TyrianTargeting.Services;

public sealed class TargetSortService
{
    private readonly Configuration configuration;

    public TargetSortService(Configuration configuration)
    {
        this.configuration = configuration;
    }

    public IReadOnlyList<TargetCandidate> Sort(IEnumerable<TargetCandidate> candidates)
    {
        var filtered = candidates.Where(this.PassesFilters).ToList();

        foreach (var candidate in filtered)
            candidate.FinalScore = this.CalculateScore(candidate);

        return this.configuration.SortMode switch
        {
            TargetSortMode.Distance => filtered.OrderBy(t => t.Distance).ToList(),
            TargetSortMode.CameraCenter => filtered.OrderByDescending(t => t.CameraScore).ThenBy(t => t.Distance).ToList(),
            TargetSortMode.LowestHp => filtered.OrderBy(t => t.HpPercent).ThenBy(t => t.Distance).ToList(),
            TargetSortMode.HighestHp => filtered.OrderByDescending(t => t.HpPercent).ThenBy(t => t.Distance).ToList(),
            TargetSortMode.CurrentCombatOnly => filtered.Where(t => t.IsInCombat).OrderBy(t => t.Distance).ToList(),
            TargetSortMode.LeftToRight => filtered.OrderBy(t => t.ScreenX).ThenBy(t => t.Distance).ToList(),
            TargetSortMode.CurrentTargetRelative => filtered.OrderByDescending(t => t.IsCalledTarget).ThenByDescending(t => t.FinalScore).ToList(),
            _ => filtered.OrderByDescending(t => t.FinalScore).ToList(),
        };
    }

    private bool PassesFilters(TargetCandidate target)
    {
        if (this.configuration.HostileOnly && !target.IsHostile)
            return false;

        if (this.configuration.AliveOnly && target.IsDead)
            return false;

        if (this.configuration.TargetableOnly && !target.IsTargetable)
            return false;

        if (this.configuration.InCombatOnly && !target.IsInCombat)
            return false;

        if (this.configuration.InCameraConeOnly && !target.IsInCameraCone)
            return false;

        if (this.configuration.IgnoreTargetsBehindPlayer && target.IsBehindPlayer)
            return false;

        if (this.configuration.IgnorePetsAndMinions && target.IsPetOrMinion)
            return false;

        if (this.configuration.IgnoreTrainingDummies && target.IsTrainingDummy)
            return false;

        if (this.configuration.UseDistanceLimit && target.Distance > this.configuration.MaxDistance)
            return false;

        if (target.HpPercent < this.configuration.MinimumHpPercent || target.HpPercent > this.configuration.MaximumHpPercent)
            return false;

        foreach (var excluded in this.configuration.ExcludedNameContains)
        {
            if (!string.IsNullOrWhiteSpace(excluded) && target.Name.Contains(excluded, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private float CalculateScore(TargetCandidate target)
    {
        var distanceScore = Math.Max(0f, 100f - target.Distance);
        var cameraScore = this.configuration.PreferCameraCenter ? target.CameraScore * 60f : 0f;
        var combatScore = this.configuration.PreferCombatTargets && target.IsInCombat ? 30f : 0f;
        var calledScore = this.configuration.PreferCalledTarget && target.IsCalledTarget ? 500f : 0f;
        var coneScore = target.IsInCameraCone ? 15f : 0f;

        return distanceScore + cameraScore + combatScore + calledScore + coneScore;
    }
}
