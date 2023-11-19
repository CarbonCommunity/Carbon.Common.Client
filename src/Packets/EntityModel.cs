/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using ProtoBuf;

namespace Carbon.Client.Packets;

[ProtoContract]
public class EntityModel : BasePacket
{
	[ProtoMember(1)]
	public string PrefabName { get; set; }

	[ProtoMember(2)]
	public ulong EntityId { get; set; }

	[ProtoMember(3)]
	public bool OriginalCollision { get; set; }

	public override void Dispose()
	{
	}
}
