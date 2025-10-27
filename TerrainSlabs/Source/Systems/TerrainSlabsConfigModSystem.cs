using ConfigLib;
using System;
using TerrainSlabs.Source.Commands;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace TerrainSlabs.Source.Systems;

public enum TerrainSmoothMode
{
    None,
    Surface,
    Column,
}

public class ServerSettings
{
    private TerrainSmoothMode smoothMode = TerrainSmoothMode.Column;
    public event Action<TerrainSmoothMode>? SmoothModeChanged;

    public TerrainSmoothMode SmoothMode
    {
        get => smoothMode;
        set => SetField(ref smoothMode, value, SmoothModeChanged);
    }

    private static void SetField<T>(ref T field, T value, Action<T>? onChanged)
    {
        if (Equals(field, value)) return;
        field = value;
        onChanged?.Invoke(value);
    }
}

internal class TerrainSlabsConfigModSystem : ModSystem
{
    private const string fileName = "terrainslabs_server.json";

    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Server;

    public ServerSettings ServerSettings { get; private set; } = new();

    public override void StartServerSide(ICoreServerAPI api)
    {
        LoadConfig(api);
        ChangeGenerationModeCommand.Register(api);

        if (api.ModLoader.IsModEnabled("configlib"))
        {
            SubscribeToConfigChange(api);
        }
    }

    public void SaveConfig(ICoreServerAPI api)
    {
        api.StoreModConfig<ServerSettings>(ServerSettings, fileName);
    }

    private void LoadConfig(ICoreServerAPI api)
    {
        try
        {
            ServerSettings settings = api.LoadModConfig<ServerSettings>(fileName);
            if (settings is not null)
            {
                ServerSettings = settings;
            }
            else
            {
                api.StoreModConfig<ServerSettings>(ServerSettings, fileName);
            }
        }
        catch (Exception e)
        {
            Mod.Logger.Warning("Could not load config from {0}, loading default settings instead.", fileName);
            Mod.Logger.Warning(e);
        }
    }

    private void SubscribeToConfigChange(ICoreServerAPI sapi)
    {
        ConfigLibModSystem system = sapi.ModLoader.GetModSystem<ConfigLibModSystem>();

        var config = system.GetConfig(Mod.Info.ModID);
        config?.AssignSettingsValues(ServerSettings);

        system.SettingChanged += (domain, config, setting) =>
        {
            if (domain != Mod.Info.ModID)
                return;
            setting.AssignSettingValue(ServerSettings);
            SaveConfig(sapi);
        };
    }
}
