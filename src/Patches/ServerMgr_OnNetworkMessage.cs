using System;
using Carbon.Client;
using HarmonyLib;
using JetBrains.Annotations;
using Network;
using UnityEngine;

namespace Carbon.Common.Client.Patches;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

[HarmonyPatch(typeof(ServerMgr), nameof(ServerMgr.OnNetworkMessage), new Type[] { typeof(Network.Message) })]
[UsedImplicitly]
public class ServerMgr_OnNetworkMessage
{
	public static bool Prefix(Message packet)
	{
		var position = packet.read.Position;

		if (packet.type == CarbonClientManager.PACKET_ID)
		{
			Carbon.Client.RPC.HandleRPCMessage(packet.connection, packet.read.UInt32(), packet);
			return false;
		}

		packet.read.Position = position;
		return true;
	}
}
