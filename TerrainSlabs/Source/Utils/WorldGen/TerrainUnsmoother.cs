using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace TerrainSlabs.Source.Utils.WorldGen;

internal class TerrainUnsmoother(ICoreAPI api, IBlockAccessor accessor) : ITerrainReplacer
{
    private readonly Dictionary<int, int> terrainReplacementMap = GetSlabRevertMap(api);

    public bool TryReplace(BlockPos pos)
    {
        if (terrainReplacementMap.TryGetValue(accessor.GetBlock(pos).Id, out int originalBlockId))
        {
            if (accessor.GetBlock(pos, BlockLayersAccess.Fluid).BlockId != 0)
            {
                accessor.SetBlock(accessor.GetBlockAbove(pos).BlockId, pos);
                accessor.SetBlock(0, pos.Up());
                pos.Down();
            }
            else
            {
                accessor.SetBlock(originalBlockId, pos);
            }
            return true;
        }
        return false;
    }

    private static Dictionary<int, int> GetSlabRevertMap(ICoreAPI api)
    {
        return ObjectCacheUtil.GetOrCreate(
            api,
            "TSblocksToRevert",
            () =>
            {
                string wildcard = $"terrainslabs:*";
                Dictionary<int, int> result = [];
                foreach (var resultBlock in api.World.SearchBlocks(wildcard))
                {
                    AssetLocation originalCode = resultBlock.Code.UseFirstPartAsDomain();
                    Block? originalBlock = api.World.GetBlock(originalCode);
                    if (originalBlock is null)
                    {
                        api.Logger.Warning("[terrainslabs] Unable to find slab block alternative with code {0}", originalCode);
                        continue;
                    }
                    result.Add(resultBlock.Id, originalBlock.Id);
                }
                return result;
            }
        );
    }
}
