using TerrainSlabs.Source.Systems;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace TerrainSlabs.Source.Commands;

public static class OffsetBlacklistCommand
{
    public static void Register(ICoreServerAPI sapi)
    {
        sapi.ChatCommands.GetOrCreate("tslab")
            .WithAlias("ts")
            .RequiresPrivilege(Privilege.controlserver)
            .BeginSubCommand("blacklist")
            .WithAlias("b")
            .BeginSubCommand("add")
            .WithAlias("a")
            .RequiresPlayer()
            .WithArgs(sapi.ChatCommands.Parsers.Word("wildcard"))
            .HandleWith(AddToBlacklist);

        sapi.ChatCommands.GetOrCreate("tslab")
            .WithAlias("ts")
            .RequiresPrivilege(Privilege.controlserver)
            .BeginSubCommand("blacklist")
            .WithAlias("b")
            .BeginSubCommand("remove")
            .WithAlias("r")
            .RequiresPlayer()
            .WithArgs(sapi.ChatCommands.Parsers.Word("wildcard"))
            .HandleWith(RemoveFromBlacklist);
    }

    private static TextCommandResult AddToBlacklist(TextCommandCallingArgs args) => UpdateBlacklist(args, true);

    private static TextCommandResult RemoveFromBlacklist(TextCommandCallingArgs args) => UpdateBlacklist(args, false);

    private static TextCommandResult UpdateBlacklist(TextCommandCallingArgs args, bool addMode)
    {
        if (args.Caller.Entity.Api is not ICoreServerAPI sapi)
        {
            return TextCommandResult.Error($"Should be called on server");
        }
        string wildcard = (string)args.Parsers[0].GetValue();
        var configSystem = sapi.ModLoader.GetModSystem<ConfigSystem>();

        int count = configSystem.UpdateBlacklist(sapi, wildcard, addMode);
        return TextCommandResult.Success($"Updated {count} blocks.");
    }
}
