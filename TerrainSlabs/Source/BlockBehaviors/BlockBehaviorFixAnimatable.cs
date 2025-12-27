using HarmonyLib;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TerrainSlabs.Source.BlockBehaviors;

public class BlockBehaviorFixAnimatable(Block block) : BlockBehavior(block)
{
    private float offset;

    public override void OnLoaded(ICoreAPI api)
    {
        offset = SlabHelper.IsSlab(block) ? 0.5f : -0.5f;
    }

    public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ref EnumHandling handling)
    {
        FixAnimatableOffset(world, blockPos, offset * -1);
    }

    public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
    {
        FixAnimatableOffset(world, pos, offset);
    }

    private static void FixAnimatableOffset(IWorldAccessor world, BlockPos pos, float offset)
    {
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
