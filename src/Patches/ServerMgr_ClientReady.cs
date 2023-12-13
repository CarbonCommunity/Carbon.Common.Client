using System;
using Carbon.Client;
using Carbon.Extensions;
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

[HarmonyPatch(typeof(ServerMgr), nameof(ServerMgr.ClientReady), new Type[] { typeof(Message) })]
[UsedImplicitly]
public class ServerMgr_ClientReady
{
	public static void Prefix(Message packet)
	{
		if (packet.connection != null && packet.connection.ToCarbonClient() is CarbonClient client)
		{
			// OnCarbonClientReady
			HookCaller.CallStaticHook(4004023948, client);
		}
	}
}
