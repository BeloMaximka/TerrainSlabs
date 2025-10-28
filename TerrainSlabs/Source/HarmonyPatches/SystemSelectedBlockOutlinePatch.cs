using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;

namespace TerrainSlabs.Source.HarmonyPatches;

[HarmonyPatch(typeof(SystemSelectedBlockOutline), nameof(SystemSelectedBlockOutline.OnRenderFrame3DPost))]
public static class SystemSelectedBlockOutlinePatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        MethodInfo method = AccessTools.Method(typeof(AABBIntersectionTestPatch), nameof(GetOffset));

        FieldInfo gameField = AccessTools.Field(typeof(SystemSelectedBlockOutline), "game");
        FieldInfo worldMapField = AccessTools.Field(gameField.FieldType, "WorldMap");
        FieldInfo relaxedField = AccessTools.Field(worldMapField.FieldType, "RelaxedBlockAccess");

        MethodInfo blockSelectionGetter = AccessTools.PropertyGetter(gameField.FieldType, "BlockSelection");
        FieldInfo positionGetter = AccessTools.Field(blockSelectionGetter.ReturnType, "Position");

        MethodInfo entityGetter = AccessTools.PropertyGetter(typeof(IPlayer), "Entity");
        FieldInfo cameraField = AccessTools.Field(entityGetter.ReturnType, "CameraPosOffset");
        FieldInfo yField = AccessTools.Field(cameraField.FieldType, "Y");

        return new CodeMatcher(instructions, generator)
            .MatchEndForward(
                new CodeMatch(OpCodes.Callvirt, entityGetter),
                new CodeMatch(OpCodes.Ldfld, cameraField),
                new CodeMatch(OpCodes.Ldfld, yField),
                new CodeMatch(OpCodes.Add)
            )
            .ThrowIfNotMatchForward("Could not find this.game.Player.Entity.CameraPosOffset.Y")
            .Advance(1)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, gameField),
                new CodeInstruction(OpCodes.Ldfld, worldMapField),
                new CodeInstruction(OpCodes.Ldfld, relaxedField),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, gameField),
                new CodeInstruction(OpCodes.Callvirt, blockSelectionGetter),
                new CodeInstruction(OpCodes.Ldfld, positionGetter),
                new CodeInstruction(OpCodes.Call, method),
                new CodeInstruction(OpCodes.Add)
            )
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
