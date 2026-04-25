using HarmonyLib;
using PlaceOnSlabs.Source.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace PlaceOnSlabs.Source.HarmonyPatches;

public static class ParticlesManagerPatch
{
    [ThreadStatic]
    private static BlockPos? cachedPos;

    public static void PatchAllParticleCode(Harmony harmony)
    {
        (Type, string)[] targets = [(typeof(Block), "OnAsyncClientParticleTick")];
        MethodInfo transpiler = AccessTools.Method(typeof(ParticlesManagerPatch), nameof(OffsetParticlesForSlabs));
        foreach (var target in targets)
        {
            var original = AccessTools.Method(target.Item1, target.Item2);
            harmony.Patch(original, transpiler: new HarmonyMethod(transpiler));
        }
    }

    private static IEnumerable<CodeInstruction> OffsetParticlesForSlabs(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        MethodInfo? spawnParticlesMethod = AccessTools.Method(typeof(IAsyncParticleManager), nameof(IAsyncParticleManager.Spawn));
        if (spawnParticlesMethod is null)
        {
            return instructions;
        }

        var managerLocal = generator.DeclareLocal(typeof(IAsyncParticleManager));
        var particleLocal = generator.DeclareLocal(typeof(IParticlePropertiesProvider));
        CodeMatcher matcher = new(instructions);

        while (true)
        {
            matcher.MatchStartForward(CodeMatch.Calls(spawnParticlesMethod));
            if (matcher.IsInvalid)
                break;

            matcher
                .InsertAndAdvance(
                    // store locals
                    new CodeInstruction(OpCodes.Stloc, particleLocal),
                    new CodeInstruction(OpCodes.Stloc, managerLocal),
                    // load locals to call our mutator
                    new CodeInstruction(OpCodes.Ldloc, managerLocal),
                    new CodeInstruction(OpCodes.Ldloc, particleLocal),
                    // call mutator
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ParticlesManagerPatch), nameof(OffsetParticle))),
                    // reload locals to original call
                    new CodeInstruction(OpCodes.Ldloc, managerLocal),
                    new CodeInstruction(OpCodes.Ldloc, particleLocal)
                )
                .Advance(1);
        }

        return matcher.InstructionEnumeration();
    }

    private static void OffsetParticle(IAsyncParticleManager manager, IParticlePropertiesProvider particles)
    {
        cachedPos ??= new(Dimensions.NormalWorld);
        cachedPos.Set((int)particles.Pos.X, (int)particles.Pos.Y - 1, (int)particles.Pos.Z);
        if (SlabHelper.IsSlab(manager.BlockAccess.GetBlock(cachedPos, BlockLayersAccess.MostSolid).BlockId))
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
