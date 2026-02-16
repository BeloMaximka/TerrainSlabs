using HarmonyLib;
using System;
using System.Reflection;
using Vintagestory.API.Common;

namespace TerrainSlabs.Source.Compatibility;

public static class DanaTweaksPatch
{
    public static void ApplyIfEnabled(ICoreAPI api, Harmony harmonyInstance)
    {
        if (api.ModLoader.IsModEnabled("danatweaks"))
        {
            ApplyHarmonyPatch(api, harmonyInstance);
        }
    }

    private static void ApplyHarmonyPatch(ICoreAPI api, Harmony harmonyInstance)
    {
        MethodInfo prefix = AccessTools.Method(typeof(DanaTweaksPatch), nameof(PatchEverySoilUnstablePrefix));

        string typeName = "DanaTweaks.BlockPatches, DanaTweaks";
        Type? type = Type.GetType(typeName);
        if (type is null)
        {
            api.Logger.Warning("Could not find type {0}", typeName);
            return;
        }

        MethodInfo original = AccessTools.Method(type, "PatchEverySoilUnstable");
        harmonyInstance.Patch(original, prefix: prefix);
    }

    private static bool PatchEverySoilUnstablePrefix(Block block)
    {
        if (block.Code.Domain == "terrainslabs")
        {
            return false;
        }

        return true;
    }
}
