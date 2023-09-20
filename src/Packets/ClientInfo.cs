/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

using ProtoBuf;

namespace Carbon.Client.Packets;

[ProtoContract]
public class ClientInfo : BasePacket
{
	[ProtoMember(1)]
	public int ScreenWidth { get; set; }

	[ProtoMember(2)]
	public int ScreenHeight { get; set; }

	public override void Dispose()
	{

	}
}
