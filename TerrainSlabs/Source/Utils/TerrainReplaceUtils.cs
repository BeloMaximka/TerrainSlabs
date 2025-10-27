using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TerrainSlabs.Source.Utils;

public static class TerrainReplaceUtils
{
    public static Dictionary<int, int> GetTerrainReplacementMap(ICoreAPI api) => GetBlockToReplaceMap(api, "TSblocksToReplace");

    private static Dictionary<int, int> GetBlockToReplaceMap(ICoreAPI api, string cacheKey)
    {
        return ObjectCacheUtil.GetOrCreate(
            api,
            cacheKey,
            () =>
            {
                string wildcard = $"terrainslabs:*";
                Dictionary<int, int> result = [];
                foreach (var resultBlock in api.World.SearchBlocks(wildcard))
                {
                    AssetLocation originalCode = new("game", resultBlock.Code.Path);
                    Block? originalBlock = api.World.GetBlock(originalCode);
                    if (originalBlock is null)
                    {
                        api.Logger.Warning("Unable to find slab block alternative with code {0}", originalCode);
                        continue;
                    }
                    result.Add(originalBlock.Id, resultBlock.Id);
                }
                return result;
            }
        );
    }
}

public class TerrainSlabReplacer(ICoreAPI api, IBlockAccessor accessor)
{
    private readonly Dictionary<int, int> terrainReplacementMap = TerrainReplaceUtils.GetTerrainReplacementMap(api);

    public bool TryReplaceWithSlab(BlockPos pos)
    {
        pos.Y++;
        if (!ShouldOffset(pos))
        {
            pos.Y--;
            return false;
        }
        pos.Y--;


        var decors = accessor.GetSubDecors(pos);
        if (decors is not null && decors.Count != 0)
        {
            return false;
        }

        if (terrainReplacementMap.TryGetValue(accessor.GetBlockId(pos), out int slabId) && HasExposedSide(pos))
        {
            accessor.SetBlock(slabId, pos);
            return true;
        }
        return false;
    }

    private bool HasExposedSide(BlockPos pos)
    {
        pos.X++;
        if (IsExposeBlock(pos, BlockFacing.indexWEST))
        {
            pos.X--;
            return true;
        }
        pos.X -= 2;

        if (IsExposeBlock(pos, BlockFacing.indexEAST))
        {
            pos.X++;
            return true;
        }
        pos.X++;

        pos.Z++;
        if (IsExposeBlock(pos, BlockFacing.indexNORTH))
        {
            pos.Z--;
            return true;
        }
        pos.Z -= 2;

        if (IsExposeBlock(pos, BlockFacing.indexSOUTH))
        {
            pos.Z++;
            return true;
        }
        pos.Z++;

        return false;
    }

    private bool ShouldOffset(BlockPos pos)
    {
        return SlabGroupHelper.ShouldOffset(accessor.GetBlockId(pos));
    }

    private bool IsExposeBlock(BlockPos pos, int faceIndex)
    {
        Block block = accessor.GetBlock(pos);
        return !SlabGroupHelper.IsSlab(block.BlockId) && !block.SideSolid[faceIndex] && block.MatterState != EnumMatterState.Liquid && block is not BlockMicroBlock;
    }
}
