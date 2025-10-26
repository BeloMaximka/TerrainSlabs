using TerrainSlabs.Source.Systems;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace TerrainSlabs.Source.Utils;

public static class WorldGenUtils
{
    public static void RegisterSlabReplacementWorldGenEvent(ICoreServerAPI sapi)
    {
        sapi.Event.GetWorldgenBlockAccessor(
            (provider) =>
            {
                var handlers = sapi.Event.GetRegisteredWorldGenHandlers("standard");
                handlers
                    .OnChunkColumnGen[(int)EnumWorldGenPass.PreDone]
                    .Add(GetSmoothTerrainDelegate(sapi, provider.GetBlockAccessor(true)));
            }
        );
    }

    private static ChunkColumnGenerationDelegate GetSmoothTerrainDelegate(ICoreServerAPI sapi, IBlockAccessor blockAccessor)
    {
        ServerSettings settings = sapi.ModLoader.GetModSystem<TerrainSlabsConfigModSystem>().ServerSettings;
        TerrainSlabReplacer slabReplacer = new(sapi, blockAccessor);
        return (request) => SmoothTerrainChunkColumn(request, blockAccessor, slabReplacer, settings);
    }

    private static void SmoothTerrainChunkColumn(
        IChunkColumnGenerateRequest request,
        IBlockAccessor blockAccessor,
        TerrainSlabReplacer slabReplacer,
        ServerSettings settings
    )
    {
        if (!settings.EnableWorldGen)
        {
            return;
        }

        BlockPos blockPos = new(Dimensions.NormalWorld);
        for (var x = 0; x < GlobalConstants.ChunkSize; x++)
        {
            for (var z = 0; z < GlobalConstants.ChunkSize; z++)
            {
                blockPos.X = request.ChunkX * GlobalConstants.ChunkSize + x;
                blockPos.Z = request.ChunkZ * GlobalConstants.ChunkSize + z;
                blockPos.Y = blockAccessor.GetTerrainMapheightAt(blockPos);
                slabReplacer.TryReplaceWithSlab(blockPos);
            }
        }
    }
}
