using HarmonyLib;
using PlaceOnSlabs.Source.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace PlaceOnSlabs.Source.BlockBehaviors;

public class BlockBehaviorFixAnimatable(Block block) : BlockBehavior(block)
{
    public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ref EnumHandling handling)
    {
        FixAnimatableOffset(world, blockPos);
    }

    public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
    {
        FixAnimatableOffset(world, pos, -1f);
    }

    private static void FixAnimatableOffset(IWorldAccessor world, BlockPos pos, float inverseModifier = 1f)
    {
        float offset = SlabHelper.GetYOffsetFloat(world.BlockAccessor.GetBlock(pos, BlockLayersAccess.SolidBlocks).BlockId);
        if (offset == 0)
        {
            return;
        }

        BlockEntity? be = world.BlockAccessor.GetBlockEntity(pos.Up());
        pos.Down();

        if (be is null || !SlabHelper.shouldOffset[be.Block.Id])
        {
            return;
        }

        foreach (var behavior in be.Behaviors)
        {
            if (behavior is BEBehaviorAnimatable animatable)
            {
                Vec3d? animPos = Traverse.Create(animatable.animUtil.renderer).Field("pos").GetValue<Vec3d>();
                animPos?.Y += offset * inverseModifier;
                return;
            }
        }
    }
}
