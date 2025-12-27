using HarmonyLib;
using System.Collections.Generic;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TerrainSlabs.Source.HarmonyPatches;

[HarmonyPatch]
public static class AnimatablePatch
{
    [HarmonyPostfix]
    [HarmonyPatch(
        typeof(AnimatableRenderer),
        methodType: MethodType.Constructor,
        typeof(Vec3d),
        typeof(Vec3f),
        typeof(AnimatorBase),
        typeof(Dictionary<string, AnimationMetaData>),
        typeof(MultiTextureMeshRef),
        typeof(EnumRenderStage)
    )]
    [HarmonyPatch(
        typeof(AnimatableRenderer),
        methodType: MethodType.Constructor,
        typeof(ICoreClientAPI),
        typeof(Vec3d),
        typeof(Vec3f),
        typeof(AnimatorBase),
        typeof(Dictionary<string, AnimationMetaData>),
        typeof(MultiTextureMeshRef),
        typeof(EnumRenderStage)
    )]
    public static void OffsetAnimatableOnSlab(ref Vec3d ___pos, ICoreClientAPI ___capi)
    {
        BlockPos pos = ___pos.AsBlockPos;
        if (
            SlabHelper.ShouldOffset(___capi.World.BlockAccessor.GetBlockId(pos))
            && SlabHelper.IsSlab(___capi.World.BlockAccessor.GetBlock(pos.Down(), BlockLayersAccess.MostSolid).BlockId)
        )
        {
            ___pos.Y -= 0.5f;
        }
    }
}
