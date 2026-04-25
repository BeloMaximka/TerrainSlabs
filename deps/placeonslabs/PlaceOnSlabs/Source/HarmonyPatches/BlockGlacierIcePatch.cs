using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace PlaceOnSlabs.Source.HarmonyPatches;

[HarmonyPatch]
public static class BlockGlacierIcePatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockGlacierIce), nameof(BlockGlacierIce.ShouldMergeFace))]
    public static void CheckGlacierSlab(BlockGlacierIce __instance, ref bool __result, int facingIndex, Block neighbourblock)
    {
        __result = __result || (facingIndex == BlockFacing.indexUP && neighbourblock.Code.Path == __instance.Code.Path);
    }
}
