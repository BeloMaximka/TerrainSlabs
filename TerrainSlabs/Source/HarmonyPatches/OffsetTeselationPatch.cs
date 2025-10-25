using HarmonyLib;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace TerrainSlabs.Source.HarmonyPatches;

[HarmonyPatch]
public static class OffsetTeselationPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(
        typeof(ChunkTesselator),
        "TesselateBlock",
        typeof(Block),
        typeof(int),
        typeof(int),
        typeof(int),
        typeof(int),
        typeof(int)
    )]
    public static bool HandleOffsetBlocks(
        TCTCache ___vars,
        Block[] ___currentChunkBlocksExt,
        Block[] ___currentChunkFluidBlocksExt,
        int[] ___currentClimateRegionMap,
        float ___currentOceanityMapTL,
        float ___currentOceanityMapTR,
        float ___currentOceanityMapBL,
        float ___currentOceanityMapBR,
        int ___regionSize,
        int[][] ___fastBlockTextureSubidsByBlockAndFace,
        IBlockTesselator[] ___blockTesselators,
        int ___seaLevel,
        ColorMapData ___defaultColorMapData,
        Block block,
        int lX,
        int faceflags,
        int posX,
        int posZ,
        int drawType
    )
    {
        if (block.DrawType == EnumDrawType.Empty)
            return false;
        ___vars.block = block;
        ___vars.drawFaceFlags = faceflags;
        ___vars.posX = posX;
        ___vars.lx = lX;
        ___vars.finalX = lX;
        ___vars.finalY = ___vars.ly;

        // Our code
        if (
            SlabGroupHelper.IsSlab(___currentChunkBlocksExt[___vars.extIndex3d + TileSideEnum.MoveIndex[5]].BlockId)
            && SlabGroupHelper.ShouldOffset(block)
        )
            ___vars.finalY -= 0.5f;
        // Our code ends, looks like a good candidate for Transpiler, but I am afraid of them
        else if (block is IDrawYAdjustable drawYadjustable)
            ___vars.finalY += drawYadjustable.AdjustYPosition(
                new BlockPos(___vars.posX, ___vars.posY, ___vars.posZ),
                ___currentChunkBlocksExt,
                ___vars.extIndex3d
            );
        ___vars.finalZ = ___vars.lz;
        int index = ___vars.blockId = block.BlockId;
        ___vars.textureSubId = 0;
        ___vars.VertexFlags = block.VertexFlags.All;
        ___vars.RenderPass = block.RenderPass;
        ___vars.fastBlockTextureSubidsByFace = ___fastBlockTextureSubidsByBlockAndFace[index];
        if (block.RandomDrawOffset != 0)
        {
            ___vars.finalX += GameMath.oaatHash(posX, 0, posZ) % 12 / (float)(24.0 + 12.0 * block.RandomDrawOffset);
            ___vars.finalZ += GameMath.oaatHash(posX, 1, posZ) % 12 / (float)(24.0 + 12.0 * block.RandomDrawOffset);
        }
        if (block.ShapeUsesColormap || block.LoadColorMapAnyway || block.Frostable)
        {
            int num1 = posX + GameMath.MurmurHash3Mod(posX, 0, posZ, 5) - 2;
            int num2 = posZ + GameMath.MurmurHash3Mod(posX, 1, posZ, 5) - 2;
            int num3 = posX / ___regionSize;
            int num4 = posZ / ___regionSize;
            int currentClimateRegion = ___currentClimateRegionMap[
                GameMath.Clamp(num2 - num4 * ___regionSize, 0, ___regionSize - 1) * ___regionSize
                    + GameMath.Clamp(num1 - num3 * ___regionSize, 0, ___regionSize - 1)
            ];
            TCTCache vars = ___vars;
            ColorMap colorMapResolved1 = block.SeasonColorMapResolved;
            int seasonMapIndex = colorMapResolved1 != null ? colorMapResolved1.RectIndex + 1 : 0;
            ColorMap colorMapResolved2 = block.ClimateColorMapResolved;
            int climateMapIndex = colorMapResolved2 != null ? colorMapResolved2.RectIndex + 1 : 0;
            int adjustedTemperature = Climate.GetAdjustedTemperature(
                currentClimateRegion >> 16 & byte.MaxValue,
                ___vars.posY - ___seaLevel
            );
            int rainFall = Climate.GetRainFall(currentClimateRegion >> 8 & byte.MaxValue, ___vars.posY);
            int num5 = block.Frostable ? 1 : 0;
            ColorMapData colorMapData = new ColorMapData(seasonMapIndex, climateMapIndex, adjustedTemperature, rainFall, num5 != 0);
            vars.ColorMapData = colorMapData;
        }
        else
            ___vars.ColorMapData = ___defaultColorMapData;
        if (block.DrawType == EnumDrawType.Liquid)
        {
            if (___vars.posY == ___seaLevel - 1)
            {
                Block block1 = ___currentChunkFluidBlocksExt[___vars.extIndex3d + TileSideEnum.MoveIndex[0]];
                Block block2 = ___currentChunkFluidBlocksExt[___vars.extIndex3d + TileSideEnum.MoveIndex[2]];
                Block block3 = ___currentChunkFluidBlocksExt[___vars.extIndex3d + TileSideEnum.MoveIndex[3]];
                Block block4 = ___currentChunkFluidBlocksExt[___vars.extIndex3d + TileSideEnum.MoveIndex[1]];
                Block block5 = ___currentChunkFluidBlocksExt[___vars.extIndex3d + TileSideEnum.MoveIndex[0] + TileSideEnum.MoveIndex[3]];
                Block block6 = ___currentChunkFluidBlocksExt[___vars.extIndex3d + TileSideEnum.MoveIndex[2] + TileSideEnum.MoveIndex[3]];
                Block block7 = ___currentChunkFluidBlocksExt[___vars.extIndex3d + TileSideEnum.MoveIndex[0] + TileSideEnum.MoveIndex[1]];
                Block block8 = ___currentChunkFluidBlocksExt[___vars.extIndex3d + TileSideEnum.MoveIndex[2] + TileSideEnum.MoveIndex[1]];
                if (
                    block5.Id == 0
                    && ___vars.lx == 0
                    && ___vars.lz == 0
                    && ___currentChunkBlocksExt[___vars.extIndex3d + TileSideEnum.MoveIndex[0] + TileSideEnum.MoveIndex[3]].Id == 0
                )
                    block5 = block;
                if (
                    block6.Id == 0
                    && ___vars.lx == 0
                    && ___vars.lz == 31
                    && ___currentChunkBlocksExt[___vars.extIndex3d + TileSideEnum.MoveIndex[2] + TileSideEnum.MoveIndex[3]].Id == 0
                )
                    block6 = block;
                if (
                    block7.Id == 0
                    && ___vars.lx == 31
                    && ___vars.lz == 0
                    && ___currentChunkBlocksExt[___vars.extIndex3d + TileSideEnum.MoveIndex[0] + TileSideEnum.MoveIndex[1]].Id == 0
                )
                    block7 = block;
                if (
                    block8.Id == 0
                    && ___vars.lx == 31
                    && ___vars.lz == 31
                    && ___currentChunkBlocksExt[___vars.extIndex3d + TileSideEnum.MoveIndex[2] + TileSideEnum.MoveIndex[1]].Id == 0
                )
                    block8 = block;
                ___vars.OceanityFlagTL =
                    block1 != block || block5 != block || block3 != block
                        ? 0
                        : (byte)
                            GameMath.BiLerp(
                                ___currentOceanityMapTL,
                                ___currentOceanityMapTR,
                                ___currentOceanityMapBL,
                                ___currentOceanityMapBR,
                                ___vars.lx / 32f,
                                ___vars.lz / 32f
                            ) << 2;
                ___vars.OceanityFlagTR =
                    block1 != block || block7 != block || block4 != block
                        ? 0
                        : (byte)
                            GameMath.BiLerp(
                                ___currentOceanityMapTL,
                                ___currentOceanityMapTR,
                                ___currentOceanityMapBL,
                                ___currentOceanityMapBR,
                                (___vars.lx + 1) / 32f,
                                ___vars.lz / 32f
                            ) << 2;
                ___vars.OceanityFlagBL =
                    block2 != block || block6 != block || block3 != block
                        ? 0
                        : (byte)
                            GameMath.BiLerp(
                                ___currentOceanityMapTL,
                                ___currentOceanityMapTR,
                                ___currentOceanityMapBL,
                                ___currentOceanityMapBR,
                                ___vars.lx / 32f,
                                (___vars.lz + 1) / 32f
                            ) << 2;
                ___vars.OceanityFlagBR =
                    block2 != block || block8 != block || block4 != block
                        ? 0
                        : (byte)
                            GameMath.BiLerp(
                                ___currentOceanityMapTL,
                                ___currentOceanityMapTR,
                                ___currentOceanityMapBL,
                                ___currentOceanityMapBR,
                                (___vars.lx + 1) / 32f,
                                (___vars.lz + 1) / 32f
                            ) << 2;
            }
            else
            {
                ___vars.OceanityFlagTL = 0;
                ___vars.OceanityFlagTR = 0;
                ___vars.OceanityFlagBL = 0;
                ___vars.OceanityFlagBR = 0;
            }
        }
        ___vars.textureVOffset =
            !block.alternatingVOffset
            || ((block.alternatingVOffsetFaces & 10) <= 0 || posX % 2 != 1)
                && ((block.alternatingVOffsetFaces & 48) <= 0 || ___vars.posY % 2 != 1)
                && ((block.alternatingVOffsetFaces & 5) <= 0 || posZ % 2 != 1)
                ? 0.0f
                : 1f;
        ___blockTesselators[drawType].Tesselate(___vars);

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ChunkTesselator), nameof(ChunkTesselator.CalculateVisibleFaces))]
    public static bool FixSnowFaceCulling(
        Block[] ___currentChunkBlocksExt,
        byte[] ___currentChunkDraw32,
        Block[] ___blocksFast,
        BlockPos ___tmpPos,
        ClientMain ___game,
        ref bool __result,
        bool skipChunkCenter,
        int baseX,
        int baseY,
        int baseZ
    )
    {
        byte[] currentChunkDraw32 = ___currentChunkDraw32;
        int num1 = 0;
        Block block1 = ___blocksFast[0];
        for (int index1 = 0; index1 < 32; ++index1)
        {
            int num2 = index1 * 32 * 32;
            for (int index2 = 0; index2 < 32; ++index2)
            {
                int num3 = (index1 * 34 + index2) * 34 + 1191;
                int num4 = index1 * (index1 ^ 31) * index2 * (index2 ^ 31);
                for (int index3 = 0; index3 < 32; ++index3)
                {
                    Block block2;
                    if ((block2 = ___currentChunkBlocksExt[num3 + index3]) == block1)
                        currentChunkDraw32[num2 + index3] = (byte)0;
                    else if (!skipChunkCenter || index3 * (index3 ^ 31) * num4 == 0)
                    {
                        num1 = num3 + index3;
                        int num5 = 0;
                        EnumFaceCullMode faceCullMode = block2.FaceCullMode;
                        SmallBoolArray sideOpaque = block2.SideOpaque;
                        int index4 = 5;
                        do
                        {
                            num5 <<= 1;
                            Block neighbourBlock = ___currentChunkBlocksExt[num1 + TileSideEnum.MoveIndex[index4]];
                            int opposite = TileSideEnum.GetOpposite(index4);
                            bool flag = neighbourBlock.SideOpaque[opposite];
                            if (
                                index4 == 4
                                && neighbourBlock.DrawType == EnumDrawType.JSONAndSnowLayer & flag
                                && !block2.AllowSnowCoverage(
                                    (IWorldAccessor)___game,
                                    ___tmpPos.Set(baseX + index3, baseY + index1, baseZ + index2)
                                )
                            )
                                flag = false;
                            switch (faceCullMode)
                            {
                                case EnumFaceCullMode.Default:
                                    if (
                                        !flag
                                        || !sideOpaque[index4]
                                            && block2.DrawType != EnumDrawType.JSON
                                            && block2.DrawType != EnumDrawType.JSONAndSnowLayer
                                    )
                                    {
                                        ++num5;
                                        break;
                                    }
                                    break;
                                case EnumFaceCullMode.NeverCull:
                                    ++num5;
                                    break;
                                case EnumFaceCullMode.Merge:
                                    if (neighbourBlock != block2 && (!sideOpaque[index4] || !flag))
                                    {
                                        ++num5;
                                        break;
                                    }
                                    break;
                                case EnumFaceCullMode.Collapse:
                                    if (
                                        neighbourBlock == block2 && (index4 == 4 || index4 == 0 || index4 == 3)
                                        || neighbourBlock != block2 && (!sideOpaque[index4] || !flag)
                                    )
                                    {
                                        ++num5;
                                        break;
                                    }
                                    break;
                                case EnumFaceCullMode.MergeMaterial:
                                    if (
                                        !block2.SideSolid[index4]
                                        || neighbourBlock.BlockMaterial != block2.BlockMaterial && (!sideOpaque[index4] || !flag)
                                        || !neighbourBlock.SideSolid[opposite]
                                    )
                                    {
                                        ++num5;
                                        break;
                                    }
                                    break;
                                case EnumFaceCullMode.CollapseMaterial:
                                    if (neighbourBlock.BlockMaterial == block2.BlockMaterial)
                                    {
                                        if (index4 == 0 || index4 == 3)
                                        {
                                            ++num5;
                                            break;
                                        }
                                        break;
                                    }
                                    if (!flag || index4 < 4 && !sideOpaque[index4])
                                    {
                                        ++num5;
                                        break;
                                    }
                                    break;
                                case EnumFaceCullMode.Liquid:
                                    if (neighbourBlock.BlockMaterial != block2.BlockMaterial)
                                    {
                                        if (index4 == 4)
                                        {
                                            ++num5;
                                            break;
                                        }
                                        FastVec3i fastVec3i = TileSideEnum.OffsetByTileSide[index4];
                                        if (
                                            !neighbourBlock.SideIsSolid(
                                                ___tmpPos.Set(
                                                    baseX + index3 + fastVec3i.X,
                                                    baseY + index1 + fastVec3i.Y,
                                                    baseZ + index2 + fastVec3i.Z
                                                ),
                                                opposite
                                            )
                                        )
                                        {
                                            ++num5;
                                            break;
                                        }
                                        break;
                                    }
                                    break;
                                case EnumFaceCullMode.Callback:
                                    if (!block2.ShouldMergeFace(index4, neighbourBlock, num2 + index3))
                                    {
                                        ++num5;
                                        break;
                                    }
                                    break;
                                case EnumFaceCullMode.MergeSnowLayer:
                                    int index5 = num1 + TileSideEnum.MoveIndex[index4] - 1156;
                                    // Our code
                                    if (
                                        index5 >= 0
                                        && index5 < ___currentChunkBlocksExt.Length
                                        && SlabGroupHelper.IsSlab(___currentChunkBlocksExt[index5].BlockId)
                                    )
                                    {
                                        ++num5;
                                        break;
                                    }
                                    // TODO: Convert to transpiler
                                    if (
                                        index4 == 4
                                        || !flag
                                            && (
                                                index4 == 5
                                                || (double)neighbourBlock.GetSnowLevel((BlockPos)null)
                                                    < (double)block2.GetSnowLevel((BlockPos)null)
                                            )
                                        || neighbourBlock.DrawType == EnumDrawType.JSONAndSnowLayer
                                            && index5 >= 0
                                            && index5 < ___currentChunkBlocksExt.Length
                                            && !___currentChunkBlocksExt[index5]
                                                .AllowSnowCoverage(
                                                    (IWorldAccessor)___game,
                                                    ___tmpPos.Set(baseX + index3, baseY + index1, baseZ + index2)
                                                )
                                    )
                                    {
                                        ++num5;
                                        break;
                                    }
                                    break;
                                case EnumFaceCullMode.FlushExceptTop:
                                    switch (index4)
                                    {
                                        case 4:
                                            ++num5;
                                            break;
                                        case 5:
                                            if (flag)
                                                break;
                                            goto case 4;
                                        default:
                                            if (neighbourBlock == block2)
                                                break;
                                            goto case 5;
                                    }
                                    break;
                                case EnumFaceCullMode.Stairs:
                                    if (!flag && (neighbourBlock != block2 || block2.SideOpaque[index4]) || index4 == 4)
                                    {
                                        ++num5;
                                        break;
                                    }
                                    break;
                            }
                        } while (index4-- != 0);
                        if (block2.DrawType == EnumDrawType.JSONAndWater)
                            num5 |= 64;
                        currentChunkDraw32[num2 + index3] = (byte)num5;
                    }
                }
                num2 += 32;
            }
        }
        __result = num1 > 0;
        return false;
    }
}
