using System;
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

[HarmonyPatch(typeof(ValidBounds), "TestInnerBounds", new Type[] { typeof(Vector3) })]
[UsedImplicitly]
public class ValidBounds_IsInside
{
	public static bool Prefix(Vector3 vPos, ref bool __result)
	{
		if (!Community.Runtime.ClientConfig.Environment.NoMap || vPos.y <= -400 || vPos.y > 5000)
		{
			return true;
		}

		__result = true;
		return false;
	}
}
