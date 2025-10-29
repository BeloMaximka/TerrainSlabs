using ProtoBuf;

namespace TerrainSlabs.Source.Network;

[ProtoContract]
public class UpdateBlocklistMessage
{
    [ProtoMember(1)]
    public bool AddMode { get; set; }

    [ProtoMember(2)]
    public required string Wildcard { get; set; }
}
