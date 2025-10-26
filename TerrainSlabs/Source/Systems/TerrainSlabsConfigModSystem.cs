using ConfigLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace TerrainSlabs.Source.Systems;

public class ServerSettings
{
    public bool EnableWorldGen { get; set; } = true;
}

internal class TerrainSlabsConfigModSystem : ModSystem
{
    public ServerSettings ServerSettings { get; private set; } = new();

    public override void StartServerSide(ICoreServerAPI api)
    {
        if (api.ModLoader.IsModEnabled("configlib"))
        {
            SubscribeToConfigChange(api);
        }
    }

    private void SubscribeToConfigChange(ICoreAPI api)
    {
        ConfigLibModSystem system = api.ModLoader.GetModSystem<ConfigLibModSystem>();

        var config = system.GetConfig(Mod.Info.ModID);
        config?.AssignSettingsValues(ServerSettings);

        system.SettingChanged += (domain, config, setting) =>
        {
            if (domain != Mod.Info.ModID) return;
            setting.AssignSettingValue(ServerSettings);
        };
    }
}
