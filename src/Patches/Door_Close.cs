using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Carbon.Client;
using HarmonyLib;
using JetBrains.Annotations;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Plugins;

namespace Carbon.Common.Client.Patches;

/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * All rights reserved.
 *
 */

[HarmonyPatch(typeof(Door), nameof(Door.RPC_CloseDoor), new Type[] { typeof(BaseEntity.RPCMessage) })]
[UsedImplicitly]
public class Door_Close
{
	public static void Prefix(BaseEntity.RPCMessage rpc, Door __instance)
	{
		if (!rpc.player.CanInteract(true) || !__instance.canHandOpen || !__instance.IsOpen() || __instance.IsBusy() || __instance.IsLocked())
			return;

		if (!RustPrefab.ServerModel.Models.TryGetValue(__instance, out var model) || model.Animation == null ||
		    model.Animation.clip == null)
		{
			return;
		}

		__instance.model.animator.enabled = false;
		Community.Runtime.CorePlugin.timer.In(model.Animation.clip.length, () => __instance.SetFlag(BaseEntity.Flags.Busy, false));
	}
}

