using HarmonyLib;
using PlaceOnSlabs.Source.BlockBehaviors;
using PlaceOnSlabs.Source.Commands;
using PlaceOnSlabs.Source.HarmonyPatches;
using PlaceOnSlabs.Source.Utils;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace PlaceOnSlabs.Source.Systems;

public class MainSystem : ModSystem
{
    private Harmony harmonyInstance = null!;

    public override double ExecuteOrder() => 0.2;

    public override void StartPre(ICoreAPI api)
    {
        harmonyInstance = new(Mod.Info.ModID);
        if (!harmonyInstance.GetPatchedMethods().Any())
        {
            harmonyInstance.PatchAll();
            RenderersPatch.PatchAllRenderers(harmonyInstance);
            BlockOffsetCollisionPatch.PatchAllBlocks(harmonyInstance);
            WorldAccessorParticlesPatch.PatchAllParticleCode(harmonyInstance);
            ParticlesManagerPatch.PatchAllParticleCode(harmonyInstance);
        }
    }

    public override void Start(ICoreAPI api)
    {
        api.RegisterBlockBehaviorClass("SlabTopPlacement", typeof(BlockBehaviorSlabTopPlacement));
        api.RegisterBlockBehaviorClass("FixAnimatable", typeof(BlockBehaviorFixAnimatable));

        RecalculateSlabFlagsCommand.Register(api);
    }

    public override void AssetsFinalize(ICoreAPI api)
    {
        SlabHelper.InitFlags(api);

        foreach (var block in api.World.Blocks.Where(block => SlabHelper.IsSlab(block.Id)))
        {
            BlockBehavior[] oldBehaviors = block.BlockBehaviors;
            block.BlockBehaviors = new BlockBehavior[block.BlockBehaviors.Length + 2];
            block.BlockBehaviors[0] = new BlockBehaviorSlabTopPlacement(block);
            block.BlockBehaviors[0].OnLoaded(api);
            block.BlockBehaviors[1] = new BlockBehaviorFixAnimatable(block);
            block.BlockBehaviors[1].OnLoaded(api);
            for (int i = 2; i < block.BlockBehaviors.Length; i++)
            {
                block.BlockBehaviors[i] = oldBehaviors[i - 2];
            }

            block.SideSolid[BlockFacing.indexUP] = true;
        }
    }

    public override void Dispose()
    {
        harmonyInstance.UnpatchAll(harmonyInstance.Id);
    }
}
