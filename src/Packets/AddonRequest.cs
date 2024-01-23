﻿/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using System.Collections.Generic;
using Carbon.Client.Assets;
using ProtoBuf;

namespace Carbon.Client.Packets;

[ProtoContract]
public class AddonRequest : BasePacket
{
	[ProtoMember(1)]
	public List<Addon.Manifest> Manifests { get; set; }
}
