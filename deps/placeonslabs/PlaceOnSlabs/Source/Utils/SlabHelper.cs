using PlaceOnSlabs.Source.Systems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;

namespace PlaceOnSlabs.Source.Utils;

public static class Offsets
{
    public const byte full_block = 0;
    public const byte slab = 8;
    public const byte layer_16_16 = 0;
    public const byte layer_15_16 = 1;
    public const byte layer_14_16 = 2;
    public const byte layer_13_16 = 3;
    public const byte layer_12_16 = 4;
    public const byte layer_11_16 = 5;
    public const byte layer_10_16 = 6;
    public const byte layer_9_16 = 7;
    public const byte layer_8_16 = 8;
    public const byte layer_7_16 = 9;
    public const byte layer_6_16 = 10;
    public const byte layer_5_16 = 11;
    public const byte layer_4_16 = 12;
    public const byte layer_3_16 = 13;
    public const byte layer_2_16 = 14;
    public const byte layer_1_16 = 15;
}

public static class SlabHelper
{
    // Per block
    public static PackedOffsetArray offset = null!;
    public static BitArray shouldOffset = null!;

    public static void InitFlags(ICoreAPI api)
    {
        var blacklist = api.ModLoader.GetModSystem<ConfigSystem>().ServerSettings.OffsetBlacklist.Select(item => (AssetLocation)item);
        offset = new(api.World.Blocks.Count);
        shouldOffset = new(api.World.Blocks.Count);
        foreach (Block block in api.World.Blocks)
        {
            offset[block.BlockId] = CalculateOffset(block);
            shouldOffset[block.BlockId] = ShouldOffset(block, blacklist);
        }
    }

    public static byte CalculateOffset(Block block)
    {
        return block.Shape.Base.Path switch
        {
            "block/basic/slab/slab-down" => Offsets.slab,
            "block/basic/submersiblelayers/2voxel" => Offsets.layer_2_16,
            "block/basic/submersiblelayers/4voxel" => Offsets.layer_4_16,
            "block/basic/submersiblelayers/6voxel" => Offsets.layer_6_16,
            "block/basic/slab/stonepath-slab-free" => Offsets.layer_7_16,
            "block/basic/slab/stonepath-snow" => Offsets.layer_7_16,
            "block/basic/submersiblelayers/8voxel" => Offsets.layer_8_16,
            "block/basic/submersiblelayers/10voxel" => Offsets.layer_10_16,
            "block/basic/submersiblelayers/12voxel" => Offsets.layer_12_16,
            "block/basic/submersiblelayers/14voxel" => Offsets.layer_14_16,
            _ => Offsets.full_block
        };
    }

    /// <returns>Negative Y value which represents how much should block above be offset</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GetYOffset(int blockId) => offset[blockId] / -16d;

    /// <returns>Negative Y value which represents how much should block above be offset</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetYOffsetFloat(int blockId) => offset[blockId] / -16f;

    /// <returns>Negative Y value which represents how much should block at pos be offset</returns>
    public static double GetPlacementYOffsetAtPos(IBlockAccessor accessor, BlockPos pos)
    {
        return GetPlacementYOffsetFromBlocks(accessor.GetBlock(pos), accessor.GetBlockBelow(pos, 1, BlockLayersAccess.MostSolid));
    }

    public static double GetPlacementYOffsetFromBlocks(Block block, Block blockBelow) =>
        shouldOffset[block.BlockId] ? GetYOffset(blockBelow.BlockId) : 0;

    // Naming is wonky because Harmony doesn't like overloading
    public static double GetPlacementYOffsetAtPosWithFluids(Block block, Block blockBelow, Block fluidBlockBelow) =>
        shouldOffset[block.BlockId] && !fluidBlockBelow.SideSolid[BlockFacing.indexUP] ? GetYOffset(blockBelow.BlockId) : 0;

    /// <summary>
    /// This check is expensive
    /// </summary>
    private static bool ShouldOffset(Block block, IEnumerable<AssetLocation> blacklist)
    {
        if (block.SideSolid.Any)
        {
            return false;
        }

        if (block.DrawType == EnumDrawType.SurfaceLayer)
        {
            // decors
            return false;
        }

        if (block is BlockGroundAndSideAttachable && !block.Code.Path.EndsWith("up"))
        {
            // torches, oil lamps etc
            return false;
        }

        if (block.BlockBehaviors.Any(behavior => behavior is BlockBehaviorHorizontalAttachable || behavior is BlockBehaviorLadder))
        {
            // toolracks, shelves etc
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
            || block is BlockAntlerMount
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
