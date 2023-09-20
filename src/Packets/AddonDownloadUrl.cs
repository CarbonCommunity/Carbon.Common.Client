/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

using ProtoBuf;

namespace Carbon.Client.Packets;

[ProtoContract]
public class AddonDownloadUrl : BasePacket
{
	[ProtoMember(1)]
	public string[] Urls { get; set; }

	[ProtoMember(2)]
	public bool UninstallAll { get; set; }
}
