using HarmonyLib;
using PlaceOnSlabs.Source.Utils;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

#pragma warning disable S101 // Types should be named in PascalCase

namespace PlaceOnSlabs.Source.HarmonyPatches;

[HarmonyPatch]
public static class AABBIntersectionTestPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(AABBIntersectionTest), nameof(AABBIntersectionTest.RayIntersectsBlockSelectionBox))]
    public static IEnumerable<CodeInstruction> OffsetSelectionBox(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        MethodInfo method = AccessTools.Method(typeof(SlabHelper), nameof(SlabHelper.GetYOffsetValue));
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

    [HarmonyPostfix]
    [HarmonyPatch(
        typeof(AABBIntersectionTest),
        nameof(AABBIntersectionTest.GetSelectedBlock),
        typeof(float),
        typeof(BlockFilter),
        typeof(bool)
    )]
    public static void CheckBlockAboveSlab(
        AABBIntersectionTest __instance,
        ref BlockSelection? __result,
        ref Block ___blockIntersected,
        BlockFilter? filter,
        bool testCollide
    )
    {
        if (__result is null || __instance.ray.dir.Y > 0 || !SlabHelper.IsSlab(___blockIntersected.BlockId))
        {
            return;
        }

        BlockPos pos = __instance.pos;
        pos.Up();
        if (!__instance.RayIntersectsBlockSelectionBox(pos, filter, testCollide))
        {
            return;
        }

        __result = new BlockSelection()
        {
            Face = __instance.hitOnBlockFace,
            Position = pos.CopyAndCorrectDimension(),
            HitPosition = __instance.hitPosition.SubCopy(pos.X, pos.InternalY, pos.Z),
            SelectionBoxIndex = __instance.hitOnSelectionBox,
            Block = ___blockIntersected,
        };
    }
}
