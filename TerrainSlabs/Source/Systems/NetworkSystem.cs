using TerrainSlabs.Source.Network;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace TerrainSlabs.Source.Systems;

public class NetworkSystem : ModSystem
{
    ICoreClientAPI? capi;

    public override void StartClientSide(ICoreClientAPI api)
    {
        capi = api;
        api.Network.RegisterChannel(TerrainSlabsGlobalValues.OffsetBlackListNetworkChannel)
            .RegisterMessageType(typeof(UpdateBlocklistMessage))
            .SetMessageHandler<UpdateBlocklistMessage>(OnUpdateBlacklistRequest);
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        api.Network.RegisterChannel(TerrainSlabsGlobalValues.OffsetBlackListNetworkChannel)
            .RegisterMessageType(typeof(UpdateBlocklistMessage));
    }

    private void OnUpdateBlacklistRequest(UpdateBlocklistMessage networkMessage)
    {
        if (capi is null)
            return;

        foreach (Block block in capi.World.SearchBlocks(networkMessage.Wildcard))
        {
            if (networkMessage.AddMode)
            {
                SlabHelper.AddToOffsetBlacklist(block.Id);
            }
            else
            {
                SlabHelper.RemoveFromOffsetBlacklist(capi, block);
            }
        }
    }
}
