using Vintagestory.API.MathTools;

namespace TerrainSlabs.Source.Utils.WorldGen;

public interface ITerrainReplacer
{
    bool TryReplace(BlockPos pos);
}
