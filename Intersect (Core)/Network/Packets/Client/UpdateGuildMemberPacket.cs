﻿using Intersect.Enums;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject]
public partial class UpdateGuildMemberPacket : IntersectPacket
{
    /// <summary>
    /// Parameterless Constructor for MessagePack
    /// </summary>
    public UpdateGuildMemberPacket()
    {

    }

    public UpdateGuildMemberPacket(Guid id, string name, GuildMemberUpdateAction action, int rank = -1)
    {
        Id = id;
        Name = name;
        Action = action;
        Rank = rank;
    }

    [Key(0)]
    public Guid Id { get; set; }

    [Key(1)]
    public string Name { get; set; }

    [Key(2)]
    public GuildMemberUpdateAction Action { get; set; }

    [Key(3)]
    public int Rank { get; set; }
}
