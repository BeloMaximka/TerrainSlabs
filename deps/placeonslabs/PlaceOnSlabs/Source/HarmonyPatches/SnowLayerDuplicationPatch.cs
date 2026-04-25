using HarmonyLib;
using PlaceOnSlabs.Source.Utils;
using System.Collections.Generic;
using System.Reflection.Emit;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;

namespace PlaceOnSlabs.Source.HarmonyPatches;

[HarmonyPatch]
public static class SnowLayerDuplicationPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SystemRenderInsideBlock), "OnRenderFrame3D")]
    public static IEnumerable<CodeInstruction> SkipDoubleRenderOnSlabs(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        return new CodeMatcher(instructions, generator)
            .MatchEndForward(CodeMatch.LoadsLocal(false, "block"), new CodeMatch(OpCodes.Brfalse))
            .ThrowIfNotMatchForward("Could not find block != null;")
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.LoadField(typeof(SystemRenderInsideBlock), "game"),
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.LoadField(typeof(SystemRenderInsideBlock), "tmpPos"),
                CodeInstruction.Call(typeof(SnowLayerDuplicationPatch), nameof(IsNotNullOrSlab))
            )
            .InstructionEnumeration();
    }

    private static bool IsNotNullOrSlab(Block? block, ClientMain game, BlockPos pos)
    {
        return block != null && !SlabHelper.IsSlab(game.blockAccessor.GetBlockBelow(pos).BlockId);
    }
}
