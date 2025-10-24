using HarmonyLib;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

#pragma warning disable S101 // Types should be named in PascalCase

namespace TerrainSlabs.Source.HarmonyPatches;

[HarmonyPatch(typeof(AABBIntersectionTest), nameof(AABBIntersectionTest.RayIntersectsBlockSelectionBox))]
public static class AABBIntersectionTestPatch
{
    [HarmonyPrefix]
    public static bool OffsetSelectionBoxes(
        AABBIntersectionTest __instance,
        BlockPos pos,
        BlockFilter filter,
        bool testCollide,
        ref bool __result,
        ref Cuboidd ___tmpCuboidd,
        ref BlockFacing ___hitOnBlockFaceTmp,
        ref Vec3d ___hitPositionTmp,
        ref Block ___blockIntersected
    )
    {
        Block blockBelow = __instance.bsTester.blockAccessor.GetBlock(pos.Down());
        if (!SlabGroupHelper.IsSlab(blockBelow.BlockId))
        {
            pos.Up();
            return true;
        }
        pos.Up();

        Block block = __instance.bsTester.blockAccessor.GetBlock(pos, 2);
        Cuboidf[] cuboidfArray;

        if (block.SideSolid.Any)
        {
            cuboidfArray = testCollide
                ? block.GetCollisionBoxes(__instance.bsTester.blockAccessor, pos)
                : block.GetSelectionBoxes(__instance.bsTester.blockAccessor, pos);
        }
        else
        {
            block = __instance.bsTester.GetBlock(pos);
            cuboidfArray = testCollide
                ? block.GetCollisionBoxes(__instance.bsTester.blockAccessor, pos)
                : __instance.bsTester.GetBlockIntersectionBoxes(pos);
        }

        if (cuboidfArray == null || (filter != null && !filter(pos, block)))
        {
            __result = false;
            return false;
        }

        bool found = false;
        bool foundIsDecor = false;

        for (int index = 0; index < cuboidfArray.Length; ++index)
        {
            ___tmpCuboidd.Set(cuboidfArray[index]).Translate(pos.X, pos.InternalY - 0.5f, pos.Z);

            if (__instance.RayIntersectsWithCuboid(___tmpCuboidd, ref ___hitOnBlockFaceTmp, ref ___hitPositionTmp))
            {
                bool isDecor = cuboidfArray[index].GetType().Name == "DecorSelectionBox"; // internal type workaround

                if (
                    !found
                    || !(!foundIsDecor | isDecor)
                    || __instance.hitPosition.SquareDistanceTo(__instance.ray.origin)
                        > ___hitPositionTmp.SquareDistanceTo(__instance.ray.origin)
                )
                {
                    __instance.hitOnSelectionBox = index;
                    found = true;
                    foundIsDecor = isDecor;
                    __instance.hitOnBlockFace = ___hitOnBlockFaceTmp;
                    __instance.hitPosition.Set(___hitPositionTmp);
                }
            }
        }

        // Handle DecorSelectionBox.PosAdjust via reflection
        if (found && cuboidfArray[__instance.hitOnSelectionBox].GetType().Name == "DecorSelectionBox")
        {
            var decorType = cuboidfArray[__instance.hitOnSelectionBox].GetType();
            var posAdjustProp = decorType.GetProperty("PosAdjust");
            Vec3i? posAdjust = posAdjustProp?.GetValue(cuboidfArray[__instance.hitOnSelectionBox]) as Vec3i;

            if (posAdjust != null)
            {
                pos.Add(posAdjust);
                block = __instance.bsTester.GetBlock(pos);
            }
        }

        if (found)
            ___blockIntersected = block;

        __result = found;
        return false; // skip original
    }
}
