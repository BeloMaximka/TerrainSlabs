using System.Collections.Generic;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace TerrainSlabs.Source.Commands;

public static class ReplaceWithTerrainSlabsCommand
{
    public static void Register(ICoreServerAPI api)
    {
        api.ChatCommands.GetOrCreate("tslab")
            .WithAlias("ts")
            .RequiresPrivilege(Privilege.buildblockseverywhere)
            .BeginSubCommand("smooth")
            .WithAlias("s")
            .BeginSubCommand("surface")
            .WithAlias("s")
            .RequiresPlayer()
            .WithArgs(api.ChatCommands.Parsers.Int("range"), api.ChatCommands.Parsers.OptionalBool("highlightBlocks", "true"))
            .HandleWith(OnHandle);
    }

    private static TextCommandResult OnHandle(TextCommandCallingArgs args)
    {
        int range = (int)args.Parsers[0].GetValue();
        bool highlightBlocks = (bool)args.Parsers[1].GetValue();
        range = 1 + range * 2;
        var bulkAccessor = args.Caller.Entity.Api.World.GetBlockAccessorBulkMinimalUpdate(true);
        var position = args.Caller.Entity.Pos.AsBlockPos.Copy();

        TerrainSlabReplacer replacer = new(args.Caller.Entity.Api, bulkAccessor);
        position.Z -= range / 2;
        position.X -= range / 2;

        List<BlockPos> changedBlockPos = new(highlightBlocks ? range * range : 0);
        int replacedCount = 0;
        for (int x = 0; x < range; x++)
        {
            for (int z = 0; z < range; z++)
            {
                position.Y = bulkAccessor.GetTerrainMapheightAt(position);

                if (bulkAccessor.IsNotTraversable(position))
                {
                    continue;
                }
                if (replacer.TryReplaceWithSlab(position))
                {
                    replacedCount++;
                    if (highlightBlocks)
                    {
                        changedBlockPos.Add(position.Copy());

                    }
                }
                position.Z++;
            }
            position.Z -= range;
            position.X++;
        }

        bulkAccessor.Commit();

        if (highlightBlocks)
        {
            args.Caller.Entity.Api.World.HighlightBlocks(args.Caller.Player, 1, changedBlockPos);
        }

        return TextCommandResult.Success($"Replaced {replacedCount} blocks");
    }
}
