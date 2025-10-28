using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;

namespace TerrainSlabs.Source.HarmonyPatches;

[HarmonyPatch(typeof(SystemRenderDecals), "UpdateDecal")]
public static class BlockDamagePatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var codes = new List<CodeInstruction>(instructions);

        MethodInfo method = AccessTools.Method(typeof(BlockDamagePatch), nameof(GetOffset));
        if (method == null)
            return codes;

        FieldInfo decalOriginField = AccessTools.Field(typeof(SystemRenderDecals), "decalOrigin");
        if (decalOriginField == null)
            return codes;
        var blockDecalType = Type.GetType("Vintagestory.Client.NoObf.BlockDecal, VintagestoryLib");
        if (blockDecalType == null)
            return codes;
        FieldInfo decalPosField = AccessTools.Field(blockDecalType, "pos");
        if (decalPosField == null)
            return codes;

        FieldInfo gameField = AccessTools.Field(typeof(SystemRenderDecals), "game");
        if (gameField == null)
            return codes;
        FieldInfo worldMapField = AccessTools.Field(gameField.FieldType, "WorldMap");
        if (worldMapField == null)
            return codes;
        FieldInfo relaxedField = AccessTools.Field(worldMapField.FieldType, "RelaxedBlockAccess");
        if (relaxedField == null)
            return codes;

        FieldInfo yField = AccessTools.Field(decalOriginField.FieldType, "Y") ?? AccessTools.Field(decalOriginField.FieldType, "y");
        if (yField == null)
            return codes;

        return new CodeMatcher(codes, generator)
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
            .MatchStartForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, decalOriginField),
                new CodeMatch(OpCodes.Ldfld, yField),
                new CodeMatch(OpCodes.Sub)
            )
            .ThrowIfNotMatchForward("Could not find (float)decal.pos.Y - (float)this.decalOrigin.Y")
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc, localVariable.LocalIndex), new CodeInstruction(OpCodes.Add))
            .InstructionEnumeration();
    }

    public static double GetOffset(IBlockAccessor accessor, BlockPos pos)
    {
        if (SlabGroupHelper.IsSlab(accessor.GetBlockBelow(pos).BlockId))
        {
            return -0.5f;
        }
        return 0;
    }
}
