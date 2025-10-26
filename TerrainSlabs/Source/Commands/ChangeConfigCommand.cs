using TerrainSlabs.Source.Systems;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace TerrainSlabs.Source.Commands;

public static class ChangeConfigCommand
{
    public static void Register(ICoreServerAPI sapi)
    {
        sapi.ChatCommands.GetOrCreate("tslab")
            .WithAlias("ts")
            .RequiresPrivilege(Privilege.buildblockseverywhere)
            .BeginSubCommand("config")
            .WithAlias("c")
            .BeginSubCommand("set")
            .WithAlias("s")
            .WithArgs(sapi.ChatCommands.Parsers.Word("name"), sapi.ChatCommands.Parsers.Word("value"))
            .HandleWith(OnHandle);
    }

    private static TextCommandResult OnHandle(TextCommandCallingArgs args)
    {
        if (args.Caller.Entity.Api is not ICoreServerAPI sapi)
        {
            return TextCommandResult.Error($"Must be called on server");
        }

        TerrainSlabsConfigModSystem configSystem = sapi.ModLoader.GetModSystem<TerrainSlabsConfigModSystem>();
        string settingName = (string)args.Parsers[0].GetValue();

        if (settingName != nameof(configSystem.ServerSettings.EnableWorldGen))
        {
            return TextCommandResult.Error($"Incorrect setting name. ");
        }

        if (!bool.TryParse((string)args.Parsers[1].GetValue(), out bool value))
        {
            return TextCommandResult.Error($"Incorrect setting value.");
        }

        configSystem.ServerSettings.EnableWorldGen = value;
        configSystem.SaveConfig(sapi);
        return TextCommandResult.Success($"Updated {nameof(configSystem.ServerSettings.EnableWorldGen)} to {value}.");
    }
}
