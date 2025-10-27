using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TerrainSlabs.Source.Utils;

public static class BlockAccessorExtension
{
    public static bool AreNeigbourBlocksLoaded(this IBlockAccessor accessor, BlockPos pos)
    {
        pos.X++;
        if (accessor.GetChunkAtBlockPos(pos) is null)
        {
            pos.X--;
            return false;
        }
        pos.X -= 2;

        if (accessor.GetChunkAtBlockPos(pos) is null)
        {
            pos.X++;
            return false;
        }
        pos.X++;

        pos.Z++;
        if (accessor.GetChunkAtBlockPos(pos) is null)
        {
            pos.Z--;
            return false;
        }
        pos.Z -= 2;

        if (accessor.GetChunkAtBlockPos(pos) is null)
        {
            pos.Z++;
            return false;
        }
        pos.Z++;

        return true;
    }
}
