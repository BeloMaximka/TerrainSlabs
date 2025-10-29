using HarmonyLib;
using System.Linq;
using TerrainSlabs.Source.BlockBehaviors;
using TerrainSlabs.Source.Blocks;
using TerrainSlabs.Source.Commands;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace TerrainSlabs.Source.Systems;

public class TerrainSlabsModSystem : ModSystem
{
    private Harmony harmonyInstance = null!;

    public override double ExecuteOrder() => 0.2;

    public override void StartPre(ICoreAPI api)
    {
        harmonyInstance = new(Mod.Info.ModID);
        if (!harmonyInstance.GetPatchedMethods().Any())
        {
            harmonyInstance.PatchAll();
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

        api.RegisterBlockBehaviorClass("RestrictTopAttachment", typeof(BlockBehaviorRestrictTopAttachment));
        api.RegisterBlockBehaviorClass("UnstableFallingSlab", typeof(BlockBehaviorUnstableFallingSlab));

        RecalculateSlabFlagsCommand.Register(api);
    }

    public override void AssetsFinalize(ICoreAPI api)
    {
        SlabGroupHelper.InitFlags(api);
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
