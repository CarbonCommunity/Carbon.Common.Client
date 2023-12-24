/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using System;
using System.Linq;
using Carbon.Core;
using ProtoBuf;

namespace Carbon.Client.Packets;

[ProtoContract]
public class RPCList : BasePacket
{
	[ProtoMember(1)]
	public string[] RpcNames { get; set; }

	[ProtoMember(2)]
	public uint[] RpcIds { get; set; }

	[ProtoMember(3)]
	public bool NoMap { get; set; }

	[ProtoMember(4)]
	public bool OldRecoil { get; set; }

	public static RPCList SERVER_Get()
	{
		return new RPCList()
		{
			RpcIds = RPC.rpcList.Select(x => x.Id).ToArray(),
			RpcNames = RPC.rpcList.Select(x => x.Name).ToArray(),
			NoMap = Client.NomapEnabled,
			OldRecoil = Client.OldRecoil
		};
	}
	public static RPCList CLIENT_Get()
	{
		return new RPCList()
		{
			RpcIds = RPC.rpcList.Select(x => x.Id).ToArray(),
			RpcNames = RPC.rpcList.Select(x => x.Name).ToArray(),
		};
	}

	public void Sync()
	{
		foreach (var item in RpcNames)
		{
			RPC.Get(item);
		}
		foreach (var item in RpcIds)
		{
			RPC.Get(item);
		}

		foreach(var item in RPC.rpcList)
		{
			Console.WriteLine($"Registered RPC: {item.Name}[{item.Id}]");
		}
	}

	public override void Dispose()
	{
		Array.Clear(RpcNames, 0, RpcNames.Length);
		Array.Clear(RpcIds, 0, RpcIds.Length);

		RpcNames = null;
		RpcIds = null;
		NoMap = default;
	}
}
