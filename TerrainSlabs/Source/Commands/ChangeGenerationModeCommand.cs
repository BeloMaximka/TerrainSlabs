using TerrainSlabs.Source.Systems;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace TerrainSlabs.Source.Commands;

public static class ChangeGenerationModeCommand
{
    public static void Register(ICoreServerAPI sapi)
    {
        sapi.ChatCommands.GetOrCreate("tslab")
            .WithAlias("ts")
            .RequiresPrivilege(Privilege.buildblockseverywhere)
            .BeginSubCommand("genmode")
            .WithAlias("g")
            .BeginSubCommand("set")
            .WithAlias("s")
            .WithArgs(sapi.ChatCommands.Parsers.Word("value"))
            .HandleWith(OnHandle);
    }

    private static TextCommandResult OnHandle(TextCommandCallingArgs args)
    {
        if (args.Caller.Entity.Api is not ICoreServerAPI sapi)
        {
            return TextCommandResult.Error($"Must be called on server");
        }
        ConfigSystem configSystem = sapi.ModLoader.GetModSystem<ConfigSystem>();

        if (!TerrainSmoothMode.TryParse((string)args.Parsers[0].GetValue(), out TerrainSmoothMode value))
        {
            return TextCommandResult.Error(
                $"Incorrect setting value. Supported values are: {string.Join(", ", System.Enum.GetNames(typeof(TerrainSmoothMode)))}"
            );
        }

        configSystem.ServerSettings.SmoothMode = value;
        configSystem.SaveConfig(sapi);
        return TextCommandResult.Success($"Set {nameof(configSystem.ServerSettings.SmoothMode)} to {value}.");
    }
}
