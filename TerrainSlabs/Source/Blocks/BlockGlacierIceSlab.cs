﻿using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace TerrainSlabs.Source.Blocks;

public class BlockGlacierIceSlab : Block
{
    private Block? fullBlock;

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

    public override bool ShouldMergeFace(int facingIndex, Block neighbourBlock, int intraChunkIndex3d)
    {
        return neighbourBlock == this || neighbourBlock == fullBlock;
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
        if (fullBlock is not null)
        {
            world.BlockAccessor.SetBlock(fullBlock?.BlockId ?? BlockId, pos);
            world.BlockAccessor.SetBlock(block.Id, pos.Up());
            return true;
        }

        return base.OnFallOnto(world, pos, block, blockEntityAttributes);
    }
}
