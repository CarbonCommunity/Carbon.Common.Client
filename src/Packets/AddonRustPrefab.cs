/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

using ProtoBuf;

namespace Carbon.Client.Packets;

[ProtoContract]
public class AddonRustPrefab : BasePacket
{
	[ProtoMember(1)]
	public string Addon { get; set; }

	[ProtoMember(2)]
	public string Asset { get; set; }

	[ProtoMember(3)]
	public bool Asynchronous { get; set; }

	public override void Dispose()
	{
		Addon = null;
		Asset = null;
		Asynchronous = default;

		base.Dispose();
	}
}
