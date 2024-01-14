using System;
using Facepunch.Extend;
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

[HarmonyPatch(typeof(TerrainHeightMap), nameof(TerrainHeightMap.GetHeight), new Type[] { typeof(Vector3) })]
[UsedImplicitly]
public class TerrainHeightMap_GetHeight
{
	public static void Postfix(Vector3 worldPos, ref float __result)
	{
		if (Community.Runtime.ClientConfig.Environment.NoMap)
		{
			__result = __result.Clamp(0f, float.MaxValue);
		}
	}
}
