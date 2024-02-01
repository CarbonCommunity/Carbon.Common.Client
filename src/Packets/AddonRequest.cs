/*
 *
 * Copyright (c) 2022-2024 Carbon Community 
 * All rights reserved.
 *
 */

using System;
using System.Collections.Generic;
using Carbon.Client.Assets;
using ProtoBuf;

namespace Carbon.Client.Packets;

[ProtoContract]
public class AddonRequest : BasePacket
{
	[ProtoMember(1)]
	public Addon.Manifest[] Manifests { get; set; }

	[ProtoMember(2)] public bool Asynchronous { get; set; }

	[ProtoMember(3)] public bool UninstallAll { get; set; }

	public override void Dispose()
	{
		if (Manifests != null)
		{
			Array.Clear(Manifests, 0, Manifests.Length);
			Manifests = null;
		}

		base.Dispose();
	}
}
