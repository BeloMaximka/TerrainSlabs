using HarmonyLib;
using System.Linq;
using TerrainSlabs.Source.BlockBehaviors;
using TerrainSlabs.Source.Commands;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace TerrainSlabs.Source;

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
        api.RegisterBlockBehaviorClass(nameof(RestrictTopAttachmentBlockBehavior), typeof(RestrictTopAttachmentBlockBehavior));
        RecalculateSlabFlagsCommand.Register(api);
    }

    public override void AssetsFinalize(ICoreAPI api)
    {
        SlabGroupHelper.InitFlags(api);
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        WorldGenUtils.RegisterSlabReplacementWorldGenEvent(api);
        ReplaceWithTerrainSlabsCommand.Register(api);
        ReplaceBlockWithTerrainSlabCommand.Register(api);
    }

    public override void Dispose()
    {
        harmonyInstance.UnpatchAll(harmonyInstance.Id);
    }
}
