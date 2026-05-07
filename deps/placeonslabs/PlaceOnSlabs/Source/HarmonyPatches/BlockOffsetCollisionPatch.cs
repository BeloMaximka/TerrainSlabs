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

// I really hope there's more performant way
[HarmonyPatch]
public static class BlockOffsetCollisionPatch
{
    private static readonly ConditionalWeakTable<Tuple<uint, Cuboidf[]>, Cuboidf[]> OffsetCache = [];

    public static void PatchAllBlocks(Harmony harmony, ICoreAPI api)
    {
        MethodInfo postfix = AccessTools.Method(typeof(BlockOffsetCollisionPatch), nameof(OffsetColisionBox));
        List<Type> blockAndSubclasses = [typeof(Block)];

        // add all non-abstract subclasses of Block
        blockAndSubclasses.AddRange(
            AppDomain
                .CurrentDomain.GetAssemblies()
                .SelectMany(SafeGetTypes)
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

            try
            {
                harmony.Patch(implementedMethod, postfix: new HarmonyMethod(postfix));
            }
            catch (Exception exception)
            {
                api.Logger.Warning("[placeonslabs] Failed patching {0}.{1}, expect potential issues with this block: {2}\n",
                    implementedMethod.DeclaringType,
                    implementedMethod.Name,
                    exception);
            }
        }
    }


    // Some types might trigger client assemblies that may be missing on the server
    // We can just ignore errors related to that
    // Not a great approach performance wise but blame C# for using exceptions for error handling
    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null)!;
        }
        catch
        {
            return [];
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

        // Offset can be potentially applied multiple times because both leaf and base methods are patched
        // So some method can call base.GetCollisionBoxes() and trigger this behavior twice
        // But we cant omit patching base methods because they can be used separately
        // This check is not perfect but should work most of the time
        for (int i = 0; i < __result.Length; i++)
        {
            if (__result[i].Y1 < 0) // probably lowered into negative by this patch
            {
                return;
            }
        }

        pos.Down();
        int blockId = blockAccessor.GetBlock(pos).BlockId;
        float offset = SlabHelper.GetYOffsetFloat(blockId);
        if (offset < 0 && SlabHelper.shouldOffset[__instance.BlockId])
        {
            __result = OffsetCache.GetValue(
                new(SlabHelper.offset[blockId], __result),
                original =>
                {
                    var box = original.Item2;
                    var arr = new Cuboidf[box.Length];
                    for (int i = 0; i < box.Length; i++)
                    {
                        arr[i] = box[i].OffsetCopy(0, offset, 0);
                    }
                    return arr;
                }
            );
        }
        pos.Up();
    }
}
