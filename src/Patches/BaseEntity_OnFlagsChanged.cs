using System;
using Carbon.Client;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Carbon.Common.Client.Patches;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

[HarmonyPatch(typeof(BaseEntity), nameof(BaseEntity.OnFlagsChanged), new Type[] { typeof(BaseEntity.Flags), typeof(BaseEntity.Flags) })]
[UsedImplicitly]
public class BaseEntity_OnFlagsChanged
{
	public static void Prefix(BaseEntity.Flags old, BaseEntity.Flags next, ref BaseEntity __instance)
	{
		if (!RustPrefab.ServerModel.Models.TryGetValue(__instance, out var model)) return;

		if (model.Animation == null)
		{
			return;
		}

		var added = next & ~old;
		var removed = old & ~next;
		var isRemoved = removed != 0;
		var flagName = (isRemoved ? removed : added).ToString().ToLower();
		var animation = $"{flagName}_{(isRemoved ? 0 : 1)}";
		
		foreach (AnimationState animState in model.Animation)
		{
			if (animState.clip.name != animation) continue;

			model.ModifyAnimation(animState.clip.name, 0f, 1f);
			break;
		}
	}
}
