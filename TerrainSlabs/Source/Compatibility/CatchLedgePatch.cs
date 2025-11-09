using HarmonyLib;
using System;
using System.Reflection;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.Common;

namespace TerrainSlabs.Source.Compatibility;

public static class CatchLedgePatch
{
    public static void ApplyIfEnabled(ICoreAPI api, Harmony harmonyInstance)
    {
        if (api.ModLoader.IsModEnabled("catchledge"))
        {
            ApplyHarmonyPatch(api, harmonyInstance);
        }
    }

    private static void ApplyHarmonyPatch(ICoreAPI api, Harmony harmonyInstance)
    {
        MethodInfo postfix = AccessTools.Method(typeof(CatchLedgePatch), nameof(IsFullCubePostfix));

        string typeName = "CatchingLedge.LedgeDetector, CatchLedge";
        Type? type = Type.GetType(typeName);
        if (type is null)
        {
            api.Logger.Warning("Could not find type {0}", typeName);
            return;
        }

        MethodInfo original = AccessTools.Method(type, "IsFullCube");
        harmonyInstance.Patch(original, postfix: postfix);
    }

    private static void IsFullCubePostfix(ref bool __result, Block b)
    {
        __result = __result || SlabHelper.IsSlab(b.BlockId);
    }
}
