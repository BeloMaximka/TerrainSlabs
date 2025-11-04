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
        Type[] onRenderFrameTargets =
        [
            typeof(KnappingRenderer),
            typeof(FirepitContentsRenderer),
            typeof(AnvilWorkItemRenderer),
            typeof(ForgeContentsRenderer),
            typeof(ClayFormRenderer),
            typeof(PotInFirepitRenderer),
        ];

        Type[] renderRecipeOutLineTargets = [typeof(KnappingRenderer), typeof(ClayFormRenderer), typeof(AnvilWorkItemRenderer)];

        MethodInfo transpiler = AccessTools.Method(typeof(RenderersPatch), nameof(HandleOffsetBlocks));
        foreach (var target in onRenderFrameTargets)
        {
            var original = AccessTools.Method(target, "OnRenderFrame");
            harmony.Patch(original, transpiler: new HarmonyMethod(transpiler));
        }
        foreach (var target in renderRecipeOutLineTargets)
        {
            var original = AccessTools.Method(target, "RenderRecipeOutLine");
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
        MethodInfo matrixSetMethod = AccessTools.Method(typeof(Matrixf), nameof(Matrixf.Set), [typeof(float[])]);

        CodeMatcher matcher = new(instructions);
        InsertAfter(matcher, rendererType, matrixIdentityMethod);
        InsertAfter(matcher, rendererType, matrixSetMethod);

        return matcher.InstructionEnumeration();
    }

    private static void InsertAfter(CodeMatcher matcher, Type rendererType, MethodInfo target)
    {
        FieldInfo apiField = AccessTools.Field(rendererType, "api");
        apiField ??= AccessTools.Field(rendererType, "capi");

        matcher.Start(); // reset to beginning for each search

        while (true) // append to all matrix.Identity/Set()
        {
            matcher.MatchEndForward(CodeMatch.Calls(target));
            if (matcher.IsInvalid)
                break;

            matcher
                .Advance(1)
                // Call OffsetMatrix(matrix, this.api, this.pos) after matrix.Identity/Set()
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, apiField),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    CodeInstruction.LoadField(rendererType, "pos"),
                    CodeInstruction.Call(typeof(RenderersPatch), nameof(OffsetMatrix))
                );
        }
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
