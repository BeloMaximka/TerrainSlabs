using TerrainSlabs.Source.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace TerrainSlabs.Source.Blocks;

public class BlockTerrainSlab : Block
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
        return OnFallOnto(this, fullBlock, world, pos, block, blockEntityAttributes);
    }

    public static bool OnFallOnto(Block slab, Block? fullBlock, IWorldAccessor world, BlockPos pos, Block block, TreeAttribute blockEntityAttributes)
    {
        if (!SlabHelper.ShouldOffset(block.Id) && fullBlock is not null)
        {
            world.BlockAccessor.SetBlock(fullBlock?.BlockId ?? slab.BlockId, pos);
        }

        pos.Up();
        world.BlockAccessor.SetBlock(block.Id, pos);
        if (block.EntityClass != null)
        {
            BlockEntity? blockEntity = world.BlockAccessor.GetBlockEntity(pos);
            blockEntityAttributes.SetInt("posx", pos.X);
            blockEntityAttributes.SetInt("posy", pos.Y);
            blockEntityAttributes.SetInt("posz", pos.Z);
            blockEntity.FromTreeAttributes(blockEntityAttributes, world);
        }

        return true;
    }
}
