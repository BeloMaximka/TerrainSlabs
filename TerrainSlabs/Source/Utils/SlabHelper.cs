using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TerrainSlabs.Source.Systems;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;

namespace TerrainSlabs.Source.Utils;

public static class SlabHelper
{
    private static BitArray isSlab = null!;
    private static BitArray shoulfOffset = null!;

    public static void InitFlags(ICoreAPI api)
    {
        var blacklist = api.ModLoader.GetModSystem<ConfigSystem>().ServerSettings.OffsetBlacklist.Select(item => (AssetLocation)item);
        isSlab = new(api.World.Blocks.Count);
        shoulfOffset = new(api.World.Blocks.Count);
        foreach (Block block in api.World.Blocks)
        {
            if (block.Code.Domain == "terrainslabs")
            {
                isSlab[block.BlockId] = true;
            }
            else if (ShouldOffset(api, block, blacklist))
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
        if (SlabHelper.ShouldOffset(block.BlockId) && SlabHelper.IsSlab(blockBelow.BlockId))
        {
            return -0.5d;
        }
        return 0;
    }

    /// <summary>
    /// This check is expensive
    /// </summary>
    private static bool ShouldOffset(ICoreAPI api, Block block, IEnumerable<AssetLocation> blacklist)
    {
        if (block.SideSolid.Any)
        {
            return false;
        }

        if (
            block is BlockMPBase
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

        if (block.Variant.TryGetValue("part", out string _))
        {
            return false;
        }

        foreach (var beBehavior in block.BlockEntityBehaviors)
        {
            if (beBehavior.Name == "Door")
            {
                return false;
            }
        }

        if (blacklist.Any(item => WildcardUtil.Match(item, block.Code)))
        {
            return false;
        }

        return true;
    }
}
