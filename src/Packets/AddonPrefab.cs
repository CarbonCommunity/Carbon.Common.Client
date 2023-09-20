/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

using ProtoBuf;

namespace Carbon.Client.Packets;

[ProtoContract]
public class AddonPrefab : BasePacket
{
	[ProtoMember(1)]
	public string Path { get; set; }

	[ProtoMember(2)]
	public BaseVector Position { get; set; }

	[ProtoMember(3)]
	public BaseVector Rotation { get; set; }

	[ProtoMember(4)]
	public BaseVector Scale { get; set; }

	[ProtoMember(5)]
	public bool Asynchronous { get; set; }

	public override void Dispose()
	{
		Path = null;
		Position = null;
		Rotation = null;
		Scale = null;
		Asynchronous = false;

		base.Dispose();
	}
}
