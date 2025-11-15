using TerrainSlabs.Source.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace TerrainSlabs.Source.Blocks;

public class BlockGlacierIceSlab : Block
{
    private Block? fullBlock;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        AssetLocation fullBlockCode = Code.UseFirstPartAsDomain();
        fullBlock = api.World.GetBlock(fullBlockCode);
        if (fullBlock is null)
        {
            api.Logger.Warning("Unable to get full block by code {0}", fullBlockCode);
        }
    }

    public override string GetHeldItemName(ItemStack itemStack)
    {
        return fullBlock?.GetHeldItemName(itemStack) ?? base.GetHeldItemName(itemStack);
    }

    public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
    {
        return fullBlock?.GetPlacedBlockName(world, pos) ?? base.GetPlacedBlockName(world, pos);
    }

    public override bool ShouldMergeFace(int facingIndex, Block neighbourBlock, int intraChunkIndex3d)
    {
        return neighbourBlock == this || neighbourBlock == fullBlock;
    }

    public override float GetLiquidBarrierHeightOnSide(BlockFacing face, BlockPos pos)
    {
        return 0;
    }

    public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack? byItemStack = null)
    {
        BlockTerrainSlab.FixAnimatableOffset(world, blockPos, -0.5f);
        base.OnBlockPlaced(world, blockPos, byItemStack);
    }

    public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
    {
        BlockTerrainSlab.FixAnimatableOffset(world, pos, 0.5f);
        base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
    }

    public override bool CanAttachBlockAt(
        IBlockAccessor blockAccessor,
        Block block,
        BlockPos pos,
        BlockFacing blockFace,
        Cuboidi? attachmentArea = null
    )
    {
        if (blockFace == BlockFacing.UP)
        {
            return SlabHelper.ShouldOffset(block.Id);
        }

        return base.CanAttachBlockAt(blockAccessor, block, pos, blockFace, attachmentArea);
    }

    public override bool CanAcceptFallOnto(IWorldAccessor world, BlockPos pos, Block fallingBlock, TreeAttribute blockEntityAttributes)
    {
        if (fullBlock is not null)
        {
            return true;
        }
        return base.CanAcceptFallOnto(world, pos, fallingBlock, blockEntityAttributes);
    }

    public override bool OnFallOnto(IWorldAccessor world, BlockPos pos, Block block, TreeAttribute blockEntityAttributes)
    {
        return BlockTerrainSlab.OnFallOnto(this, fullBlock, world, pos, block, blockEntityAttributes);
    }
}
