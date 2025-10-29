using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;

namespace TerrainSlabs.Source.Utils;

public static class SlabHelper
{
    private static readonly HashSet<int> offsetBlacklist = [];
    private static BitArray isSlab = null!;
    private static BitArray shoulfOffset = null!;

    public static void InitBlacklist(ICoreAPI api, IEnumerable<string> blacklist)
    {
        foreach (string wildcard in blacklist)
        {
            Block[] blocks = api.World.SearchBlocks(wildcard);
            if (blocks.Length == 0)
            {
                api.Logger.Warning("No blocks found for offsset blacklisting by code {0}", wildcard);
                continue;
            }
            foreach (int id in blocks.Select(block => block.Id))
            {
                AddToOffsetBlacklist(id);
            }
        }
    }

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

    public static void AddToOffsetBlacklist(int blockId)
    {
        offsetBlacklist.Add(blockId);
        shoulfOffset[blockId] = false;
    }

    public static void RemoveFromOffsetBlacklist(ICoreAPI api, Block block)
    {
        offsetBlacklist.Remove(block.Id);
        shoulfOffset[block.Id] = ShouldOffset(api, block);
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
