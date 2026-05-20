using System.Numerics;

namespace TyrianTargeting.Models;

public sealed class TargetCandidate
{
    public ulong ObjectId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ObjectKindName { get; init; } = string.Empty;
    public Vector3 Position { get; init; }

    public float Distance { get; init; }
    public float CameraScore { get; init; }
    public float HpPercent { get; init; }
    public float ScreenX { get; init; }

    public bool IsHostile { get; init; }
    public bool IsDead { get; init; }
    public bool IsTargetable { get; init; }
    public bool IsInCombat { get; init; }
    public bool IsInCameraCone { get; init; } = true;
    public bool IsBehindPlayer { get; init; }
    public bool IsPetOrMinion { get; init; }
    public bool IsTrainingDummy { get; init; }

    public bool IsCalledTarget { get; set; }
    public bool IsSoftPreview { get; set; }
    public float FinalScore { get; set; }
}
