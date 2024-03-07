using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Carbon.Components;
using HarmonyLib;
using JetBrains.Annotations;

namespace Carbon.Common.Client.Patches;

/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * All rights reserved.
 *
 */

[HarmonyPatch(typeof(BaseMountable), nameof(BaseMountable.VehicleFixedUpdate))]
[UsedImplicitly]
public class BaseMountable_VehicleFixedUpdate
{
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
	{
		using var array = TemporaryArray<CodeInstruction>.New(instructions.ToArray());
		var x = 0;
		var targetIndex = Array.FindIndex(array.Array, x => x.operand is MethodInfo { Name: "TestDist" });

		if (targetIndex == -1)
		{
			Logger.Error($"Failed patching BaseMountable.VehicleFixedUpdate", new InvalidOperationException("targetIndex is -1"));
			yield break;
		}

		foreach (var instruction in instructions)
		{
			if (x++ != targetIndex)
			{
				yield return instruction;
				continue;
			}

			instruction.operand = float.MaxValue;
			instruction.opcode = OpCodes.Ldc_R4;
			yield return new CodeInstruction(OpCodes.Pop);
			yield return new CodeInstruction(OpCodes.Pop);
			yield return instruction;
		}
	}
}

