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
                var genHandlers = sapi.Event.GetRegisteredWorldGenHandlers("standard").OnChunkColumnGen[(int)EnumWorldGenPass.PreDone];
                var accessor = provider.GetBlockAccessor(true);

                var settings = sapi.ModLoader.GetModSystem<TerrainSlabsConfigModSystem>().ServerSettings;
                var generationDelegate = GetGenerationDelegate(settings.SmoothMode, sapi, accessor);
                genHandlers.Add(generationDelegate);

                settings.SmoothModeChanged += (mode) =>
                {
                    genHandlers.Remove(generationDelegate);
                    generationDelegate = GetGenerationDelegate(mode, sapi, accessor);
                    genHandlers.Add(generationDelegate);
                };
            }
        );
    }

    private static ChunkColumnGenerationDelegate GetGenerationDelegate(
        TerrainSmoothMode mode,
        ICoreServerAPI sapi,
        IBlockAccessor blockAccessor
    )
    {
        return mode switch
        {
            TerrainSmoothMode.Surface => GetSmoothSurfaceDelegate(sapi, blockAccessor),
            _ => SkipWorldGenPass,
        };
    }

    private static void SkipWorldGenPass(IChunkColumnGenerateRequest request)
    {
        // do nothing
    }

    private static ChunkColumnGenerationDelegate GetSmoothSurfaceDelegate(ICoreServerAPI sapi, IBlockAccessor blockAccessor)
    {
        TerrainSlabReplacer slabReplacer = new(sapi, blockAccessor);
        return (request) => SmoothTerrainChunkSurface(request, blockAccessor, slabReplacer);
    }

    private static void SmoothTerrainChunkSurface(
        IChunkColumnGenerateRequest request,
        IBlockAccessor blockAccessor,
        TerrainSlabReplacer slabReplacer
    )
    {
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
