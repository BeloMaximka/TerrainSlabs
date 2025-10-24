using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace TerrainSlabs.Source;
public static class SlabGroupHelper
{
    private static int slabIdStart;
    private static int slabIdEnd;

    public static void UpdateIdRange(int start, int end)
    {
        slabIdStart = start;
        slabIdEnd = end;
    }

    public static bool IsSlab(int blockId)
    {
        return blockId >= slabIdStart && blockId <= slabIdEnd;
    }

    public static void RemapSlabIdIntoGroup(ICoreServerAPI sapi)
    {
        ServerSystemBlockIdRemapper? remapper = sapi.GetSystem<ServerSystemBlockIdRemapper>();
        if (remapper is null)
        {
            sapi.Logger.Error("Unable to get {0} from {1}", nameof(ServerSystemBlockIdRemapper), nameof(ICoreServerAPI));
            return;
        }

        slabIdStart = 200;
        slabIdEnd = slabIdStart - 1;
        var blockMap = remapper.LoadStoredBlockCodesById();
        Block[] slabBlocks = sapi.World.SearchBlocks(new("terrainslabs:*"));
        foreach (var slabBlock in slabBlocks)
        {
            slabIdEnd++;
            blockMap[slabBlock.Id] = blockMap[slabIdEnd];
            blockMap[slabIdEnd] = slabBlock.Code;

            Block blockToMove = sapi.World.Blocks[slabIdEnd];
            blockToMove.BlockId = slabBlock.BlockId;
            sapi.World.Blocks[slabBlock.Id] = blockToMove;

            slabBlock.BlockId = slabIdEnd;
            sapi.World.Blocks[slabIdEnd] = slabBlock;
        }
        remapper.StoreBlockCodesById(blockMap);
    }
}
