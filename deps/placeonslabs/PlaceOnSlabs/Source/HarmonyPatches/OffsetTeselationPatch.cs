using HarmonyLib;
using PlaceOnSlabs.Source.Utils;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace PlaceOnSlabs.Source.HarmonyPatches;

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
        MethodInfo method = AccessTools.Method(typeof(SlabHelper), nameof(SlabHelper.GetYOffsetFromBlocksWithFluids));

        FieldInfo blockListField = AccessTools.Field(typeof(ChunkTesselator), "currentChunkBlocksExt");
        FieldInfo fluidBlockListField = AccessTools.Field(typeof(ChunkTesselator), "currentChunkFluidBlocksExt");
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
                // current block
                new CodeInstruction(OpCodes.Ldarg_1),
                // block below
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
                // fluid block below
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, fluidBlockListField),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, varsField),
                new CodeInstruction(OpCodes.Ldfld, extIndex3dField),
                new CodeInstruction(OpCodes.Ldsfld, moveIndexField),
                new CodeInstruction(OpCodes.Ldc_I4_5),
                new CodeInstruction(OpCodes.Ldelem_I4),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Ldelem_Ref),
                // call method
                new CodeInstruction(OpCodes.Call, method),
                new CodeInstruction(OpCodes.Add)
            )
            .InstructionEnumeration();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ChunkTesselator), nameof(ChunkTesselator.CalculateVisibleFaces))]
    public static IEnumerable<CodeInstruction> FixSnowFaceCulling(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        FieldInfo moveIndexField = AccessTools.Field(typeof(TileSideEnum), nameof(TileSideEnum.MoveIndex));

        CodeMatcher matcher = new CodeMatcher(instructions, generator)
            // Set label at any break statement because they are similar
            .MatchStartForward(
                CodeMatch.LoadsLocal(),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(OpCodes.Add),
                CodeMatch.StoresLocal(),
                new CodeMatch(OpCodes.Br)
            )
            .ThrowIfNotMatchForward("Could not find ++num5; break;")
            .CreateLabel(out Label label)
            .MatchEndForward(
                CodeMatch.LoadsLocal(false, "num1"),
                CodeMatch.LoadsField(moveIndexField),
                CodeMatch.LoadsLocal(false, "index4"),
                new CodeMatch(OpCodes.Ldelem_I4),
                new CodeMatch(OpCodes.Add),
                CodeMatch.LoadsConstant(1156),
                new CodeMatch(OpCodes.Sub),
                CodeMatch.StoresLocal("index5")
            )
            .ThrowIfNotMatchForward("Could not  int index5 = num1 + TileSideEnum.MoveIndex[index4] - 1156;");

        var index5 = matcher.Instruction.operand;

        return matcher
            .Advance(1)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_S, index5),
                CodeInstruction.LoadArgument(0),
                CodeInstruction.LoadField(typeof(ChunkTesselator), "currentChunkBlocksExt"),
                CodeInstruction.Call(typeof(OffsetTeselationPatch), nameof(ShouldIgnoreCulling)),
                new CodeInstruction(OpCodes.Brtrue, label)
            )
            .InstructionEnumeration();
    }

    private static bool ShouldIgnoreCulling(int indexBelow, Block[] blocks)
    {
        return indexBelow >= 0 && indexBelow < blocks.Length && SlabHelper.IsSlab(blocks[indexBelow].BlockId);
    }
}
