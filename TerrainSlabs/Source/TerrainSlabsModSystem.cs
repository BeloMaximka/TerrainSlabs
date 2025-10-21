using HarmonyLib;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace TerrainSlabs.Source;

public class TerrainSlabsModSystem : ModSystem
{
    private Harmony harmonyInstance;

    public override void StartPre(ICoreAPI api)
    {
        harmonyInstance = new(Mod.Info.ModID);
        if (api.Side == EnumAppSide.Client && !harmonyInstance.GetPatchedMethods().Any())
        {
            harmonyInstance.PatchAll();
        }
    }

    public override void Start(ICoreAPI api)
    {
        api.RegisterBlockClass(nameof(BlockSoilSlab), typeof(BlockSoilSlab));
        api.RegisterBlockClass(nameof(BlockTallGrassOffset), typeof(BlockTallGrassOffset));
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        if (!harmonyInstance.GetPatchedMethods().Any())
        {
            harmonyInstance.PatchAll();
        }
    }

    public override void Dispose()
    {
        harmonyInstance.UnpatchAll(harmonyInstance.Id);
    }
}
