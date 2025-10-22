using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace TerrainSlabs.Source;

public static class TerrainReplaceUtil
{
    // TODO: move to config and make it not look like a mess
    private static readonly List<string> blocksToReplace =
    [
        "soil-*",
        "forestfloor-*",
        "gravel-andesite",
        "gravel-chalk",
        "gravel-chert",
        "gravel-conglomerate",
        "gravel-limestone",
        "gravel-claystone",
        "gravel-granite",
        "gravel-sandstone",
        "gravel-shale",
        "gravel-basalt",
        "gravel-peridotite",
        "gravel-phyllite",
        "gravel-slate",
        "gravel-bauxite",
        "sand-andesite",
        "sand-chalk",
        "sand-chert",
        "sand-conglomerate",
        "sand-limestone",
        "sand-claystone",
        "sand-granite",
        "sand-sandstone",
        "sand-shale",
        "sand-basalt",
        "sand-peridotite",
        "sand-phyllite",
        "sand-slate",
        "sand-bauxite",
    ];
    private static readonly List<string> topObjectToOffset = ["tallgrass-*"];

    public static Dictionary<int, int> GetTerrainReplacementMap(ICoreAPI api) =>
        GetBlockToReplaceMap(api, "TSblocksToReplace", blocksToReplace);

    public static Dictionary<int, int> GetTopReplacementMap(ICoreAPI api) =>
        GetBlockToReplaceMap(api, "TStopObjectToOffset", topObjectToOffset);

    private static Dictionary<int, int> GetBlockToReplaceMap(ICoreAPI api, string cacheKey, IEnumerable<string> codes)
    {
        return ObjectCacheUtil.GetOrCreate<Dictionary<int, int>>(
            api,
            cacheKey,
            () =>
            {
                Dictionary<int, int> result = [];
                foreach (var code in codes)
                {
                    foreach (var block in api.World.SearchBlocks(code))
                    {
                        AssetLocation slabCode = new(block.Code) { Domain = "terrainslabs" };
                        Block? slabBlock = api.World.GetBlock(slabCode);
                        if (slabBlock is null)
                        {
                            api.Logger.Warning("Unable to find slab block alternative with code {0}", slabCode);
                            continue;
                        }
                        result.Add(block.Id, slabBlock.Id);
                    }
                }
                return result;
            }
        );
    }
}

public class TerrainSlabReplacer(ICoreAPI api, IBlockAccessor accessor)
{
    private readonly BlockPos posBuffer = new(0);
    private readonly Dictionary<int, int> terrainReplacementMap = TerrainReplaceUtil.GetTerrainReplacementMap(api);
    private readonly Dictionary<int, int> topReplacementMap = TerrainReplaceUtil.GetTopReplacementMap(api);

    public void TryReplaceWithSlab(BlockPos pos)
    {
        posBuffer.X = pos.X;
        posBuffer.Y = pos.Y;
        posBuffer.Z = pos.Z;
        posBuffer.dimension = pos.dimension;

        if (terrainReplacementMap.TryGetValue(accessor.GetBlock(posBuffer).Id, out int slabId) && slabId != 0 && HasExposedSide(posBuffer))
        {
            accessor.SetBlock(slabId, posBuffer);

            posBuffer.Y++;
            if (topReplacementMap.TryGetValue(accessor.GetBlock(posBuffer).Id, out int blockWithOffsetId) && slabId != 0)
            {
                accessor.SetBlock(blockWithOffsetId, posBuffer);
            }
        }
    }

    private bool HasExposedSide(BlockPos pos)
    {
        pos.X++;
        if (IsExposeBlock(pos, BlockFacing.indexEAST))
        {
            pos.X--;
            return true;
        }
        pos.X -= 2;

        if (IsExposeBlock(pos, BlockFacing.indexWEST))
        {
            pos.X++;
            return true;
        }
        pos.X++;

        pos.Z++;
        if (IsExposeBlock(pos, BlockFacing.indexSOUTH))
        {
            pos.Z--;
            return true;
        }
        pos.Z -= 2;

        if (IsExposeBlock(pos, BlockFacing.indexNORTH))
        {
            pos.Z++;
            return true;
        }
        pos.Z++;

        return false;
    }

    private bool IsExposeBlock(BlockPos pos, int faceIndex)
    {
        Block block = accessor.GetBlock(pos);
        return block.Code.Domain != "terrainslabs" && !block.SideSolid[faceIndex] && block.MatterState != EnumMatterState.Liquid;
    }
}
