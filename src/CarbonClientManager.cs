using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Carbon.Client.Assets;
using Carbon.Client.Contracts;
using Carbon.Client.Packets;
using Carbon.Client.SDK;
using Carbon.Components;
using Carbon.Extensions;
using HarmonyLib;
using Network;
using UnityEngine;

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

	public const Message.Type PACKET_ID = (Message.Type)77;

	internal const string _PATCH_NAME = "com.carbon.clientpatch";
	internal Harmony _PATCH;

	public int AddonCount => AddonManager.Instance.LoadedAddons.Count;
	public int AssetCount => AddonManager.Instance.LoadedAddons.Sum(x => x.Key.Assets.Count);
	public int SpawnablePrefabsCount => AddonManager.Instance.Prefabs.Count;
	public int PrefabsCount => AddonManager.Instance.CreatedPrefabs.Count;
	public int RustPrefabsCount => AddonManager.Instance.CreatedRustPrefabs.Count;
	public int EntityCount => AddonManager.Instance.CreatedEntities.Count;

	public void Init()
	{
		Community.Runtime.CorePlugin.timer.Every(2f, () =>
		{
			foreach (var client in Clients)
			{
				if (client.Value.HasCarbonClient && client.Value.IsConnected && client.Value.IsDownloadingAddons && client.Value.Player != null)
				{
					client.Value.Player.ClientKeepConnectionAlive(default);
				}
			}
		});
	}
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

		using var packet = RPCList.SERVER_Get();
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

	public void NetworkOldRecoil(bool oldRecoil)
	{
		using var packet = new OldRecoil { Enable = oldRecoil };

		foreach (var client in Clients.Where(x => x.Value.IsConnected && x.Value.HasCarbonClient))
		{
			client.Value.Send("oldrecoil", packet);
		}
	}

	public void SendRequestsToAllPlayers(bool uninstallAll = true, bool loadingScreen = true)
	{
		foreach (var player in BasePlayer.activePlayerList)
		{
			SendRequestToPlayer(player.Connection, uninstallAll, loadingScreen);
		}
	}
	public void SendRequestToPlayer(Connection connection, bool uninstallAll = true, bool loadingScreen = true)
	{
		if (connection == null)
		{
			return;
		}

		var client = connection.ToCarbonClient() as CarbonClient;

		if (!client.HasCarbonClient)
		{
			return;
		}

		AddonManager.Instance.Deliver(client,
			uninstallAll: uninstallAll,
			loadingScreen: loadingScreen,
			urls: Community.Runtime.ClientConfig.NetworkableAddons);
	}

	public void InstallAddons(string[] urls)
	{
		Logger.Warn($" C4C: Downloading {urls.Length:n0} URLs synchronously...");

		var task = AddonManager.Instance.LoadAddons(urls);
		task.Wait();

		AddonManager.Instance.Install(task.Result);

		SendRequestsToAllPlayers();
	}
	public async void InstallAddonsAsync(string[] urls)
	{
		Logger.Warn($" C4C: Downloading {urls.Length:n0} URLs asynchronously...");

		var addons = await AddonManager.Instance.LoadAddons(urls);
		Community.Runtime.CorePlugin.persistence.StartCoroutine(AddonManager.Instance.InstallAsync(addons, () =>
		{
			SendRequestsToAllPlayers();
		}));
	}
	public void UninstallAddons()
	{
		AddonManager.Instance.Uninstall();
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
