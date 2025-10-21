using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace TerrainSlabs.Source;

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
            .WithArgs(api.ChatCommands.Parsers.Int("range"))
            .HandleWith(OnHandle);
    }

    private static TextCommandResult OnHandle(TextCommandCallingArgs args)
    {
        int radus = (int)args.Parsers[0].GetValue();
        var bulkAccessor = args.Caller.Entity.Api.World.GetBlockAccessorBulkMinimalUpdate(true);
        var position = args.Caller.Entity.Pos.AsBlockPos.Copy();

        TerrainSlabReplacer replacer = new(args.Caller.Entity.Api, bulkAccessor);
        position.Z -= radus / 2;
        position.X -= radus / 2;
        for (int x = 0; x < radus; x++)
        {
            for (int z = 0; z < radus; z++)
            {
                replacer.TryReplaceWithSlab(position);
                position.Z++;
            }
            position.Z -= radus;
            position.X++;
        }

        bulkAccessor.Commit();

        return TextCommandResult.Success("Done.");
    }
}
