using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TerrainSlabs.Source;

public class SoilSlab : BlockSoil
{
    public override bool CanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
    {
        if (block.Code.Path.StartsWithFast("snowlayer"))
        {
            // TODO: Adde snowlayeroffset and replace falling layer
            return true;
        }

        return base.CanAttachBlockAt(blockAccessor, block, pos, blockFace, attachmentArea);
    }
}
