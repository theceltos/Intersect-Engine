﻿namespace Intersect.Network;

public sealed partial class PacketLifecycle
{
    public Type PacketType { get; }

    public IReadOnlyList<IPacketHandler> PreHandlers { get; }

    public IReadOnlyList<IPacketHandler> PostHandlers { get; }

    public IPacketHandler Preprocessor { get; }

    public IPacketHandler Handler { get; }


}
