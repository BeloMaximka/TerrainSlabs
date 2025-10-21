using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TerrainSlabs.Source;

class BlockTallGrassOffset : BlockTallGrass, IDrawYAdjustable
{
    float IDrawYAdjustable.AdjustYPosition(BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
    {
        return -0.5f;
    }
}
