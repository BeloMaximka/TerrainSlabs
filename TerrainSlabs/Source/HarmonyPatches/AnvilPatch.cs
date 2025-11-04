using HarmonyLib;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TerrainSlabs.Source.HarmonyPatches;

[HarmonyPatch]
public static class AnvilPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockEntityAnvil), nameof(BlockEntityAnvil.Initialize))]
    public static void OffsetParticlesOnSlab(BlockEntityAnvil __instance, ref float ___voxYOff, ICoreAPI ___Api)
    {
        if (SlabHelper.IsSlab(___Api.World.BlockAccessor.GetBlockBelow(__instance.Pos).Id))
        {
            ___voxYOff -= 0.5f;
        }
    }
}
