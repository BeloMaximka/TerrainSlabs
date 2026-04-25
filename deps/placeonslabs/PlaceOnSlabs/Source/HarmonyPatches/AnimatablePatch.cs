using HarmonyLib;
using PlaceOnSlabs.Source.Utils;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace PlaceOnSlabs.Source.HarmonyPatches;

[HarmonyPatch]
public static class AnimatablePatch
{
    [HarmonyPostfix]
    [HarmonyPatch(
        typeof(AnimatableRenderer),
        methodType: MethodType.Constructor,
        typeof(ICoreClientAPI),
        typeof(Vec3d),
        typeof(Vec3f),
        typeof(AnimatorBase),
        typeof(Dictionary<string, AnimationMetaData>),
        typeof(MeshData),
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
