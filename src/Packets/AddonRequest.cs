/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using ProtoBuf;

namespace Carbon.Client.Packets;

[ProtoContract]
public class AddonRequest : BasePacket
{
	[ProtoMember(1)]
	public int AddonCount { get; set; }

	[ProtoMember(2)]
	public int BufferSize { get; set; }

	[ProtoMember(3)]
	public bool LoadingScreen { get; set; }
}
