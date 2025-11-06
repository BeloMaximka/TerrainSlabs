using HarmonyLib;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace TerrainSlabs.Source.HarmonyPatches;

[HarmonyPatch]
public static class DecorOverlayPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SurfaceLayerTesselator), nameof(SurfaceLayerTesselator.DrawBlockFace))]
    public static bool FixDecorForSlabs(
        SurfaceLayerTesselator __instance,
        float[] ___uv,
        TCTCache vars,
        int tileSide,
        FastVec3f[] quadOffsets,
        TextureAtlasPosition texPos,
        int flags,
        int colorMapDataValue,
        MeshData[] meshPools,
        float blockHeight = 1f,
        int rotIndex = 0
    )
    {
        Block[] currentChunkBlocksExt = Traverse.Create(vars).Field("tct").Field("currentChunkBlocksExt").GetValue<Block[]>();
        if (!SlabHelper.IsSlab(currentChunkBlocksExt[vars.extIndex3d]))
        {
            return true;
        }

        float uvOffset = (tileSide == BlockFacing.indexUP || tileSide == BlockFacing.indexDOWN) ? 0 : (texPos.y1 - texPos.y2) / 2; // our change
        float topVertexOffset = (tileSide == BlockFacing.indexDOWN) ? 0.5f : 0; // our change
        float nonBottomVertexOffset = (tileSide != BlockFacing.indexUP) ? 0.5f : 0; // our change
        MeshData meshPool = meshPools[(int)texPos.atlasNumber];
        int verticesCount = meshPool.VerticesCount;
        int[] lightRgbByCorner = vars.CurrentLightRGBByCorner;
        float uvx1 = texPos.x1;
        float uvy1 = texPos.y1 - uvOffset; // our change
        float uvx2 = texPos.x2;
        float uvy2 = texPos.y2;
        if (rotIndex > 1)
        {
            uvx1 = texPos.x2;
            uvy1 = texPos.y2;
            uvx2 = texPos.x1;
            uvy2 = texPos.y1;
        }
        if (rotIndex == 1 || rotIndex == 3)
        {
            float num1 = uvx2 - uvx1;
            float num2 = uvy2 - uvy1;
            float num3 = uvx1;
            float num4 = uvy1;
            uvx1 = num3 + num1;
            uvy1 = num4;
            uvx2 = num3;
            uvy2 = num4 + num2;
        }
        Vec3f normalf = BlockFacing.ALLFACES[tileSide].Normalf;
        float num5 = vars.finalX - normalf.X * 1f / 500f;
        float num6 = vars.finalY - normalf.Y * 1f / 500f;
        float num7 = vars.finalZ - normalf.Z * 1f / 500f;
        float num8 = 1.0001f;
        float num9 = 0.0f;
        float num10 = 0.0f;
        float num11 = 0.0f;
        float num12 = 0.0f;
        float num13 = 0.0f;
        float num14 = 0.0f;
        float num15 = 0.0f;
        float num16 = 0.0f;
        float num17 = 0.0f;
        float num18 = 0.0f;
        int index = (8 - vars.decorRotationData % 4 * 2) % 8;
        float[] uv = ___uv;

        if (vars.decorSubPosition > 0)
        {
            string path = vars.block.Code.Path;
            float xSize = (uvx2 - uvx1) / (float)GlobalConstants.CaveArtColsPerRow;
            float ySize = (uvy2 - uvy1) / (float)GlobalConstants.CaveArtColsPerRow;
            uvx1 += (float)((int)path[path.Length - 3] - 49) * xSize;
            uvy1 += (float)((int)path[path.Length - 1] - 49) * ySize;
            uvx2 = uvx1 + xSize;
            uvy2 = uvy1 + ySize;
            int num19 = vars.decorSubPosition - 1;
            float num20 = num8 / 16f;
            float num21 = (float)(num19 % 16) * num20;
            float num22 = (float)(num19 / 16) * num20;
            float num23 = 15f / 16f;
            float num24 = num20 * 4f;
            float num25 = num24;
            float num26 = 1f - num24;
            switch (tileSide)
            {
                case 0:
                    num5 += num21 - num24;
                    num6 += num23 - num22 - num24;
                    if ((double)num21 > (double)num26)
                    {
                        if (!CaveArtBlockOnSide(vars, tileSide, BlockFacing.EAST))
                        {
                            float excess = (float)(((double)num21 - (double)num26) / 0.5);
                            cropRightSide(ref uvx1, ref uvx2, ref uvy1, ref uvy2, xSize, ySize, excess, vars.decorRotationData);
                            num9 = (float)(-(double)num24 * 2.0) * excess;
                            num10 = (float)(-(double)num24 * 2.0) * excess;
                        }
                    }
                    else if ((double)num21 < (double)num25 && !CaveArtBlockOnSide(vars, tileSide, BlockFacing.WEST))
                    {
                        float excess = (float)(((double)num25 - (double)num21) / 0.5);
                        cropLeftSide(ref uvx1, ref uvx2, ref uvy1, ref uvy2, xSize, ySize, excess, vars.decorRotationData);
                        num11 = num24 * 2f * excess;
                        num12 = num24 * 2f * excess;
                    }
                    if ((double)num23 - (double)num22 > (double)num26)
                    {
                        if (!CaveArtBlockOnSide(vars, tileSide, BlockFacing.UP))
                        {
                            float excess = (float)(((double)num23 - (double)num22 - (double)num26) / 0.5);
                            cropTopSide(ref uvx1, ref uvx2, ref uvy1, ref uvy2, xSize, ySize, excess, vars.decorRotationData);
                            num13 = (float)(-(double)num24 * 2.0) * excess;
                            break;
                        }
                        break;
                    }
                    if ((double)num23 - (double)num22 < (double)num25 && !CaveArtBlockOnSide(vars, tileSide, BlockFacing.DOWN))
                    {
                        float excess = (float)(((double)num25 - (double)num23 + (double)num22) / 0.5);
                        cropBottomSide(ref uvx1, ref uvx2, ref uvy1, ref uvy2, xSize, ySize, excess, vars.decorRotationData);
                        num14 = num24 * 2f * excess;
                        break;
                    }
                    break;
                case 1:
                    num7 += num21 - num24;
                    num6 += num23 - num22 - num24;
                    num5 += 0.5f;
                    if ((double)num21 > (double)num26)
                    {
                        if (!CaveArtBlockOnSide(vars, tileSide, BlockFacing.SOUTH))
                        {
                            float excess = (float)(((double)num21 - (double)num26) / 0.5);
                            cropRightSide(ref uvx1, ref uvx2, ref uvy1, ref uvy2, xSize, ySize, excess, vars.decorRotationData);
                            num15 = (float)(-(double)num24 * 2.0) * excess;
                            num16 = (float)(-(double)num24 * 2.0) * excess;
                        }
                    }
                    else if ((double)num21 < (double)num25 && !CaveArtBlockOnSide(vars, tileSide, BlockFacing.NORTH))
                    {
                        float excess = (float)(((double)num25 - (double)num21) / 0.5);
                        cropLeftSide(ref uvx1, ref uvx2, ref uvy1, ref uvy2, xSize, ySize, excess, vars.decorRotationData);
                        num17 = num24 * 2f * excess;
                        num18 = num24 * 2f * excess;
                    }
                    if ((double)num23 - (double)num22 > (double)num26)
                    {
                        if (!CaveArtBlockOnSide(vars, tileSide, BlockFacing.UP))
                        {
                            float excess = (float)(((double)num23 - (double)num22 - (double)num26) / 0.5);
                            cropTopSide(ref uvx1, ref uvx2, ref uvy1, ref uvy2, xSize, ySize, excess, vars.decorRotationData);
                            num13 = (float)(-(double)num24 * 2.0) * excess;
                            break;
                        }
                        break;
                    }
                    if ((double)num23 - (double)num22 < (double)num25 && !CaveArtBlockOnSide(vars, tileSide, BlockFacing.DOWN))
                    {
                        float excess = (float)(((double)num25 - (double)num23 + (double)num22) / 0.5);
                        cropBottomSide(ref uvx1, ref uvx2, ref uvy1, ref uvy2, xSize, ySize, excess, vars.decorRotationData);
                        num14 = num24 * 2f * excess;
                        break;
                    }
                    break;
                case 2:
                    num5 += num23 - num21 - num24;
                    num6 += num23 - num22 - num24;
                    num7 += 0.5f;
                    if ((double)num23 - (double)num21 > (double)num26)
                    {
                        if (!CaveArtBlockOnSide(vars, tileSide, BlockFacing.EAST))
                        {
                            float excess = (float)(((double)num23 - (double)num21 - (double)num26) / 0.5);
                            cropLeftSide(ref uvx1, ref uvx2, ref uvy1, ref uvy2, xSize, ySize, excess, vars.decorRotationData);
                            num11 = (float)(-(double)num24 * 2.0) * excess;
                            num12 = (float)(-(double)num24 * 2.0) * excess;
                        }
                    }
                    else if ((double)num23 - (double)num21 < (double)num25 && !CaveArtBlockOnSide(vars, tileSide, BlockFacing.WEST))
                    {
                        float excess = (float)(((double)num25 - (double)num23 + (double)num21) / 0.5);
                        cropRightSide(ref uvx1, ref uvx2, ref uvy1, ref uvy2, xSize, ySize, excess, vars.decorRotationData);
                        num9 = num24 * 2f * excess;
                        num10 = num24 * 2f * excess;
                    }
                    if ((double)num23 - (double)num22 > (double)num26)
                    {
                        if (!CaveArtBlockOnSide(vars, tileSide, BlockFacing.UP))
                        {
                            float excess = (float)(((double)num23 - (double)num22 - (double)num26) / 0.5);
                            cropTopSide(ref uvx1, ref uvx2, ref uvy1, ref uvy2, xSize, ySize, excess, vars.decorRotationData);
                            num13 = (float)(-(double)num24 * 2.0) * excess;
                            break;
                        }
                        break;
                    }
                    if ((double)num23 - (double)num22 < (double)num25 && !CaveArtBlockOnSide(vars, tileSide, BlockFacing.DOWN))
                    {
                        float excess = (float)(((double)num25 - (double)num23 + (double)num22) / 0.5);
                        cropBottomSide(ref uvx1, ref uvx2, ref uvy1, ref uvy2, xSize, ySize, excess, vars.decorRotationData);
                        num14 = num24 * 2f * excess;
                        break;
                    }
                    break;
                case 3:
                    num7 += num23 - num21 - num24;
                    num6 += num23 - num22 - num24;
                    if ((double)num23 - (double)num21 > (double)num26)
                    {
                        if (!CaveArtBlockOnSide(vars, tileSide, BlockFacing.SOUTH))
                        {
                            float excess = (float)(((double)num23 - (double)num21 - (double)num26) / 0.5);
                            cropLeftSide(ref uvx1, ref uvx2, ref uvy1, ref uvy2, xSize, ySize, excess, vars.decorRotationData);
                            num17 = (float)(-(double)num24 * 2.0) * excess;
                            num18 = (float)(-(double)num24 * 2.0) * excess;
                        }
                    }
                    else if ((double)num23 - (double)num21 < (double)num25 && !CaveArtBlockOnSide(vars, tileSide, BlockFacing.NORTH))
                    {
                        float excess = (float)(((double)num25 - (double)num23 + (double)num21) / 0.5);
                        cropRightSide(ref uvx1, ref uvx2, ref uvy1, ref uvy2, xSize, ySize, excess, vars.decorRotationData);
                        num15 = num24 * 2f * excess;
                        num16 = num24 * 2f * excess;
                    }
                    if ((double)num23 - (double)num22 > (double)num26)
                    {
                        if (!CaveArtBlockOnSide(vars, tileSide, BlockFacing.UP))
                        {
                            float excess = (float)(((double)num23 - (double)num22 - (double)num26) / 0.5);
                            cropTopSide(ref uvx1, ref uvx2, ref uvy1, ref uvy2, xSize, ySize, excess, vars.decorRotationData);
                            num13 = (float)(-(double)num24 * 2.0) * excess;
                            break;
                        }
                        break;
                    }
                    if ((double)num23 - (double)num22 < (double)num25 && !CaveArtBlockOnSide(vars, tileSide, BlockFacing.DOWN))
                    {
                        float excess = (float)(((double)num25 - (double)num23 + (double)num22) / 0.5);
                        cropBottomSide(ref uvx1, ref uvx2, ref uvy1, ref uvy2, xSize, ySize, excess, vars.decorRotationData);
                        num14 = num24 * 2f * excess;
                        break;
                    }
                    break;
                case 4:
                    num5 += num21 - num24;
                    num7 += num23 - num22 - num24;
                    num6 += 0.5f;
                    if ((double)num21 > (double)num26)
                    {
                        if (!CaveArtBlockOnSide(vars, tileSide, BlockFacing.EAST))
                        {
                            float excess = (float)(((double)num21 - (double)num26) / 0.5);
                            cropRightSide(ref uvx1, ref uvx2, ref uvy1, ref uvy2, xSize, ySize, excess, vars.decorRotationData);
                            num9 = (float)(-(double)num24 * 2.0) * excess;
                            num10 = (float)(-(double)num24 * 2.0) * excess;
                        }
                    }
                    else if ((double)num21 < (double)num25 && !CaveArtBlockOnSide(vars, tileSide, BlockFacing.WEST))
                    {
                        float excess = (float)(((double)num25 - (double)num21) / 0.5);
                        cropLeftSide(ref uvx1, ref uvx2, ref uvy1, ref uvy2, xSize, ySize, excess, vars.decorRotationData);
                        num11 = num24 * 2f * excess;
                        num12 = num24 * 2f * excess;
                    }
                    if ((double)num23 - (double)num22 > (double)num26)
                    {
                        if (!CaveArtBlockOnSide(vars, tileSide, BlockFacing.SOUTH))
                        {
                            float excess = (float)(((double)num23 - (double)num22 - (double)num26) / 0.5);
                            cropTopSide(ref uvx1, ref uvx2, ref uvy1, ref uvy2, xSize, ySize, excess, vars.decorRotationData);
                            num17 = (float)(-(double)num24 * 2.0) * excess;
                            num15 = (float)(-(double)num24 * 2.0) * excess;
                            break;
                        }
                        break;
                    }
                    if ((double)num23 - (double)num22 < (double)num25 && !CaveArtBlockOnSide(vars, tileSide, BlockFacing.NORTH))
                    {
                        float excess = (float)(((double)num25 - (double)num23 + (double)num22) / 0.5);
                        cropBottomSide(ref uvx1, ref uvx2, ref uvy1, ref uvy2, xSize, ySize, excess, vars.decorRotationData);
                        num18 = num24 * 2f * excess;
                        num16 = num24 * 2f * excess;
                        break;
                    }
                    break;
                case 5:
                    num5 += num21 - num24;
                    num7 += num22 - num24;
                    if ((double)num21 > (double)num26)
                    {
                        if (!CaveArtBlockOnSide(vars, tileSide, BlockFacing.EAST))
                        {
                            float excess = (float)(((double)num21 - (double)num26) / 0.5);
                            cropRightSide(ref uvx1, ref uvx2, ref uvy1, ref uvy2, xSize, ySize, excess, vars.decorRotationData);
                            num9 = (float)(-(double)num24 * 2.0) * excess;
                            num10 = (float)(-(double)num24 * 2.0) * excess;
                        }
                    }
                    else if ((double)num21 < (double)num25 && !CaveArtBlockOnSide(vars, tileSide, BlockFacing.WEST))
                    {
                        float excess = (float)(((double)num25 - (double)num21) / 0.5);
                        cropLeftSide(ref uvx1, ref uvx2, ref uvy1, ref uvy2, xSize, ySize, excess, vars.decorRotationData);
                        num11 = num24 * 2f * excess;
                        num12 = num24 * 2f * excess;
                    }
                    if ((double)num22 > (double)num26)
                    {
                        if (!CaveArtBlockOnSide(vars, tileSide, BlockFacing.SOUTH))
                        {
                            float excess = (float)(((double)num22 - (double)num26) / 0.5);
                            cropBottomSide(ref uvx1, ref uvx2, ref uvy1, ref uvy2, xSize, ySize, excess, vars.decorRotationData);
                            num18 = (float)(-(double)num24 * 2.0) * excess;
                            num16 = (float)(-(double)num24 * 2.0) * excess;
                            break;
                        }
                        break;
                    }
                    if ((double)num22 < (double)num25 && !CaveArtBlockOnSide(vars, tileSide, BlockFacing.NORTH))
                    {
                        float excess = (float)(((double)num25 - (double)num22) / 0.5);
                        cropTopSide(ref uvx1, ref uvx2, ref uvy1, ref uvy2, xSize, ySize, excess, vars.decorRotationData);
                        num17 = num24 * 2f * excess;
                        num15 = num24 * 2f * excess;
                        break;
                    }
                    break;
            }
            num8 = num24 * 2f;
        }
        if ((vars.decorRotationData & 4) > 0)
        {
            uv[0] = uv[6] = uvx1;
            uv[2] = uv[4] = uvx2;
        }
        else
        {
            uv[0] = uv[6] = uvx2;
            uv[2] = uv[4] = uvx1;
        }
        uv[1] = uv[3] = uvy1;
        uv[5] = uv[7] = uvy2;
        FastVec3f quadOffset1 = quadOffsets[6];
        meshPool.AddVertexWithFlags(
            num5 + quadOffset1.X * num8 + num10,
            num6 + quadOffset1.Y * num8 + num14 - topVertexOffset, // our change
            num7 + quadOffset1.Z * num8 + num16,
            uv[(index + 4) % 8],
            uv[(index + 5) % 8],
            lightRgbByCorner[tileSide > 3 ? 0 : 3],
            flags
        );
        FastVec3f quadOffset2 = quadOffsets[4];
        meshPool.AddVertexWithFlags(
            num5 + quadOffset2.X * num8 + num9,
            num6 + quadOffset2.Y * num8 + num13 - nonBottomVertexOffset, // our change
            num7 + quadOffset2.Z * num8 + num15,
            uv[(index + 2) % 8],
            uv[(index + 3) % 8],
            lightRgbByCorner[tileSide > 3 ? 2 : 1],
            flags
        );
        FastVec3f quadOffset3 = quadOffsets[5];
        meshPool.AddVertexWithFlags(
            num5 + quadOffset3.X * num8 + num11,
            num6 + quadOffset3.Y * num8 + num13 - nonBottomVertexOffset, // our change
            num7 + quadOffset3.Z * num8 + num17,
            uv[index],
            uv[index + 1],
            lightRgbByCorner[tileSide > 3 ? 3 : 0],
            flags
        );
        FastVec3f quadOffset4 = quadOffsets[7];
        meshPool.AddVertexWithFlags(
            num5 + quadOffset4.X * num8 + num12,
            num6 + quadOffset4.Y * num8 + num14 - topVertexOffset, // our change
            num7 + quadOffset4.Z * num8 + num18,
            uv[(index + 6) % 8],
            uv[(index + 7) % 8],
            lightRgbByCorner[tileSide > 3 ? 1 : 2],
            flags
        );
        meshPool.CustomInts.Add4(colorMapDataValue);
        meshPool.AddQuadIndices(verticesCount);

        return false;
    }

    private static void cropRightSide(
        ref float uvx1,
        ref float uvx2,
        ref float uvy1,
        ref float uvy2,
        float xSize,
        float ySize,
        float excess,
        int rot
    )
    {
        switch (rot % 8)
        {
            case 0:
            case 6:
                uvx1 += xSize * excess;
                break;
            case 1:
            case 5:
                uvy1 += ySize * excess - 0.5f;
                break;
            case 2:
            case 4:
                uvx2 -= xSize * excess;
                break;
            case 3:
            case 7:
                uvy2 -= ySize * excess - 0.5f;
                break;
        }
    }

    private static void cropLeftSide(
        ref float uvx1,
        ref float uvx2,
        ref float uvy1,
        ref float uvy2,
        float xSize,
        float ySize,
        float excess,
        int rot
    )
    {
        switch (rot % 8)
        {
            case 0:
            case 6:
                uvx2 -= xSize * excess;
                break;
            case 1:
            case 5:
                uvy2 -= ySize * excess;
                break;
            case 2:
            case 4:
                uvx1 += xSize * excess;
                break;
            case 3:
            case 7:
                uvy1 += ySize * excess;
                break;
        }
    }

    private static void cropTopSide(
        ref float uvx1,
        ref float uvx2,
        ref float uvy1,
        ref float uvy2,
        float xSize,
        float ySize,
        float excess,
        int rot
    )
    {
        switch (rot % 8)
        {
            case 0:
            case 4:
                uvy1 += ySize * excess;
                break;
            case 1:
            case 7:
                uvx2 -= xSize * excess;
                break;
            case 2:
            case 6:
                uvy2 -= ySize * excess;
                break;
            case 3:
            case 5:
                uvx1 += xSize * excess;
                break;
        }
    }

    private static void cropBottomSide(
        ref float uvx1,
        ref float uvx2,
        ref float uvy1,
        ref float uvy2,
        float xSize,
        float ySize,
        float excess,
        int rot
    )
    {
        switch (rot % 8)
        {
            case 0:
            case 4:
                uvy2 -= ySize * excess;
                break;
            case 1:
            case 7:
                uvx1 += xSize * excess;
                break;
            case 2:
            case 6:
                uvy1 += ySize * excess;
                break;
            case 3:
            case 5:
                uvx2 -= xSize * excess;
                break;
        }
    }

    private static bool CaveArtBlockOnSide(TCTCache vars, int tileSide, BlockFacing neibDir)
    {
        Block[] currentChunkBlocksExt = Traverse.Create(vars).Field("tct").Field("currentChunkBlocksExt").GetValue<Block[]>();
        int extIndex3d = vars.extIndex3d;
        EnumBlockMaterial blockMaterial = currentChunkBlocksExt[extIndex3d].BlockMaterial;
        switch (neibDir.Index)
        {
            case 0:
                extIndex3d -= 34;
                break;
            case 1:
                ++extIndex3d;
                break;
            case 2:
                extIndex3d += 34;
                break;
            case 3:
                --extIndex3d;
                break;
            case 4:
                extIndex3d += 1156;
                break;
            case 5:
                extIndex3d -= 1156;
                break;
        }
        Block block = currentChunkBlocksExt[extIndex3d];
        return block.SideSolid[tileSide] && block.BlockMaterial == blockMaterial;
    }
}
