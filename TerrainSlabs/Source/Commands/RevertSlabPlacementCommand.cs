using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace TerrainSlabs.Source.Commands;

public static class RevertSlabPlacementCommand
{
    public static void Register(ICoreServerAPI api)
    {
        api.ChatCommands.GetOrCreate("tslab")
            .WithAlias("ts")
            .RequiresPrivilege(Privilege.buildblockseverywhere)
            .BeginSubCommand("unsmooth")
            .WithAlias("us")
            .BeginSubCommand("surface")
            .WithAlias("s")
            .RequiresPlayer()
            .WithArgs(api.ChatCommands.Parsers.Int("range"))
            .HandleWith(OnHandle);
    }

    private static TextCommandResult OnHandle(TextCommandCallingArgs args)
    {
        ICoreAPI api = args.Caller.Entity.Api;

        Dictionary<int, int> terrainReplacementMap = ObjectCacheUtil.GetOrCreate(
            api,
            "TSblocksToRevert",
            () =>
            {
                string wildcard = $"terrainslabs:*";
                Dictionary<int, int> result = [];
                foreach (var resultBlock in api.World.SearchBlocks(wildcard))
                {
                    AssetLocation originalCode = new("game", resultBlock.Code.Path);
                    Block? originalBlock = api.World.GetBlock(originalCode);
                    if (originalBlock is null)
                    {
                        api.Logger.Warning("Unable to find slab block alternative with code {0}", originalCode);
                        continue;
                    }
                    result.Add(resultBlock.Id, originalBlock.Id);
                }
                return result;
            }
        );

        int range = 1 + (int)args.Parsers[0].GetValue() * 2;
        var bulkAccessor = args.Caller.Entity.Api.World.GetBlockAccessorBulkMinimalUpdate(true);
        var position = args.Caller.Entity.Pos.AsBlockPos.Copy();

        position.Z -= range / 2;
        position.X -= range / 2;
        int replacedCount = 0;
        for (int x = 0; x < range; x++)
        {
            for (int z = 0; z < range; z++)
            {
                position.Y = bulkAccessor.GetTerrainMapheightAt(position);

                if (terrainReplacementMap.TryGetValue(bulkAccessor.GetBlock(position).Id, out int originalBlockId))
                {
                    replacedCount++;
                    bulkAccessor.SetBlock(originalBlockId, position);
                }

                position.Z++;
            }
            position.Z -= range;
            position.X++;
        }

        bulkAccessor.Commit();

        return TextCommandResult.Success($"Replaced {replacedCount} blocks");
    }
}
