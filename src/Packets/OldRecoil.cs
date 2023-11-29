/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using ProtoBuf;

namespace Carbon.Client.Packets;

[ProtoContract]
public class OldRecoil : BasePacket
{
	[ProtoMember(1)]
	public bool Enable { get; set; }
}
