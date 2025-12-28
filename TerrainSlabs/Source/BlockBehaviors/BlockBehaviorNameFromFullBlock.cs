using System.Text;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TerrainSlabs.Source.BlockBehaviors;

public class BlockBehaviorNameFromFullBlock(Block block) : BlockBehavior(block)
{
    private Block? fullBlock;

    public override void OnLoaded(ICoreAPI api)
    {
        AssetLocation fullBlockCode = block.Code.UseFirstPartAsDomain();
        fullBlock = api.World.GetBlock(fullBlockCode);
        if (fullBlock is null)
        {
            api.Logger.Warning(
                "[terrainslabs] Unable to get full block in {0} by code {1}",
                nameof(BlockBehaviorNameFromFullBlock),
                fullBlockCode
            );
        }
    }

    public override void GetHeldItemName(StringBuilder sb, ItemStack itemStack)
    {
        if (fullBlock is not null)
        {
            sb.Clear();
            sb.Append(fullBlock.GetHeldItemName(itemStack));
        }
    }

    public override void GetPlacedBlockName(StringBuilder sb, IWorldAccessor world, BlockPos pos)
    {
        if (fullBlock is not null)
        {
            sb.Clear();
            sb.Append(fullBlock.GetPlacedBlockName(world, pos));
        }
    }
}
