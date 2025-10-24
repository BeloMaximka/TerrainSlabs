using HarmonyLib;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace TerrainSlabs.Source;

public class TerrainSlabsModSystem : ModSystem
{
    private Harmony harmonyInstance = null!;

    public override double ExecuteOrder() => 0.2;

    public override void StartPre(ICoreAPI api)
    {
        harmonyInstance = new(Mod.Info.ModID);
        if (api.Side == EnumAppSide.Client && !harmonyInstance.GetPatchedMethods().Any())
        {
            harmonyInstance.PatchAll();
        }
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        if (!harmonyInstance.GetPatchedMethods().Any())
        {
            harmonyInstance.PatchAll();
        }
    }

    public override void AssetsLoaded(ICoreAPI api)
    {
        if (api is ICoreServerAPI sapi)
        {
            string[] slabCodes = ["soil-*", "sand-*", "gravel-*", "forestfloor-*"]; // TODO: Move to config and filter gravel so we don't create 1-8 variant
            bool idsNonSequential = false;
            int prevBlockId = 0;
            int slabIdStart = 0;
            int slabIdEnd = 0;
            AssetLocation slabShapeCode = new("block/basic/slab/slab-down");
            foreach (var wildcard in slabCodes)
            {
                Block[] blocks = sapi.World.SearchBlocks(wildcard);
                foreach (var block in blocks)
                {
                    Block slabBlock = block.ProperClone();
                    slabBlock.Code.Domain = "terrainslabs";
                    slabBlock.SideOpaque.Fill(false);
                    slabBlock.SideOpaque[BlockFacing.indexDOWN] = true;
                    slabBlock.EmitSideAo = 0;
                    foreach (var box in slabBlock.CollisionBoxes)
                    {
                        box.Y2 -= 0.5f;
                    }
                    foreach (var box in slabBlock.SelectionBoxes)
                    {
                        box.Y2 -= 0.5f;
                    }

                    slabBlock.Shape.Base = slabShapeCode;
                    if (slabBlock.Shape.Alternates is not null)
                    {
                        foreach (var shape in slabBlock.Shape.Alternates)
                        {
                            shape.Base = slabShapeCode;
                        }
                    }

                    sapi.RegisterBlock(slabBlock);
                    if (prevBlockId != 0 && slabBlock.BlockId - 1 != prevBlockId)
                    {
                        idsNonSequential = true;
                    }
                    if (slabIdStart == 0)
                    {
                        slabIdStart = slabBlock.BlockId;
                    }
                    slabIdEnd = slabBlock.BlockId;
                }
            }
            if (idsNonSequential)
            {
                sapi.Logger.Notification("Slab ids are not sequential, remapper will be used to group them in one place.");
                SlabGroupHelper.RemapSlabIdIntoGroup(sapi);
            }
            else
            {
                SlabGroupHelper.UpdateIdRange(slabIdStart, slabIdEnd);
            }
        }
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
