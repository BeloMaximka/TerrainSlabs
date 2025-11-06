using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace TerrainSlabs.Source.HarmonyPatches;

internal struct DecorOffset
{
    public float uvOffset;
    public float topVertexOffset;
    public float nonBottomVertexOffset;
}

[HarmonyPatch]
public static class DecorOverlayPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SurfaceLayerTesselator), nameof(SurfaceLayerTesselator.DrawBlockFace))]
    private static IEnumerable<CodeInstruction> FixDecorForSlabs2(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        CodeMatcher matcher = new CodeMatcher(instructions, generator)
            .DeclareLocal(typeof(DecorOffset), out LocalBuilder decorOffset)
            .Start()
            .InsertAndAdvance(
                CodeInstruction.LoadArgument(1), // vars
                CodeInstruction.LoadField(typeof(TCTCache), "tct"), // vars.tct
                CodeInstruction.LoadField(typeof(ChunkTesselator), "currentChunkBlocksExt"), // vars.tct.currentChunkBlocksExt
                CodeInstruction.LoadArgument(1), // vars
                CodeInstruction.LoadArgument(2), // tileSide
                CodeInstruction.LoadArgument(4), // texPos
                CodeInstruction.LoadArgument(9), // rotIndex
                CodeInstruction.Call(typeof(DecorOverlayPatch), nameof(CalcDecorOffset)),
                CodeInstruction.StoreLocal(decorOffset.LocalIndex)
            );

        int uvyIndex = 4;
        MethodInfo normalfGetter = AccessTools.PropertyGetter(typeof(BlockFacing), nameof(BlockFacing.Normalf));
        matcher
            .MatchEndForward(CodeMatch.Calls(normalfGetter))
            .ThrowIfNotMatchForward("Could not find normalf =")
            .Advance(2)
            .InsertAfterAndAdvance(
                CodeInstruction.LoadLocal(uvyIndex),
                CodeInstruction.LoadLocal(decorOffset.LocalIndex),
                CodeInstruction.LoadField(typeof(DecorOffset), nameof(DecorOffset.uvOffset)),
                new CodeInstruction(OpCodes.Sub),
                CodeInstruction.StoreLocal(uvyIndex)
            );

        var fastVec3fField = AccessTools.Field(typeof(FastVec3f), nameof(FastVec3f.Y));

        matcher
            .MatchEndForward(CodeMatch.LoadsLocal(false, "quadOffset1"), CodeMatch.LoadsField(fastVec3fField))
            .ThrowIfNotMatchForward("Could not find quadOffset1.Y")
            .Advance(3)
            .InsertAfterAndAdvance(
                CodeInstruction.LoadLocal(decorOffset.LocalIndex),
                CodeInstruction.LoadField(typeof(DecorOffset), nameof(DecorOffset.topVertexOffset)),
                new CodeInstruction(OpCodes.Sub)
            );

        matcher
            .MatchEndForward(CodeMatch.LoadsLocal(false, "quadOffset2"), CodeMatch.LoadsField(fastVec3fField))
            .ThrowIfNotMatchForward("Could not find quadOffset2.Y")
            .Advance(3)
            .InsertAfterAndAdvance(
                CodeInstruction.LoadLocal(decorOffset.LocalIndex),
                CodeInstruction.LoadField(typeof(DecorOffset), nameof(DecorOffset.nonBottomVertexOffset)),
                new CodeInstruction(OpCodes.Sub)
            );

        matcher
            .MatchEndForward(CodeMatch.LoadsLocal(false, "quadOffset3"), CodeMatch.LoadsField(fastVec3fField))
            .ThrowIfNotMatchForward("Could not find quadOffset3.Y")
            .Advance(3)
            .InsertAfterAndAdvance(
                CodeInstruction.LoadLocal(decorOffset.LocalIndex),
                CodeInstruction.LoadField(typeof(DecorOffset), nameof(DecorOffset.nonBottomVertexOffset)),
                new CodeInstruction(OpCodes.Sub)
            );

        matcher
            .MatchEndForward(CodeMatch.LoadsLocal(false, "quadOffset4"), CodeMatch.LoadsField(fastVec3fField))
            .ThrowIfNotMatchForward("Could not find quadOffset4.Y")
            .Advance(3)
            .InsertAfterAndAdvance(
                CodeInstruction.LoadLocal(decorOffset.LocalIndex),
                CodeInstruction.LoadField(typeof(DecorOffset), nameof(DecorOffset.topVertexOffset)),
                new CodeInstruction(OpCodes.Sub)
            );

        return matcher.InstructionEnumeration();
    }

    private static DecorOffset CalcDecorOffset(Block[] blocks, TCTCache vars, int tileSide, TextureAtlasPosition texPos, int rotIndex)
    {
        DecorOffset offset = new();
        if (SlabHelper.IsSlab(blocks[vars.extIndex3d]))
        {
            if ((tileSide != BlockFacing.indexUP && tileSide != BlockFacing.indexDOWN))
            {
                offset.uvOffset = (texPos.y1 - texPos.y2) / 2;
                if (rotIndex == 3) // figure this shit by yourself
                {
                    offset.uvOffset = (texPos.y2 - texPos.y1) / 2;
                }
            }

            offset.topVertexOffset = (tileSide == BlockFacing.indexDOWN) ? 0.5f : 0;
            offset.nonBottomVertexOffset = (tileSide != BlockFacing.indexUP) ? 0.5f : 0;
        }

        return offset;
    }
}
