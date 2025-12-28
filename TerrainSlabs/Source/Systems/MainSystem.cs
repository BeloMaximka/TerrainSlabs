using HarmonyLib;
using System.Linq;
using TerrainSlabs.Source.BlockBehaviors;
using TerrainSlabs.Source.Blocks;
using TerrainSlabs.Source.Commands;
using TerrainSlabs.Source.Compatibility;
using TerrainSlabs.Source.HarmonyPatches;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace TerrainSlabs.Source.Systems;

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
            WorldAccessorParticlesPatch.PatchAllParticleCode(harmonyInstance);
            ParticlesManagerPatch.PatchAllParticleCode(harmonyInstance);

            CatchLedgePatch.ApplyIfEnabled(api, harmonyInstance);
        }
    }

    public override void Start(ICoreAPI api)
    {
        api.RegisterBlockClass(nameof(BlockForestFloorSlab), typeof(BlockForestFloorSlab));
        api.RegisterBlockClass(nameof(BlockGlacierIceSlab), typeof(BlockGlacierIceSlab));
        api.RegisterBlockClass(nameof(BlockSnowSlab), typeof(BlockSnowSlab));
        api.RegisterBlockClass(nameof(BlockSoilDepositSlab), typeof(BlockSoilDepositSlab));
        api.RegisterBlockClass(nameof(BlockSoilSlab), typeof(BlockSoilSlab));
        api.RegisterBlockClass(nameof(BlockTerrainSlab), typeof(BlockTerrainSlab));

        api.RegisterBlockBehaviorClass("SlabTopPlacement", typeof(BlockBehaviorSlabTopPlacement));
        api.RegisterBlockBehaviorClass("UnstableFallingSlab", typeof(BlockBehaviorUnstableFallingSlab));
        api.RegisterBlockBehaviorClass("FixAnimatable", typeof(BlockBehaviorFixAnimatable));
        api.RegisterBlockBehaviorClass("NameFromFullBlock", typeof(BlockBehaviorNameFromFullBlock));

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

    public override void StartServerSide(ICoreServerAPI api)
    {
        WorldGenUtils.RegisterSlabReplacementWorldGenEvent(api);

        AlterTerrainCommand.Register(api);
        SmoothBlockCommand.Register(api);
    }

    public override void Dispose()
    {
        harmonyInstance.UnpatchAll(harmonyInstance.Id);
    }
}
