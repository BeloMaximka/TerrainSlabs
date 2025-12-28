using PlaceOnSlabs.Source.Utils;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TerrainSlabs.Source.Utils.WorldGen;

public class TerrainSmoother(ICoreAPI api, IBlockAccessor accessor) : ITerrainReplacer
{
    private readonly Dictionary<int, int> terrainReplacementMap = GetTerrainReplacementMap(api);

    public bool TryReplace(BlockPos pos)
    {
        pos.Y++;
        if (!ShouldOffset(pos))
        {
            pos.Y--;
            return false;
        }
        pos.Y--;

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
        if (TryHandleSideBlock(pos, BlockFacing.indexWEST))
        {
            pos.X--;
            return true;
        }
        pos.X -= 2;

        if (TryHandleSideBlock(pos, BlockFacing.indexEAST))
        {
            pos.X++;
            return true;
        }
        pos.X++;

        pos.Z++;
        if (TryHandleSideBlock(pos, BlockFacing.indexNORTH))
        {
            pos.Z--;
            return true;
        }
        pos.Z -= 2;

        if (TryHandleSideBlock(pos, BlockFacing.indexSOUTH))
        {
            pos.Z++;
            return true;
        }
        pos.Z++;

        return false;
    }

    private bool ShouldOffset(BlockPos pos)
    {
        return SlabHelper.ShouldOffset(accessor.GetBlockId(pos));
    }

    /// <summary>
    /// Checks if side is "Exposed" enough to place a slab
    /// Also makes beaches if it finds water
    /// TODO: move water-related logic out and name it better
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="faceIndex"></param>
    /// <returns></returns>
    private bool TryHandleSideBlock(BlockPos pos, int faceIndex)
    {
        Block solidBlock = accessor.GetBlock(pos, BlockLayersAccess.Solid);
        Block liquidBlock = accessor.GetBlock(pos, BlockLayersAccess.Fluid);

        if ( // beach generation
            liquidBlock.BlockId != 0
            && SlabHelper.ShouldOffset(solidBlock.BlockId)
            && accessor.GetBlockAbove(pos, 1, BlockLayersAccess.Solid).BlockId == 0
            && terrainReplacementMap.TryGetValue(accessor.GetBlockBelow(pos).BlockId, out int slabId)
        )
        {
            accessor.SetBlock(solidBlock.BlockId, pos.Up());
            accessor.SetBlock(slabId, pos.Down());
            return false;
        }

        return !SlabHelper.IsSlab(solidBlock.BlockId)
            && !solidBlock.SideSolid[faceIndex]
            && liquidBlock.BlockId == 0
            && solidBlock is not BlockMicroBlock;
    }

    private static Dictionary<int, int> GetTerrainReplacementMap(ICoreAPI api)
    {
        return ObjectCacheUtil.GetOrCreate(
            api,
            "TSblocksToSmooth",
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
                    result.Add(originalBlock.Id, resultBlock.Id);
                }
                return result;
            }
        );
    }
}
