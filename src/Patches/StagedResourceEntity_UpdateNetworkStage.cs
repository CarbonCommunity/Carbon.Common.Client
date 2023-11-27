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

[HarmonyPatch(typeof(StagedResourceEntity), nameof(StagedResourceEntity.UpdateNetworkStage), new Type[] { })]
[UsedImplicitly]
public class BaseEntity_UpdateNetworkStage
{
	public static void Prefix(ref StagedResourceEntity __instance)
	{
		if (RustPrefab.ServerModel.Models.TryGetValue(__instance, out var model))
		{
			if (model.Animation == null)
			{
				return;
			}

			var stageName = $"stage_{__instance.FindBestStage()}";

			foreach (AnimationState animState in model.Animation)
			{
				if (animState.clip.name != stageName) continue;

				model.ModifyAnimation(animState.clip.name, 0f, 1f);
				break;
			}
		}
	}
}
