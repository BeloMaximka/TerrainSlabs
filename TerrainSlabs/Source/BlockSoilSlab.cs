using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TerrainSlabs.Source;

public class BlockSoilSlab : BlockSoil
{
    public override bool CanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
    {
        if (block.BlockMaterial == EnumBlockMaterial.Snow)
        {
            // TODO: Adde snowlayeroffset and replace falling layer
            return true;
        }

        return base.CanAttachBlockAt(blockAccessor, block, pos, blockFace, attachmentArea);
    }

    public override bool CanAcceptFallOnto(IWorldAccessor world, BlockPos pos, Block fallingBlock, TreeAttribute blockEntityAttributes)
    {
        if (fallingBlock.BlockMaterial == EnumBlockMaterial.Snow)
        {
            // TODO: Adde snowlayeroffset and replace falling layer
            return true;
        }

        return base.CanAcceptFallOnto(world, pos, fallingBlock, blockEntityAttributes);
    }

    public override bool OnFallOnto(IWorldAccessor world, BlockPos pos, Block block, TreeAttribute blockEntityAttributes)
    {
        if (block.BlockMaterial == EnumBlockMaterial.Snow)
        {
            // TODO: Adde snowlayeroffset and replace falling layer
            Block blockToPlace = block;
            Block blockAbove = world.BlockAccessor.GetBlock(pos.Up());
            if (blockAbove.BlockMaterial == EnumBlockMaterial.Snow || blockAbove.BlockMaterial == EnumBlockMaterial.Plant)
            {
                blockToPlace = blockAbove.GetSnowCoveredVariant(pos, blockAbove.GetSnowLevel(pos) + 1);
                if (blockToPlace is null)
                {
                    return false;
                }
            }
            world.BlockAccessor.SetBlock(blockToPlace.Id, pos);
            return true;
        }

        return base.OnFallOnto(world, pos, block, blockEntityAttributes);
    }
}
