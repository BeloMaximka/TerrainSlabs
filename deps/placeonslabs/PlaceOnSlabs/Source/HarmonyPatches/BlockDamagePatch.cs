using HarmonyLib;
using PlaceOnSlabs.Source.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Vintagestory.Client.NoObf;

namespace PlaceOnSlabs.Source.HarmonyPatches;

[HarmonyPatch(typeof(SystemRenderDecals), "UpdateDecal")]
public static class BlockDamagePatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        MethodInfo method = AccessTools.Method(typeof(SlabHelper), nameof(SlabHelper.GetYOffsetValue));

        FieldInfo decalOriginField = AccessTools.Field(typeof(SystemRenderDecals), "decalOrigin");
        var blockDecalType = Type.GetType("Vintagestory.Client.NoObf.BlockDecal, VintagestoryLib");
        FieldInfo decalPosField = AccessTools.Field(blockDecalType, "pos");

        FieldInfo gameField = AccessTools.Field(typeof(SystemRenderDecals), "game");
        FieldInfo worldMapField = AccessTools.Field(gameField.FieldType, "WorldMap");
        FieldInfo relaxedField = AccessTools.Field(worldMapField.FieldType, "RelaxedBlockAccess");

        FieldInfo yField = AccessTools.Field(decalOriginField.FieldType, "Y") ?? AccessTools.Field(decalOriginField.FieldType, "y");

        return new CodeMatcher(instructions, generator)
            .Start()
            .DeclareLocal(typeof(float), out LocalBuilder localVariable)
            .InsertAndAdvance(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, gameField),
                new CodeInstruction(OpCodes.Ldfld, worldMapField),
                new CodeInstruction(OpCodes.Ldfld, relaxedField),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldfld, decalPosField),
                new CodeInstruction(OpCodes.Call, method),
                new CodeInstruction(OpCodes.Stloc, localVariable.LocalIndex)
            )
            .MatchEndForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, decalOriginField),
                new CodeMatch(OpCodes.Ldfld, yField),
                new CodeMatch(OpCodes.Sub)
            )
            .Advance(1)
            .ThrowIfNotMatchForward("Could not find (float)decal.pos.Y - (float)this.decalOrigin.Y")
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc, localVariable.LocalIndex), new CodeInstruction(OpCodes.Add))
            .InstructionEnumeration();
    }
}
