/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using ProtoBuf;

namespace Carbon.Client.Packets;

[ProtoContract]
public class EntityModelAnimSync : BasePacket
{
	[ProtoMember(1)]
	public ulong EntityId { get; set; }

	[ProtoMember(2)]
	public float Time { get; set; }

	public override void Dispose()
	{
	}
}
