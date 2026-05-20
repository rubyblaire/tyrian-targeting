using TyrianTargeting.Models;

namespace TyrianTargeting.Services;

public sealed class CalledTargetService
{
    private readonly Configuration configuration;

    public CalledTargetService(Configuration configuration)
    {
        this.configuration = configuration;
    }

    public bool HasCalledTarget => this.configuration.CalledTargetObjectId != 0;

    public void Set(TargetCandidate candidate)
    {
        this.Set(candidate.ObjectId, candidate.Name);
    }

    public void Set(ulong objectId, string name)
    {
        this.configuration.CalledTargetObjectId = objectId;
        this.configuration.CalledTargetName = name;
        this.configuration.Save();
        PluginServices.ChatGui.Print($"Tyrian Targeting: called target set to {name}.");
    }

    public void Clear()
    {
        this.configuration.CalledTargetObjectId = 0;
        this.configuration.CalledTargetName = string.Empty;
        this.configuration.Save();
        PluginServices.ChatGui.Print("Tyrian Targeting: called target cleared.");
    }

    public void MarkCalledTargets(IEnumerable<TargetCandidate> candidates)
    {
        foreach (var candidate in candidates)
            candidate.IsCalledTarget = this.configuration.CalledTargetObjectId != 0 && candidate.ObjectId == this.configuration.CalledTargetObjectId;
    }
}
