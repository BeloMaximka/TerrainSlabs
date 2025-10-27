using TerrainSlabs.Source.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TerrainSlabs.Source.BlockBehaviors;

public class BlockBehaviorRestrictTopAttachment(Block block) : BlockBehavior(block)
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
        handling = EnumHandling.PreventSubsequent;

        return SlabGroupHelper.ShouldOffset(block.BlockId);
    }
}
