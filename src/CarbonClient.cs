using System;
using System.Diagnostics;
using Carbon.Client.Contracts;
using Carbon.Client.Packets;
using Carbon.Client.SDK;
using Network;
using ProtoBuf;
using UnityEngine;

/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Client;

public class CarbonClient : ICarbonClient
{
	public BasePlayer Player { get; set; }
	public Connection Connection { get; set; }

	public bool IsConnected => Connection != null && Connection.active;
	public bool HasCarbonClient { get; set; }

	public bool IsDownloadingAddons { get; set; }

	public int ScreenWidth { get; set; }
	public int ScreenHeight { get; set; }

	#region Methods

	public bool Send(RPC rpc, IPacket packet = default, bool checks = true)
	{
		if (!Community.Runtime.ClientConfig.Enabled)
		{
			return false;
		}

		if (checks && !IsValid()) return false;

		try
		{
			var info = new SendInfo(Connection);

			if (packet == null)
			{
				NetworkSend(rpc).Send(info);
			}
			else
			{
				var write = NetworkSend(rpc);
				var bytes = packet.Serialize();
				write.WriteObject(bytes.Length);
				write.WriteObject(bytes);
				write.Send(info);
			}
		}
		catch (Exception ex)
		{
			ex = ex.Demystify();
			UnityEngine.Debug.LogError($"Failed sending Carbon client RPC {rpc.Name}[{rpc.Id}] to {Connection.username}[{Connection.userid}] ({ex.Message})\n{ex.StackTrace}");
			return false;
		}

		return true;
	}
	public bool Send(string rpc, IPacket packet = default, bool checks = true)
	{
		return Send(RPC.Get(rpc), packet, checks);
	}

	public NetWrite NetworkSend(RPC rpc)
	{
		var write = Net.sv.StartWrite(CarbonClientManager.PACKET_ID);
		write.UInt32(rpc.Id);
		return write;
	}

	void ICarbonClient.Send(string rpc, IPacket packet, bool checks)
	{
		Send(rpc, packet, checks);
	}

	public T Receive<T>(Message message)
	{
		if (!message.read.TemporaryBytesWithSize(out var buffer, out var length))
		{
			return default;
		}

		return Serializer.Deserialize<T>(new ReadOnlySpan<byte>(buffer, 0, length));
	}

	#endregion

	#region CUI

	public void CreateLoadingCUI(string content)
	{
		using var cui = new LoadingScreenCUI()
		{
			Content = content
		};

		Send("loading_createcui", cui);
	}
	public void DestroyLoadingCUI(string name)
	{
		using var cui = new LoadingScreenCUI()
		{
			Content = name
		};

		Send("loading_destroycui", cui);
	}

	#endregion

	#region Addons

	public void SpawnPrefab(string path, Vector3 position, Vector3 rotation, Vector3 scale, bool asynchronous = true)
	{
		SpawnPrefab(path, position, Quaternion.Euler(rotation), scale, asynchronous);
	}
	public void SpawnPrefab(string path, Vector3 vector, Quaternion quaternion, Vector3 scale, bool asynchronous = true)
	{
		using var packet = new AddonPrefab
		{
			Path = path,
			Position = BaseVector.ToProtoVector(vector),
			Rotation = BaseVector.ToProtoVector(quaternion),
			Scale = BaseVector.ToProtoVector(scale),
			Asynchronous = asynchronous
		};
		Send("addon_spawn", packet);
	}
	public void DestroyPrefab(string path)
	{
		using var packet = new AddonPrefab
		{
			Path = path,
		};
		Send("addon_destroy", packet);
	}
	public void DestroyAllPrefabs ()
	{
		Send("addon_destroyall");
	}
	public void Uninstall(string addon)
	{
		using var packet = new AddonPrefab
		{
			Path = addon,
		};
		Send("addon_uninstall", packet);
	}
	public void UninstallAllAddons()
	{
		Send("addon_uninstallall");
	}

	#endregion

	public bool IsValid()
	{
		return IsConnected && HasCarbonClient;
	}

	public void OnConnected()
	{
		// OnCarbonClientJoined
		HookCaller.CallStaticHook(2630056331, this);
	}
	public void OnDisconnect()
	{
		IsDownloadingAddons = false;

		// OnCarbonClientLeft
		HookCaller.CallStaticHook(978897282, this);
	}
	public void Dispose()
	{
		IsDownloadingAddons = false;
		Player = null;
		Connection = null;
	}
}
