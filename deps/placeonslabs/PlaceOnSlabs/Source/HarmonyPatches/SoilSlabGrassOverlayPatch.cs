using HarmonyLib;
using PlaceOnSlabs.Source.Utils;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace PlaceOnSlabs.Source.HarmonyPatches;

[HarmonyPatch]
public static class SoilSlabGrassOverlayPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(TopsoilTesselator), "DrawBlockFaceTopSoil")]
    public static IEnumerable<CodeInstruction> HandleSoilSlabBlocks(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        FieldInfo yField = AccessTools.Field(typeof(FastVec3f), nameof(FastVec3f.Y));
        FieldInfo x1Field = AccessTools.Field(typeof(TextureAtlasPosition), nameof(TextureAtlasPosition.x1));
        FieldInfo x2Field = AccessTools.Field(typeof(TextureAtlasPosition), nameof(TextureAtlasPosition.x2));
        FieldInfo y1Field = AccessTools.Field(typeof(TextureAtlasPosition), nameof(TextureAtlasPosition.y1));
        FieldInfo y2Field = AccessTools.Field(typeof(TextureAtlasPosition), nameof(TextureAtlasPosition.y2));

        return new CodeMatcher(instructions, generator)
            .Start()
            .DeclareLocal(typeof(float), out LocalBuilder offsetY)
            .InsertAndAdvance(
                CodeInstruction.LoadArgument(1),
                CodeInstruction.LoadField(typeof(TCTCache), nameof(TCTCache.blockId)),
                CodeInstruction.Call(typeof(SoilSlabGrassOverlayPatch), nameof(GetYMutiplier)),
                CodeInstruction.StoreLocal(offsetY.LocalIndex)
            )
            // Make side half height
            .MatchEndForward(CodeMatch.LoadsLocal(false, "quadOffset1"), CodeMatch.LoadsField(yField))
            .ThrowIfNotMatchForward("Could not find quadOffset1.Y")
            .Advance(1)
            .InsertAndAdvance(CodeInstruction.LoadLocal(offsetY.LocalIndex), new CodeInstruction(OpCodes.Mul))
            // Make side half height
            .MatchEndForward(CodeMatch.LoadsLocal(false, "quadOffset2"), CodeMatch.LoadsField(yField))
            .ThrowIfNotMatchForward("Could not find quadOffset2.Y")
            .Advance(1)
            .InsertAndAdvance(CodeInstruction.LoadLocal(offsetY.LocalIndex), new CodeInstruction(OpCodes.Mul))
            // Adjust base texture uv mapping
            .MatchEndForward(
                new CodeMatch(OpCodes.Ldloc_0),
                CodeMatch.LoadsField(x2Field),
                new CodeMatch(OpCodes.Ldloc_0),
                CodeMatch.LoadsField(y1Field)
            )
            .ThrowIfNotMatchForward("Could not find textureAtlasPosition1.x2, textureAtlasPosition1.y1")
            .RemoveInstruction()
            .InsertAndAdvance(
                CodeInstruction.LoadArgument(1),
                CodeInstruction.LoadField(typeof(TCTCache), nameof(TCTCache.blockId)),
                CodeInstruction.LoadArgument(2),
                CodeInstruction.Call(typeof(SoilSlabGrassOverlayPatch), nameof(GetUvMapBottomHalfOffset))
            )
            // Make side half height
            .MatchEndForward(CodeMatch.LoadsLocal(false, "quadOffset3"), CodeMatch.LoadsField(yField))
            .ThrowIfNotMatchForward("Could not find quadOffset3.Y")
            .Advance(1)
            .InsertAndAdvance(CodeInstruction.LoadLocal(offsetY.LocalIndex), new CodeInstruction(OpCodes.Mul))
            // Adjust base texture uv mapping
            .MatchEndForward(
               new CodeMatch(OpCodes.Ldloc_0),
                CodeMatch.LoadsField(x1Field),
                new CodeMatch(OpCodes.Ldloc_0),
                CodeMatch.LoadsField(y1Field)
            )
            .ThrowIfNotMatchForward("Could not find textureAtlasPosition1.x1, textureAtlasPosition1.y1")
            .RemoveInstruction()
            .InsertAndAdvance(
                CodeInstruction.LoadArgument(1),
                CodeInstruction.LoadField(typeof(TCTCache), nameof(TCTCache.blockId)),
                CodeInstruction.LoadArgument(2),
                CodeInstruction.Call(typeof(SoilSlabGrassOverlayPatch), nameof(GetUvMapBottomHalfOffset))
            )
            // Make side half height
            .MatchEndForward(CodeMatch.LoadsLocal(false, "quadOffset4"), CodeMatch.LoadsField(yField))
            .ThrowIfNotMatchForward("Could not find quadOffset4.Y")
            .Advance(1)
            .InsertAndAdvance(CodeInstruction.LoadLocal(offsetY.LocalIndex), new CodeInstruction(OpCodes.Mul))
            // Fix uv side uv mapping
            .MatchStartForward(new CodeMatch(OpCodes.Ldloc_1), CodeMatch.LoadsField(y2Field))
            .ThrowIfNotMatchForward("Could not find textureAtlasPosition2.y2")
            .Advance(1)
            .RemoveInstruction()
            .Insert(
                CodeInstruction.LoadArgument(1),
                CodeInstruction.LoadField(typeof(TCTCache), nameof(TCTCache.blockId)),
                CodeInstruction.LoadArgument(2),
                CodeInstruction.Call(typeof(SoilSlabGrassOverlayPatch), nameof(GetUvMapTopHalfOffset))
            )
            // Change smth important in UpdateChunkMinMax
            .MatchEndForward(CodeMatch.LoadsLocal(false, "ly"), new CodeMatch(OpCodes.Ldc_R4))
            .ThrowIfNotMatchForward("Could not find ly + 1f")
            .Advance(1)
            .InsertAndAdvance(CodeInstruction.LoadLocal(offsetY.LocalIndex), new CodeInstruction(OpCodes.Mul))
            .InstructionEnumeration();
    }

    private static float GetUvMapTopHalfOffset(TextureAtlasPosition atlas, int slabId, int flags)
    {
        if (SlabHelper.IsSlab(slabId))
        {
            bool isTop = (flags & BlockFacing.ALLFACES[BlockFacing.indexUP].NormalPackedFlags) != 0;
            return isTop ? atlas.y2 : atlas.y2 - (atlas.y2 - atlas.y1) / 2;
        }
        return atlas.y2;
    }

    private static float GetUvMapBottomHalfOffset(TextureAtlasPosition atlas, int slabId, int flags)
    {
        if (SlabHelper.IsSlab(slabId))
        {
            bool isTop = (flags & BlockFacing.ALLFACES[BlockFacing.indexUP].NormalPackedFlags) != 0;
            return isTop ? atlas.y1 : atlas.y2 - (atlas.y2 - atlas.y1) / 2;
        }
        return atlas.y1;
    }

    private static float GetYMutiplier(int slabId)
    {
        return SlabHelper.IsSlab(slabId) ? 0.5f : 1f;
    }
}
