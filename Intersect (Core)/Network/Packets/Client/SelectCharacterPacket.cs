﻿using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject]
public partial class SelectCharacterPacket : IntersectPacket
{
    //Parameterless Constructor for MessagePack
    public SelectCharacterPacket()
    {
    }

    public SelectCharacterPacket(Guid charId)
    {
        CharacterId = charId;
    }

    [Key(0)]
    public Guid CharacterId { get; set; }

}
