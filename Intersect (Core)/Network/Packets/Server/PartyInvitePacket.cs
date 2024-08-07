﻿using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public partial class PartyInvitePacket : IntersectPacket
{
    //Parameterless Constructor for MessagePack
    public PartyInvitePacket()
    {
    }

    public PartyInvitePacket(string leaderName, Guid leaderId)
    {
        LeaderName = leaderName;
        LeaderId = leaderId;
    }

    [Key(0)]
    public string LeaderName { get; set; }

    [Key(1)]
    public Guid LeaderId { get; set; }

}
