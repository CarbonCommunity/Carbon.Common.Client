using System;
using System.Collections.Generic;
using Carbon.Client.Packets;
using Carbon.Client.SDK;
using HarmonyLib;
using Network;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

namespace Carbon.Client;

public class CarbonClientManager : ICarbonClientManager
{
	public Dictionary<Connection, ICarbonClient> Clients { get; } = new();

	internal const string _PATCH_NAME = "com.carbon.clientpatch";
	internal Harmony _PATCH;

	public void ApplyPatch()
	{
		_PATCH?.UnpatchAll(_PATCH_NAME);
		_PATCH = new Harmony(_PATCH_NAME);

		try
		{
			_PATCH.PatchAll(typeof(CarbonClientManager).Assembly);
		}
		catch (Exception ex)
		{
			Logger.Error($"Failed patching Client Manager", ex);
		}
	}

	public void OnConnected(Connection connection)
	{
		var client = Get(connection);

		if (client == null)
		{
			return;
		}

		if (!client.IsConnected)
		{
			Logger.Warn($"Client {client.Connection?.username}[{client.Connection?.userid}] is not connected to deliver ping.");
			return;
		}

		if (client.HasCarbonClient)
		{
			Logger.Warn($"Already connected with Carbon for client {client.Connection?.username}[{client.Connection?.userid}].");
			return;
		}

		using var packet = RPCList.Get();
		client.Send("ping", packet, bypassChecks: true);
	}
	public void OnDisconnected(Connection connection)
	{
		var client = Get(connection);

		client.OnDisconnect();

		DisposeClient(client);
	}

	public ICarbonClient Get(Connection connection)
	{
		if (connection == null)
		{
			return null;
		}

		if (!Clients.TryGetValue(connection, out var client))
		{
			Clients.Add(connection, client = Make(connection));
		}

		return client;
	}
	public ICarbonClient Get(BasePlayer player)
	{
		var client = Get(player?.Connection);
		client.Player = player;
		return client;
	}

	public bool IsCarbonClient(BasePlayer player)
	{
		var client = Get(player);

		if (client == null)
		{
			return false;
		}

		return client.HasCarbonClient;
	}
	public bool IsCarbonClient(Connection connection)
	{
		var client = Get(connection);

		if (client == null)
		{
			return false;
		}

		return client.HasCarbonClient;
	}

	public void DisposeClient(ICarbonClient client)
	{
		if (Clients.ContainsKey(client.Connection))
		{
			Clients.Remove(client.Connection);
			client.Dispose();
		}
	}

	internal static ICarbonClient Make(Connection connection)
	{
		if (connection == null)
		{
			return null;
		}

		return new CarbonClient
		{
			Connection = connection,
			Player = connection.player as BasePlayer
		};
	}
}
