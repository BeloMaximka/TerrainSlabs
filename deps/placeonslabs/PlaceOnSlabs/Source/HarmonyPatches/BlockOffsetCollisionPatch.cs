using HarmonyLib;
using PlaceOnSlabs.Source.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace PlaceOnSlabs.Source.HarmonyPatches;

[HarmonyPatch]
public static class BlockOffsetCollisionPatch
{
    private static readonly ConditionalWeakTable<Cuboidf[], Cuboidf[]> OffsetCache = [];

    public static void PatchAllBlocks(Harmony harmony)
    {
        MethodInfo postfix = AccessTools.Method(typeof(BlockOffsetCollisionPatch), nameof(OffsetColisionBox));
        List<Type> blockAndSubclasses = [typeof(Block)];

        // add all non-abstract subclasses of Block
        blockAndSubclasses.AddRange(
            AppDomain
                .CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOf(blockAndSubclasses[0]))
        );

        foreach (var blockType in blockAndSubclasses)
        {
            MethodInfo? implementedMethod = blockType.GetMethod(
                nameof(Block.GetCollisionBoxes),
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                null,
                [typeof(IBlockAccessor), typeof(BlockPos)],
                null
            );

            if (implementedMethod is null)
            {
                // patch only implemented methods
                continue;
            }

            harmony.Patch(implementedMethod, postfix: new HarmonyMethod(postfix));
        }
    }

    private static void OffsetColisionBox(Block __instance, ref Cuboidf[] __result, object[] __args)
    {
        // we use __args because Harmony is strict about parameter names
        // and BlockMultiBlock declares IBlockAccessor as ba for some reason
        if (__result is null || __args[0] is not IBlockAccessor blockAccessor || __args[1] is not BlockPos pos)
        {
            return;
        }

        pos.Down();
        if (SlabHelper.IsSlab(blockAccessor.GetBlock(pos).BlockId) && SlabHelper.ShouldOffset(__instance))
        {
            __result = OffsetCache.GetValue(
                __result,
                original =>
                {
                    var arr = new Cuboidf[original.Length];
                    for (int i = 0; i < original.Length; i++)
                    {
                        arr[i] = original[i].OffsetCopy(0, -0.5f, 0);
                    }
                    return arr;
                }
            );
        }
        pos.Up();
    }
}
