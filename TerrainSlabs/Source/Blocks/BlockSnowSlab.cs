using TerrainSlabs.Source.Utils;
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

        AssetLocation fullBlockCode = Code.UseFirstPartAsDomain();
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
        return BlockTerrainSlab.OnFallOnto(this, fullBlock, world, pos, block, blockEntityAttributes);
    }

    public override Block GetSnowCoveredVariant(BlockPos pos, float snowLevel)
    {
        if (snowLevel <= 0) return api.World.Blocks[0];
        if ((int)snowLevel == 4) return this;
        // The next line is possible null reference but I have no idea how the game expects me to handle it 
        // Also this line is idetical to BlockSnow.cs
        return api.World.GetBlock("game:snowlayer-" + (int)snowLevel)!; 
    }
}
