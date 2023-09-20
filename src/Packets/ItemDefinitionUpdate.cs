/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

using ProtoBuf;

namespace Carbon.Client.Packets;

[ProtoContract]
public class ItemDefinitionUpdate : BasePacket
{
	[ProtoMember(1)]
	public string Shortname { get; set; }

	[ProtoMember(2)]
	public string DisplayName { get; set; }

	[ProtoMember(3)]
	public string DisplayDescription { get; set; }

	public override void Dispose()
	{
		Shortname = null;
		DisplayName = null;
		DisplayDescription = null;
	}
}
