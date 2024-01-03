/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using ProtoBuf;

namespace Carbon.Client.Packets;

[ProtoContract]
public class ClientOptions : BasePacket
{
	[ProtoMember(1)]
	public bool UseOldRecoil = false;

	[ProtoMember(2)]
	public float ClientGravity = -1f;

	[ProtoMember(3)]
	public float PlayerGravity = -1f;
}
