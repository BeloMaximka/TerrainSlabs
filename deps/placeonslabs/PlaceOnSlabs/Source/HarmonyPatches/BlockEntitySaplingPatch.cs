using HarmonyLib;
using PlaceOnSlabs.Source.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace PlaceOnSlabs.Source.HarmonyPatches;

// TODO: Fix trees that produce more than one block column (TreeGen?)
[HarmonyPatch]
public static class BlockEntitySaplingPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockEntitySapling), "CheckGrow")]
    public static void ReplaceSlabWithFullBlockOnGrow(BlockEntitySapling __instance)
    {
        if (__instance.Api.World.BlockAccessor.GetBlock(__instance.Pos) == __instance.Block)
        {
            return; // not grown yet
        }

        Block blockBelow = __instance.Api.World.BlockAccessor.GetBlockBelow(__instance.Pos);
        if (!SlabHelper.IsSlab(blockBelow))
        {
            return;
        }

        Block? fullBlock = __instance.Api.World.GetBlock(blockBelow.Code.Path);
        if (fullBlock is null)
        {
            __instance.Api.Logger.Warning(
                "Could not get full block {0} to replace slab when growing tree {1}",
                blockBelow.Code.Path,
                __instance.Block.Code
            );
            return;
        }

        __instance.Pos.Down();
        __instance.Api.World.BlockAccessor.SetBlock(fullBlock.BlockId, __instance.Pos);
        __instance.Pos.Up();
    }
}
