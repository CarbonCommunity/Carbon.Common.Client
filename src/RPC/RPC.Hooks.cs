using Carbon.Client.Packets;
using Oxide.Core;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

namespace Carbon.Client;

public class RPCHooks
{
	[RPC.Method("pong")]
	private static void Pong(CarbonClient client, Network.Message message)
	{
		if (client.HasCarbonClient)
		{
			Logger.Warn($"Player '{client.Connection}' attempted registering twice.");
			return;
		}

		var result = client.Receive<RPCList>(message);
		result.Sync();
		result.Dispose();

		client.HasCarbonClient = true;
		client.Send("clientinfo");
		Logger.Log($"{client.Connection} joined with Carbon client");

		client.OnConnected();
	}

	[RPC.Method("inventoryopen")]
	private static void InventoryOpen(CarbonClient client, Network.Message message)
	{
		// OnInventoryOpen
		HookCaller.CallStaticHook(3601759205, client);
	}

	[RPC.Method("inventoryclose")]
	private static void InventoryClose(CarbonClient client, Network.Message message)
	{
		// OnInventoryClose
		HookCaller.CallStaticHook(3858974801, client);
	}

	[RPC.Method("clientinfo")]
	private static void ClientInfo(CarbonClient client, Network.Message message)
	{
		var info = client.Receive<ClientInfo>(message);

		// client.ScreenWidth = info.ScreenWidth;
		// client.ScreenHeight = info.ScreenHeight;
	}

	[RPC.Method("hookcall")]
	private static void HookCall(CarbonClient client, Network.Message message)
	{
		var info = client.Receive<HookCall>(message);

		Interface.CallHook(info.Hook, client);
	}
}
