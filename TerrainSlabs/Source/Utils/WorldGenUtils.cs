using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TerrainSlabs.Source.Systems;
using TerrainSlabs.Source.Utils.WorldGen;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace TerrainSlabs.Source.Utils;

public static class WorldGenUtils
{
    public static void RegisterSlabReplacementWorldGenEvent(ICoreServerAPI sapi)
    {
#if DEBUG
        RuntimeEnv.DebugOutOfRangeBlockAccess = true;
#endif
        sapi.Event.GetWorldgenBlockAccessor(
            (provider) =>
            {
                var genHandlers = sapi.Event.GetRegisteredWorldGenHandlers("standard").OnChunkColumnGen[(int)EnumWorldGenPass.PreDone];
                var accessor = provider.GetBlockAccessor(true);

                var settings = sapi.ModLoader.GetModSystem<ConfigSystem>().ServerSettings;
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
            TerrainSmoothMode.Column => GetSmoothColumnDelegate(sapi, blockAccessor),
            _ => SkipWorldGenPass,
        };
    }

    private static void SkipWorldGenPass(IChunkColumnGenerateRequest request)
    {
        // do nothing
    }

    private static ChunkColumnGenerationDelegate GetSmoothSurfaceDelegate(ICoreServerAPI sapi, IBlockAccessor blockAccessor)
    {
        TerrainSmoother smoother = new(sapi, blockAccessor);
        return (request) => SmoothTerrainChunkSurface(sapi, request, smoother, blockAccessor);
    }

    private static void SmoothTerrainChunkSurface(
        ICoreAPI api,
        IChunkColumnGenerateRequest request,
        TerrainSmoother smoother,
        IBlockAccessor blockAccessor
    )
    {
#if DEBUG
        Stopwatch sw = Stopwatch.StartNew();
#endif

        BlockPos blockPos = new(Dimensions.NormalWorld);
        for (var x = 0; x < GlobalConstants.ChunkSize; x++)
        {
            for (var z = 0; z < GlobalConstants.ChunkSize; z++)
            {
                blockPos.X = request.ChunkX * GlobalConstants.ChunkSize + x;
                blockPos.Z = request.ChunkZ * GlobalConstants.ChunkSize + z;
                blockPos.Y = blockAccessor.GetTerrainMapheightAt(blockPos);
                smoother.TryReplace(blockPos);
            }
        }

#if DEBUG
        sw.Stop();
        if (TerrainSlabsGlobals.DebugMode)
        {
            api.Logger.Debug(
                "[terrainslabs] Took {0} ms to smooth a chunk surface",
                Math.Round((double)sw.ElapsedTicks / Stopwatch.Frequency * 1000, 4)
            );
        }
#endif
    }

    private static ChunkColumnGenerationDelegate GetSmoothColumnDelegate(ICoreServerAPI sapi, IBlockAccessor blockAccessor)
    {
        TerrainSmoother smoother = new(sapi, blockAccessor);
        return (request) => SmoothTerrainChunkColumn(sapi, request, smoother, blockAccessor);
    }

    private static void SmoothTerrainChunkColumn(
        ICoreAPI api,
        IChunkColumnGenerateRequest request,
        TerrainSmoother smoother,
        IBlockAccessor blockAccessor
    )
    {
#if DEBUG
        Stopwatch sw = Stopwatch.StartNew();
#endif
        BlockPos blockPos = new(Dimensions.NormalWorld);
        for (var x = 0; x < GlobalConstants.ChunkSize; x++)
        {
            for (var z = 0; z < GlobalConstants.ChunkSize; z++)
            {
                blockPos.X = request.ChunkX * GlobalConstants.ChunkSize + x;
                blockPos.Z = request.ChunkZ * GlobalConstants.ChunkSize + z;
                blockPos.Y = blockAccessor.GetTerrainMapheightAt(blockPos);
                var yLevels = GetStructureYLevels(request, blockPos.X, blockPos.Z);

                while (blockPos.Y > 10)
                {
                    MoveLowerThanStructure(yLevels, blockPos);
                    smoother.TryReplace(blockPos);
                    blockPos.Y--;
                }
            }
        }

#if DEBUG
        sw.Stop();
        if (TerrainSlabsGlobals.DebugMode)
        {
            api.Logger.Debug(
                "[terrainslabs] Took {0} ms to smooth a chunk column",
                Math.Round((double)sw.ElapsedTicks / Stopwatch.Frequency * 1000, 4)
            );
        }
#endif
    }

    private static List<Cuboidi> GetStructureYLevels(IChunkColumnGenerateRequest request, int x, int z)
    {
        return request
            .Chunks[0]
            .MapChunk.MapRegion.GeneratedStructures.Where(structure =>
                x >= structure.Location.X1 && x <= structure.Location.X2 && z >= structure.Location.Z1 && z <= structure.Location.Z2
            )
            .Select(s => s.Location)
            .ToList();
    }

    private static void MoveLowerThanStructure(List<Cuboidi> yLevels, BlockPos pos)
    {
        foreach (var location in yLevels)
        {
            if (pos.Y >= location.Y1 && pos.Y <= location.Y2)
            {
                pos.Y = location.Y1 - 1;
            }
        }
    }
}
