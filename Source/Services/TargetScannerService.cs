using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using TyrianTargeting.Models;

namespace TyrianTargeting.Services;

public sealed class TargetScannerService
{
    private readonly Configuration configuration;

    public TargetScannerService(Configuration configuration)
    {
        this.configuration = configuration;
    }

    public IReadOnlyList<TargetCandidate> Scan()
    {
        var results = new List<TargetCandidate>();

        try
        {
            if (!PluginServices.ClientState.IsLoggedIn)
                return results;

            var localPlayer = PluginServices.ObjectTable.LocalPlayer;
            if (localPlayer is null || !localPlayer.IsValid())
                return results;

            foreach (var obj in PluginServices.ObjectTable)
            {
                if (obj is null)
                    continue;

                if (!obj.IsValid())
                    continue;

                if (obj.GameObjectId == localPlayer.GameObjectId)
                    continue;

                if (obj.GameObjectId == 0)
                    continue;

                var name = obj.Name.TextValue;
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var distance = Vector3.Distance(localPlayer.Position, obj.Position);
                var relation = CalculateFacingRelation(localPlayer, obj);
                var hpPercent = ReadHpPercent(obj);
                var kindName = obj.ObjectKind.ToString();

                var isBattleNpc = obj.ObjectKind == ObjectKind.BattleNpc;
                var isPlayer = obj.ObjectKind == ObjectKind.Pc;
                var isHostile = isBattleNpc || (PluginServices.ClientState.IsPvP && isPlayer);
                var isPetOrMinion = IsPetOrMinion(obj, kindName);
                var isTrainingDummy = IsTrainingDummy(name);
                var isInCombat = IsProbablyInCombat(localPlayer, obj, hpPercent);

                results.Add(new TargetCandidate
                {
                    ObjectId = obj.GameObjectId,
                    Name = name,
                    ObjectKindName = kindName,
                    Position = obj.Position,
                    Distance = distance,
                    CameraScore = relation.CenterScore,
                    HpPercent = hpPercent,
                    ScreenX = relation.ScreenOrder,
                    IsHostile = isHostile,
                    IsDead = obj.IsDead,
                    IsTargetable = obj.IsTargetable,
                    IsInCombat = isInCombat,
                    IsInCameraCone = relation.InFront,
                    IsBehindPlayer = !relation.InFront,
                    IsPetOrMinion = isPetOrMinion,
                    IsTrainingDummy = isTrainingDummy,
                });
            }
        }
        catch (Exception ex)
        {
            PluginServices.Log.Error(ex, "Tyrian Targeting failed while scanning the live object table.");
        }

        return results;
    }

    private static (float CenterScore, float ScreenOrder, bool InFront) CalculateFacingRelation(IGameObject localPlayer, IGameObject target)
    {
        var toTarget = target.Position - localPlayer.Position;
        toTarget.Y = 0f;

        if (toTarget.LengthSquared() <= 0.001f)
            return (1f, 0f, true);

        toTarget = Vector3.Normalize(toTarget);

        // FFXIV actor rotation is radians. This approximates "camera center" with the player's facing.
        // It is intentionally isolated here so we can upgrade to true camera/screen scoring later.
        var forward = new Vector3(MathF.Sin(localPlayer.Rotation), 0f, MathF.Cos(localPlayer.Rotation));
        var right = new Vector3(forward.Z, 0f, -forward.X);

        var dot = Math.Clamp(Vector3.Dot(forward, toTarget), -1f, 1f);
        var side = Math.Clamp(Vector3.Dot(right, toTarget), -1f, 1f);
        var centerScore = MathF.Max(0f, (dot + 1f) * 0.5f);

        return (centerScore, side, dot >= 0f);
    }

    private static float ReadHpPercent(IGameObject obj)
    {
        try
        {
            var type = obj.GetType();
            var currentProp = type.GetProperty("CurrentHp");
            var maxProp = type.GetProperty("MaxHp");

            if (currentProp is null || maxProp is null)
                return obj.IsDead ? 0f : 100f;

            var current = Convert.ToSingle(currentProp.GetValue(obj));
            var max = Convert.ToSingle(maxProp.GetValue(obj));

            if (max <= 0f)
                return obj.IsDead ? 0f : 100f;

            return Math.Clamp((current / max) * 100f, 0f, 100f);
        }
        catch
        {
            return obj.IsDead ? 0f : 100f;
        }
    }

    private static bool IsProbablyInCombat(IGameObject localPlayer, IGameObject target, float hpPercent)
    {
        if (target.TargetObjectId == localPlayer.GameObjectId)
            return true;

        if (localPlayer.TargetObjectId == target.GameObjectId)
            return true;

        return hpPercent is > 0f and < 100f;
    }

    private static bool IsPetOrMinion(IGameObject obj, string kindName)
    {
        if (obj.OwnerId != 0 && obj.OwnerId != 0xE0000000)
            return true;

        return kindName.Contains("Companion", StringComparison.OrdinalIgnoreCase)
            || kindName.Contains("Ornament", StringComparison.OrdinalIgnoreCase)
            || kindName.Contains("Mount", StringComparison.OrdinalIgnoreCase)
            || kindName.Contains("Minion", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTrainingDummy(string name)
    {
        return name.Contains("Striking Dummy", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Training Dummy", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Stone, Sky, Sea", StringComparison.OrdinalIgnoreCase);
    }
}
