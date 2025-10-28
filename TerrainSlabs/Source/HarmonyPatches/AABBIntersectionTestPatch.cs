using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

#pragma warning disable S101 // Types should be named in PascalCase

namespace TerrainSlabs.Source.HarmonyPatches;

[HarmonyPatch(typeof(AABBIntersectionTest), nameof(AABBIntersectionTest.RayIntersectsBlockSelectionBox))]
public static class AABBIntersectionTestPatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        MethodInfo method = AccessTools.Method(typeof(AABBIntersectionTestPatch), nameof(GetOffset));
        FieldInfo bsTesterField = AccessTools.Field(typeof(AABBIntersectionTest), "bsTester");
        MethodInfo accessorGetter = AccessTools.PropertyGetter(typeof(IWorldIntersectionSupplier), "blockAccessor");
        MethodInfo internalYGetter = AccessTools.PropertyGetter(typeof(BlockPos), nameof(BlockPos.InternalY));

        return new CodeMatcher(instructions, generator)
            .Start()
            .DeclareLocal(typeof(double), out LocalBuilder localVariable)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, bsTesterField),
                new CodeInstruction(OpCodes.Callvirt, accessorGetter),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Call, method),
                new CodeInstruction(OpCodes.Stloc, localVariable.LocalIndex)
            )
            .MatchEndForward(
                new CodeMatch(OpCodes.Ldarg_1),
                new CodeMatch(OpCodes.Callvirt, internalYGetter),
                new CodeMatch(OpCodes.Conv_R8)
            )
            .Advance(1)
            .ThrowIfNotMatchForward("Could not find pos.InternalY")
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc, localVariable.LocalIndex), new CodeInstruction(OpCodes.Add))
            .InstructionEnumeration();
    }

    private static double GetOffset(IBlockAccessor accessor, BlockPos pos)
    {
        if (SlabGroupHelper.ShouldOffset(accessor.GetBlockId(pos)) && SlabGroupHelper.IsSlab(accessor.GetBlockBelow(pos).BlockId))
        {
            return -0.5d;
        }
        return 0d;
    }
}
