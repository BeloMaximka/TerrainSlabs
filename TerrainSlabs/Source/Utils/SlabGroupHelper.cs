using System;
using System.Collections;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;

namespace TerrainSlabs.Source.Utils;

public static class SlabGroupHelper
{
    private static BitArray isSlab = null!;
    static BitArray shoulfOffset = null!;

    public static void InitFlags(ICoreAPI api)
    {
        isSlab = new(api.World.Blocks.Count);
        shoulfOffset = new(api.World.Blocks.Count);
        foreach (Block block in api.World.Blocks)
        {
            if (block.Code.Domain == "terrainslabs")
            {
                isSlab[block.BlockId] = true;
            }
            else if (ShouldOffset(api, block))
            {
                shoulfOffset[block.BlockId] = true;
            }
        }
    }

    public static bool IsSlab(int blockId)
    {
        return isSlab[blockId];
    }

    public static bool IsSlab(Block block)
    {
        return isSlab[block.BlockId];
    }

    public static bool ShouldOffset(int blockId)
    {
        return shoulfOffset[blockId];
    }

    public static bool ShouldOffset(Block block)
    {
        return shoulfOffset[block.BlockId];
    }

    public static double GetYOffsetValue(IBlockAccessor accessor, BlockPos pos)
    {
        return GetYOffsetFromBlocks(accessor.GetBlock(pos), accessor.GetBlockBelow(pos));
    }

    public static double GetYOffsetFromBlocks(Block block, Block blockBelow)
    {
        if (SlabGroupHelper.ShouldOffset(block.BlockId) && SlabGroupHelper.IsSlab(blockBelow.BlockId))
        {
            return -0.5d;
        }
        return 0;
    }

    /// <summary>
    /// This check is expensive
    /// </summary>
    private static bool ShouldOffset(ICoreAPI api, Block block)
    {
        if (block.SideSolid.Any)
        {
            return false;
        }

        if (
            block is BlockKnappingSurface
            || block is BlockClayForm
            || block is BlockMPBase
            || block is IBlockItemFlow
            || block is ITreeGenerator
            || block is BlockFruitTreePart
            || block is BlockStalagSection
            || block is BlockMicroBlock
            || block is BlockFullCoating
        )
        {
            return false;
        }

        if (block.Code.Domain == "game" && block.Code.Path.StartsWith("lognarrow")) // TODO: Move to config
        {
            return false;
        }

        foreach (var beBehavior in block.BlockEntityBehaviors)
        {
            Type? type = api.ClassRegistry.GetBlockEntityBehaviorClass(beBehavior.Name);
            if (type is not null && typeof(BEBehaviorAnimatable).IsAssignableFrom(type))
            {
                return false;
            }
        }

        return true;
    }
}
