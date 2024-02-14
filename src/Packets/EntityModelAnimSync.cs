/*
 *
 * Copyright (c) 2022-2024 Carbon Community 
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

	[ProtoMember(5)]
	public bool Replay { get; set; }

	public override void Dispose()
	{
	}
}
