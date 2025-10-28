using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TerrainSlabs.Source.Blocks;

public class BlockSnowSlab : Block
{
    private Block? fullBlock;

    private readonly Cuboidf[] fullBox = [new(0f, 0f, 0f, 1f, 0.5f, 1f)];

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        AssetLocation fullBlockCode = new("game", Code.Path);
        fullBlock = api.World.GetBlock(fullBlockCode);
        if (fullBlock is null)
        {
            api.Logger.Warning("Unable to get full block by code {0}", fullBlockCode);
        }
    }

    public override bool CanAcceptFallOnto(IWorldAccessor world, BlockPos pos, Block fallingBlock, TreeAttribute blockEntityAttributes)
    {
        if (fullBlock is not null)
        {
            return true;
        }
        return base.CanAcceptFallOnto(world, pos, fallingBlock, blockEntityAttributes);
    }

    public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
    {
        if (blockAccessor.GetBlockAbove(pos) is BlockLayered)
        {
            return fullBox;
        }

        return base.GetCollisionBoxes(blockAccessor, pos);
    }

    public override bool ShouldMergeFace(int facingIndex, Block neighbourBlock, int intraChunkIndex3d)
    {
        return true;
    }

    public override bool OnFallOnto(IWorldAccessor world, BlockPos pos, Block block, TreeAttribute blockEntityAttributes)
    {
        if (fullBlock is not null)
        {
            world.BlockAccessor.SetBlock(fullBlock?.BlockId ?? BlockId, pos);
            world.BlockAccessor.SetBlock(block.Id, pos.Up());
            return true;
        }

        return base.OnFallOnto(world, pos, block, blockEntityAttributes);
    }
}
