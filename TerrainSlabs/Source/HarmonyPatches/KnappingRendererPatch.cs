using HarmonyLib;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TerrainSlabs.Source.HarmonyPatches;

[HarmonyPatch]
public static class KnappingRendererPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BlockEntityKnappingSurface), "spawnParticles")]
    public static bool OffsetParticlesForSlabs(BlockEntityKnappingSurface __instance, Vec3d pos)
    {
        if (SlabHelper.IsSlab(__instance.Api.World.BlockAccessor.GetBlockBelow(pos.AsBlockPos).Id))
        {
            pos.Y -= 0.5f;
        }
        return true;
    }
}
