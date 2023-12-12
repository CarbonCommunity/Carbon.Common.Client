/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using ProtoBuf;

namespace Carbon.Client.Packets;

[ProtoContract]
public class LoadingScreenCUI : BasePacket
{
	[ProtoMember(1)]
	public string Content { get; set; }

	public override void Dispose()
	{
		Content = null;

		base.Dispose();
	}
}
