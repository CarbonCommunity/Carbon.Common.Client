using Carbon.Core;
using HarmonyLib;
using JetBrains.Annotations;

namespace Carbon.Common.Client.Patches;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

[HarmonyPatch(typeof(SaveRestore), nameof(SaveRestore.Load), new System.Type[] { typeof(string), typeof(bool) })]
[UsedImplicitly]
public class SaveRestore_Load
{
	public static void Postfix(string strFilename, bool allowOutOfDateSaves, ref bool __result)
	{
		if (Community.Runtime.ClientConfig.Enabled)
		{
			Community.Runtime.CorePlugin.To<CorePlugin>().ReloadCarbonClientAddons(false);
		}
	}
}

