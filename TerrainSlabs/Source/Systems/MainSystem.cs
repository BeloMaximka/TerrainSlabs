using HarmonyLib;
using System.Linq;
using TerrainSlabs.Source.BlockBehaviors;
using TerrainSlabs.Source.Blocks;
using TerrainSlabs.Source.Commands;
using TerrainSlabs.Source.HarmonyPatches;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
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
        SlabHelper.InitFlags(api);

        if (api is ICoreClientAPI capi)
        {
            string[] blackList = capi.World.Config.GetString(TerrainSlabsGlobals.WorldConfigName, string.Empty).Split('|');
            SlabHelper.InitBlacklist(capi, blackList);
        }
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        WorldGenUtils.RegisterSlabReplacementWorldGenEvent(api);

        AlterTerrainCommand.Register(api);
        SmoothBlockCommand.Register(api);
        OffsetBlacklistCommand.Register(api);
    }

    public override void Dispose()
    {
        harmonyInstance.UnpatchAll(harmonyInstance.Id);
    }
}
