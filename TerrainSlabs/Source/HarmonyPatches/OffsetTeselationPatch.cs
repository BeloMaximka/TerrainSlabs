using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
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
    [HarmonyTranspiler]
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
    public static IEnumerable<CodeInstruction> HandleOffsetBlocks(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        MethodInfo method = AccessTools.Method(typeof(SlabGroupHelper), nameof(SlabGroupHelper.GetYOffsetFromBlocks));

        FieldInfo blockListField = AccessTools.Field(typeof(ChunkTesselator), "currentChunkBlocksExt");
        FieldInfo varsField = AccessTools.Field(typeof(ChunkTesselator), "vars");
        FieldInfo extIndex3dField = AccessTools.Field(varsField.FieldType, "extIndex3d");
        FieldInfo lyField = AccessTools.Field(varsField.FieldType, "ly");
        FieldInfo moveIndexField = AccessTools.Field(typeof(TileSideEnum), nameof(TileSideEnum.MoveIndex));

        return new CodeMatcher(instructions, generator)
            .MatchEndForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, varsField),
                new CodeMatch(OpCodes.Ldfld, lyField),
                new CodeMatch(OpCodes.Conv_R4)
            )
            .ThrowIfNotMatchForward("Could not find this.vars.finalY = (float) this.vars.ly")
            .Advance(1)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, blockListField),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, varsField),
                new CodeInstruction(OpCodes.Ldfld, extIndex3dField),
                new CodeInstruction(OpCodes.Ldsfld, moveIndexField),
                new CodeInstruction(OpCodes.Ldc_I4_5),
                new CodeInstruction(OpCodes.Ldelem_I4),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Ldelem_Ref),
                new CodeInstruction(OpCodes.Call, method),
                new CodeInstruction(OpCodes.Add)
            )
            .InstructionEnumeration();
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
