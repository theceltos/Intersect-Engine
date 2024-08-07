﻿using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public partial class SpellCastPacket : IntersectPacket
{
    //Parameterless Constructor for MessagePack
    public SpellCastPacket()
    {
    }

    public SpellCastPacket(Guid entityId, Guid spellId)
    {
        EntityId = entityId;
        SpellId = spellId;
    }

    [Key(0)]
    public Guid EntityId { get; set; }

    [Key(1)]
    public Guid SpellId { get; set; }

}
