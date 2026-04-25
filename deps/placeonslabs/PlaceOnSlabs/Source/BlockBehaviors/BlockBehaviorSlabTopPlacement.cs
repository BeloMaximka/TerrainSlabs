using PlaceOnSlabs.Source.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace PlaceOnSlabs.Source.BlockBehaviors;

public class BlockBehaviorSlabTopPlacement(Block block) : BlockBehavior(block)
{
    public override bool CanAttachBlockAt(
        IBlockAccessor world,
        Block block,
        BlockPos pos,
        BlockFacing blockFace,
        ref EnumHandling handling,
        Cuboidi? attachmentArea = null
    )
    {
        if (blockFace == BlockFacing.UP)
        {
            handling = EnumHandling.PreventSubsequent;
            return SlabHelper.ShouldOffset(block.Id);
        }

        return base.CanAttachBlockAt(world, block, pos, blockFace, ref handling, attachmentArea);
    }

    public override float GetLiquidBarrierHeightOnSide(BlockFacing face, BlockPos pos, ref EnumHandling handled)
    {
        handled = EnumHandling.PreventSubsequent;
        return 0f;
    }
}
