using System;
using System.Linq;
using Carbon.Client;
using Carbon.Extensions;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Carbon.Common.Client.Patches;

/*
 *
 * Copyright (c) 2022-2024 Carbon Community 
 * All rights reserved.
 *
 */

[HarmonyPatch(typeof(BaseCombatEntity), nameof(BaseCombatEntity.OnHealthChanged), new Type[] { typeof(float), typeof(float) })]
[UsedImplicitly]
public class BaseCombatEntity_OnHealthChanged
{
	public static void Prefix(float oldvalue, float newvalue, ref BaseCombatEntity __instance)
	{
		if (!RustPrefab.ServerModel.Models.TryGetValue(__instance, out var model)) return;

		if (model.Animation == null || model.Animation.clip == null || model.HealthAnimations == null)
		{
			return;
		}

		var percentage = ((int)__instance.healthFraction.Scale(0f, 1f, 0f, 100f)).RoundUpToNearestCount(5);
		var currentAnimation = model.Animation.clip.name;
		var nearestAnimation = model.HealthAnimations.OrderBy(x => Math.Abs(x.Key - percentage)).FirstOrDefault();

		if (nearestAnimation.Value != currentAnimation)
		{
			model.ModifyAnimation(nearestAnimation.Value, 0f, 1f, sendUpdate: false);
		}
	}
}
