using TyrianTargeting.Models;

namespace TyrianTargeting.Services;

public sealed class SoftTargetPreviewService
{
    private readonly Configuration configuration;

    public SoftTargetPreviewService(Configuration configuration)
    {
        this.configuration = configuration;
    }

    public TargetCandidate? CurrentPreview { get; private set; }

    public void UpdatePreview(IReadOnlyList<TargetCandidate> sortedTargets)
    {
        foreach (var target in sortedTargets)
            target.IsSoftPreview = false;

        if (!this.configuration.ShowSoftTargetPreview || sortedTargets.Count == 0)
        {
            this.CurrentPreview = null;
            return;
        }

        this.CurrentPreview = sortedTargets[0];
        this.CurrentPreview.IsSoftPreview = true;
    }

    public void Clear()
    {
        this.CurrentPreview = null;
    }
}
