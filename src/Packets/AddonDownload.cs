/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

using System;
using Carbon.Client.Assets;
using ProtoBuf;

namespace Carbon.Client.Packets;

[ProtoContract]
public class AddonDownload : BasePacket
{
	[ProtoMember(1)]
	public byte[] BufferChunk { get; set; }

	[ProtoMember(2)]
	public Formats Format { get; set; }

	[ProtoMember(3)]
	public bool UninstallAll { get; set; }

	public enum Formats
	{
		First,
		Content,
		Last,
		Whole
	}

	public override void Dispose()
	{
		if (BufferChunk != null)
		{
			Array.Clear(BufferChunk, 0, BufferChunk.Length);
			BufferChunk = null;
		}

		base.Dispose();
	}
}
