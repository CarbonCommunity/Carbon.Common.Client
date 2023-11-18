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
	public string Clip { get; set; }

	[ProtoMember(3)]
	public float Time { get; set; }

	[ProtoMember(4)]
	public float Speed { get; set; }

	public override void Dispose()
	{
	}
}
