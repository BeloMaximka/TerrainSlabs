using HarmonyLib;
using System.Runtime.CompilerServices;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TerrainSlabs.Source.HarmonyPatches;

[HarmonyPatch]
public static class BlockPatch
{
    private static readonly ConditionalWeakTable<Cuboidf[], Cuboidf[]> OffsetCache = [];

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Block), nameof(Block.GetCollisionBoxes))]
    public static void OffsetColisionBox(Block __instance, ref Cuboidf[] __result, IBlockAccessor blockAccessor, BlockPos pos)
    {
        if (__result is null)
        {
            return;
        }

        pos.Down();
        if (SlabHelper.IsSlab(blockAccessor.GetBlock(pos).BlockId) && SlabHelper.ShouldOffset(__instance))
        {
            __result = OffsetCache.GetValue(
                __result,
                original =>
                {
                    var arr = new Cuboidf[original.Length];
                    for (int i = 0; i < original.Length; i++)
                    {
                        arr[i] = original[i].OffsetCopy(0, -0.5f, 0);
                    }
                    return arr;
                }
            );
        }
        pos.Up();
    }
}
