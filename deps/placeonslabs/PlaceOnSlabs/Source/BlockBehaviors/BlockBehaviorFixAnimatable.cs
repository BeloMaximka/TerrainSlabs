using HarmonyLib;
using PlaceOnSlabs.Source.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace PlaceOnSlabs.Source.BlockBehaviors;

public class BlockBehaviorFixAnimatable(Block block) : BlockBehavior(block)
{
    private float offset;

    public override void OnLoaded(ICoreAPI api)
    {
        // TODO: move this check to SlabHelper
        offset = block.Shape.Base.Path == "block/basic/slab/slab-down" ? 0.5f : -0.5f;
    }

    public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ref EnumHandling handling)
    {
        FixAnimatableOffset(world, blockPos, offset * -1);
    }

    public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier, ref EnumHandling handling)
    {
        FixAnimatableOffset(world, pos, offset);
    }

    private static void FixAnimatableOffset(IWorldAccessor world, BlockPos pos, float offset)
    {
        if (!SlabHelper.IsSlab(world.BlockAccessor.GetBlock(pos, BlockLayersAccess.SolidBlocks).BlockId))
        {
            return;
        }

        BlockEntity? be = world.BlockAccessor.GetBlockEntity(pos.Up());
        pos.Down();

        if (be is null || !SlabHelper.ShouldOffset(be.Block.Id))
        {
            return;
        }

        foreach (var behavior in be.Behaviors)
        {
            if (behavior is BEBehaviorAnimatable animatable)
            {
                Vec3d? animPos = Traverse.Create(animatable.animUtil.renderer).Field("pos").GetValue<Vec3d>();
                if (animPos is not null)
                {
                    animPos.Y += offset;
                }

                return;
            }
        }
    }
}
