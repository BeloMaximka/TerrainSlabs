using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace TerrainSlabs.Source;

public static class TerrainReplaceUtil
{
    // TODO: move to config
    private static readonly List<string> blocksToReplace = ["soil-*", "sand-*", "gravel-*"];
    private static readonly List<string> topObjectToOffset = ["tallgrass-*"];

    public static Dictionary<int, Block> GetTerrainReplacementMap(ICoreAPI api) =>
        GetBlockToReplaceMap(api, "TSblocksToReplace", blocksToReplace);

    public static Dictionary<int, Block> GetTopReplacementMap(ICoreAPI api) =>
        GetBlockToReplaceMap(api, "TStopObjectToOffset", topObjectToOffset);

    private static Dictionary<int, Block> GetBlockToReplaceMap(ICoreAPI api, string cacheKey, IEnumerable<string> codes)
    {
        return ObjectCacheUtil.GetOrCreate<Dictionary<int, Block>>(
            api,
            cacheKey,
            () =>
            {
                Dictionary<int, Block> result = [];
                foreach (var code in codes)
                {
                    foreach (var block in api.World.SearchBlocks(code))
                    {
                        AssetLocation slabCode = new(block.Code) { Domain = "terrainslabs" };
                        Block slabBlock = api.World.GetBlock(slabCode);
                        if (slabCode is null)
                        {
                            api.Logger.Warning("Unable to find slab block alternative with code {0}", slabCode);
                            continue;
                        }
                        result.Add(block.Id, slabBlock);
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
    private readonly Dictionary<int, Block> terrainReplacementMap = TerrainReplaceUtil.GetTerrainReplacementMap(api);
    private readonly Dictionary<int, Block> topReplacementMap = TerrainReplaceUtil.GetTopReplacementMap(api);

    public void TryReplaceWithSlab(BlockPos pos)
    {
        posBuffer.X = pos.X;
        posBuffer.Y = pos.Y;
        posBuffer.Z = pos.Z;
        posBuffer.dimension = pos.dimension;

        posBuffer.Y = accessor.GetRainMapHeightAt(posBuffer);
        if (terrainReplacementMap.TryGetValue(accessor.GetBlock(posBuffer).Id, out Block? slab) && HasExposedSide(posBuffer))
        {
            accessor.SetBlock(slab.Id, posBuffer);

            posBuffer.Y++;
            if (topReplacementMap.TryGetValue(accessor.GetBlock(posBuffer).Id, out Block? blockWithOffset))
            {
                accessor.SetBlock(blockWithOffset.Id, posBuffer);
            }
        }
    }

    private bool HasExposedSide(BlockPos pos)
    {
        pos.X++;
        if (IsExposeBlock(pos))
        {
            pos.X--;
            return true;
        }
        pos.X -= 2;

        if (IsExposeBlock(pos))
        {
            pos.X++;
            return true;
        }
        pos.X++;

        pos.Z++;
        if (IsExposeBlock(pos))
        {
            pos.Z--;
            return true;
        }
        pos.Z -= 2;

        if (IsExposeBlock(pos))
        {
            pos.Z++;
            return true;
        }
        pos.Z++;

        return false;
    }

    private bool IsExposeBlock(BlockPos pos)
    {
        Block block = accessor.GetBlock(pos);
        return block.MatterState != EnumMatterState.Liquid && block.CollisionBoxes is null;
    }
}
