using HarmonyLib;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace TerrainSlabs.Source.HarmonyPatches;

// This is so ugly but I suck and graphical programming
[HarmonyPatch]
public static class SoilSlabGrassOverlayPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TopsoilTesselator), "DrawBlockFaceTopSoil")]
    public static bool HandleItemWithBuildingMode(
        TCTCache vars,
        int flags,
        FastVec3f[] quadOffsets,
        int colorMapDataValue,
        int textureSubId,
        int textureSubIdSecond,
        MeshData[] meshPools,
        int rotIndex
    )
    {
        if (!SlabGroupHelper.IsSlab(vars.block.BlockId))
        {
            return true;
        }

        TextureAtlasPosition textureAtlasPosition1 = vars.textureAtlasPositionsByTextureSubId[textureSubId];
        TextureAtlasPosition textureAtlasPosition2 = vars.textureAtlasPositionsByTextureSubId[textureSubIdSecond];
        MeshData meshPool = meshPools[textureAtlasPosition1.atlasNumber];
        int verticesCount = meshPool.VerticesCount;
        float lx = vars.lx;
        float ly = vars.ly;
        float lz = vars.lz;

        // Scale factor for height (0.5 -> half height)
        const float heightScale = 0.5f;

        FastVec3f quadOffset1 = quadOffsets[7];
        meshPool.AddVertexWithFlags(
            lx + quadOffset1.X,
            ly + quadOffset1.Y * heightScale,
            lz + quadOffset1.Z,
            textureAtlasPosition1.x2,
            textureAtlasPosition1.y2,
            vars.CurrentLightRGBByCorner[3],
            flags
        );

        FastVec3f quadOffset2 = quadOffsets[5];
        meshPool.AddVertexWithFlags(
            lx + quadOffset2.X,
            ly + quadOffset2.Y * heightScale,
            lz + quadOffset2.Z,
            textureAtlasPosition1.x2,
            textureAtlasPosition1.y1,
            vars.CurrentLightRGBByCorner[1],
            flags
        );

        FastVec3f quadOffset3 = quadOffsets[4];
        meshPool.AddVertexWithFlags(
            lx + quadOffset3.X,
            ly + quadOffset3.Y * heightScale,
            lz + quadOffset3.Z,
            textureAtlasPosition1.x1,
            textureAtlasPosition1.y1,
            vars.CurrentLightRGBByCorner[0],
            flags
        );

        FastVec3f quadOffset4 = quadOffsets[6];
        meshPool.AddVertexWithFlags(
            lx + quadOffset4.X,
            ly + quadOffset4.Y * heightScale,
            lz + quadOffset4.Z,
            textureAtlasPosition1.x1,
            textureAtlasPosition1.y2,
            vars.CurrentLightRGBByCorner[2],
            flags
        );

        float x1 = textureAtlasPosition2.x1;
        float u = textureAtlasPosition2.x1 + (float)((textureAtlasPosition2.x2 - (double)textureAtlasPosition2.x1) / 2.0);
        float y1 = textureAtlasPosition2.y1;
        bool isTop = (flags & BlockFacing.ALLFACES[BlockFacing.indexUP].NormalPackedFlags) != 0;
        float y2 = isTop ? textureAtlasPosition2.y2 : textureAtlasPosition2.y2 - (textureAtlasPosition2.y2 - textureAtlasPosition2.y1) / 2;
        switch (rotIndex)
        {
            case 0:
                meshPool.CustomShorts.AddPackedUV(u, y2, true, true);
                meshPool.CustomShorts.AddPackedUV(u, y1, true, false);
                meshPool.CustomShorts.AddPackedUV(x1, y1, false, false);
                meshPool.CustomShorts.AddPackedUV(x1, y2, false, true);
                break;
            case 1:
                meshPool.CustomShorts.AddPackedUV(x1, y2, false, true);
                meshPool.CustomShorts.AddPackedUV(x1, y1, false, false);
                meshPool.CustomShorts.AddPackedUV(u, y1, true, false);
                meshPool.CustomShorts.AddPackedUV(u, y2, true, true);
                break;
            case 2:
                meshPool.CustomShorts.AddPackedUV(u, y1, true, false);
                meshPool.CustomShorts.AddPackedUV(u, y2, true, true);
                meshPool.CustomShorts.AddPackedUV(x1, y2, false, true);
                meshPool.CustomShorts.AddPackedUV(x1, y1, false, false);
                break;
            case 3:
                meshPool.CustomShorts.AddPackedUV(x1, y1, false, false);
                meshPool.CustomShorts.AddPackedUV(x1, y2, false, true);
                meshPool.CustomShorts.AddPackedUV(u, y2, true, true);
                meshPool.CustomShorts.AddPackedUV(u, y1, true, false);
                break;
        }
        meshPool.CustomInts.Add4(colorMapDataValue);
        meshPool.AddQuadIndices(verticesCount);

        vars.UpdateChunkMinMax(lx, ly, lz);
        // update to half height (was ly + 1f)
        vars.UpdateChunkMinMax(lx + 1f, ly + 0.5f, lz + 1f);

        return false;
    }
}
