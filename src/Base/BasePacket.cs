/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

using System;
using System.IO;
using Carbon.Client.Contracts;
using Carbon.Client.Packets;
using Network;
using ProtoBuf;

namespace Carbon.Client;

[ProtoContract]
[ProtoInclude(10, typeof(RPCList))]
[ProtoInclude(11, typeof(ClientInfo))]
[ProtoInclude(12, typeof(ItemDefinitionUpdate))]
[ProtoInclude(13, typeof(ClientModifications))]
[ProtoInclude(14, typeof(HookCall))]
[ProtoInclude(15, typeof(AddonRequest))]
[ProtoInclude(16, typeof(AddonDownload))]
[ProtoInclude(17, typeof(AddonDownloadUrl))]
[ProtoInclude(18, typeof(AddonPrefab))]
[ProtoInclude(19, typeof(AddonRustPrefab))]
public class BasePacket : IPacket, IDisposable
{
	public static T Deserialize<T>(NetRead reader)
	{
		reader.TemporaryBytesWithSize(out byte[] buf, out int count);
		return Serializer.Deserialize<T>(new ReadOnlySpan<byte>(buf, 0, count));
	}

	public byte[] Serialize()
	{
		using var stream = new MemoryStream();
		Serializer.Serialize(stream, this);
		return stream.ToArray();
	}

	public virtual void Dispose()
	{
	}
}
