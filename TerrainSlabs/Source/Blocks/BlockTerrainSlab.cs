using HarmonyLib;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

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

    public override float GetLiquidBarrierHeightOnSide(BlockFacing face, BlockPos pos)
    {
        return 0;
    }

    public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack? byItemStack = null)
    {
        FixAnimatableOffset(world, blockPos, -0.5f);
        base.OnBlockPlaced(world, blockPos, byItemStack);
    }

    public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
    {
        FixAnimatableOffset(world, pos, 0.5f);
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
        return OnFallOnto(this, fullBlock, world, pos, block, blockEntityAttributes);
    }

    public static void FixAnimatableOffset(IWorldAccessor world, BlockPos pos, float offset)
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

    public static bool OnFallOnto(
        Block slab,
        Block? fullBlock,
        IWorldAccessor world,
        BlockPos pos,
        Block block,
        TreeAttribute blockEntityAttributes
    )
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
