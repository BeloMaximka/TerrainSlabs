using HarmonyLib;
using PlaceOnSlabs.Source.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace PlaceOnSlabs.Source.HarmonyPatches;

public static class WorldAccessorParticlesPatch
{
    [ThreadStatic]
    private static BlockPos? cachedPos;

    public static void PatchAllParticleCode(Harmony harmony)
    {
        (Type, string)[] targets =
        [
            (typeof(BlockEntityCoalPile), "SpawnBurningCoalParticles"),
            (typeof(BlockEntityKnappingSurface), "spawnParticles"),
            (typeof(BlockEntityAnvil), "spawnParticles"),
        ];
        MethodInfo transpiler = AccessTools.Method(typeof(WorldAccessorParticlesPatch), nameof(OffsetParticlesForSlabs));
        foreach (var target in targets)
        {
            var original = AccessTools.Method(target.Item1, target.Item2);
            harmony.Patch(original, transpiler: new HarmonyMethod(transpiler));
        }
    }

    private static IEnumerable<CodeInstruction> OffsetParticlesForSlabs(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        MethodInfo? spawnParticlesMethod = AccessTools.Method(
            typeof(IWorldAccessor),
            nameof(IWorldAccessor.SpawnParticles),
            [typeof(IParticlePropertiesProvider), typeof(IPlayer)]
        );
        if (spawnParticlesMethod is null)
        {
            return instructions;
        }

        var accessorLocal = generator.DeclareLocal(typeof(IWorldAccessor));
        var providerLocal = generator.DeclareLocal(typeof(IParticlePropertiesProvider));
        var playerLocal = generator.DeclareLocal(typeof(IPlayer));
        CodeMatcher matcher = new(instructions);

        while (true)
        {
            matcher.MatchStartForward(CodeMatch.Calls(spawnParticlesMethod));
            if (matcher.IsInvalid)
                break;

            matcher
                .InsertAndAdvance(
                    // store player -> local2
                    new CodeInstruction(OpCodes.Stloc, playerLocal),
                    // store provider -> local1
                    new CodeInstruction(OpCodes.Stloc, providerLocal),
                    // store accessor -> local0
                    new CodeInstruction(OpCodes.Stloc, accessorLocal),
                    // load accessor + provider to call our mutator
                    new CodeInstruction(OpCodes.Ldloc, accessorLocal),
                    new CodeInstruction(OpCodes.Ldloc, providerLocal),
                    // call mutator
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(WorldAccessorParticlesPatch), nameof(OffsetParticle))),
                    // reload accessor, provider, player for the original SpawnParticles call
                    new CodeInstruction(OpCodes.Ldloc, accessorLocal),
                    new CodeInstruction(OpCodes.Ldloc, providerLocal),
                    new CodeInstruction(OpCodes.Ldloc, playerLocal)
                )
                .Advance(1);
        }

        return matcher.InstructionEnumeration();
    }

    private static void OffsetParticle(IWorldAccessor accessor, IParticlePropertiesProvider particles)
    {
        cachedPos ??= new(Dimensions.NormalWorld);
        cachedPos.Set((int)particles.Pos.X, (int)particles.Pos.Y - 1, (int)particles.Pos.Z);
        if (SlabHelper.IsSlab(accessor.BlockAccessor.GetBlockId(cachedPos)))
        {
            if (particles is AdvancedParticleProperties advancedParticle)
            {
                advancedParticle.basePos.Y -= 0.5;
                return;
            }
            if (particles is SimpleParticleProperties simpleParticle)
            {
                simpleParticle.MinPos.Y -= 0.5;
            }
        }
    }
}
