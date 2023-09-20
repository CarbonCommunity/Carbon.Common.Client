/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

using ProtoBuf;

namespace Carbon.Client.Packets;

[ProtoContract]
public class HookCall : BasePacket
{
	[ProtoMember(1)]
	public string Hook { get; set; }

	public override void Dispose()
	{
		Hook = null;
	}
}
