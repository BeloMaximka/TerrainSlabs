using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TerrainSlabs.Source.HarmonyPatches;

public static class RenderersPatch
{
    public static void PatchAllRenderers(Harmony harmony)
    {
        Type[] targets =
        [
            typeof(KnappingRenderer),
            typeof(FirepitContentsRenderer),
            typeof(AnvilWorkItemRenderer),
            typeof(ForgeContentsRenderer),
            typeof(ClayFormRenderer),
            typeof(PotInFirepitRenderer),
        ];

        MethodInfo transpiler = AccessTools.Method(typeof(RenderersPatch), nameof(HandleOffsetBlocks));
        foreach (var target in targets)
        {
            var original = AccessTools.Method(target, "OnRenderFrame");
            harmony.Patch(original, transpiler: new HarmonyMethod(transpiler));
        }
    }

    private static IEnumerable<CodeInstruction> HandleOffsetBlocks(MethodBase original, IEnumerable<CodeInstruction> instructions)
    {
        if (original.DeclaringType is not Type rendererType)
        {
            return instructions;
        }

        MethodInfo matrixIdentityMethod = AccessTools.Method(typeof(Matrixf), nameof(Matrixf.Identity));
        FieldInfo apiField = AccessTools.Field(rendererType, "api");
        apiField ??= AccessTools.Field(rendererType, "capi");

        CodeMatcher matcher = new(instructions);
        while (true) // append to all matrix.Identity()
        {
            matcher.MatchEndForward(CodeMatch.Calls(matrixIdentityMethod));
            if (matcher.IsInvalid)
                break;

            matcher
                .Advance(1)
                // Call OffsetMatrix(matrix, this.api, this.pos) after matrix.Identity()
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, apiField),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    CodeInstruction.LoadField(rendererType, "pos"),
                    CodeInstruction.Call(typeof(RenderersPatch), nameof(OffsetMatrix))
                );
        }

        return matcher.InstructionEnumeration();
    }

    private static Matrixf OffsetMatrix(Matrixf matrix, ICoreClientAPI api, BlockPos pos)
    {
        if (SlabHelper.IsSlab(api.World.BlockAccessor.GetBlockBelow(pos).BlockId))
        {
            matrix.Translate(0, -0.5f, 0);
        }
        return matrix;
    }
}
