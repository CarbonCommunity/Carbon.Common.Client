using System;
using System.Reflection;
using Carbon.Extensions;
using UnityEngine;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

namespace Carbon.Client
{
	public partial class RustComponent
	{
		public Component _instance;

		public static readonly char[] LayerSplitter = new char[] { '|' };

		public bool Apply(GameObject go)
		{
			if (!HandleDisabled(go))
			{
				if (HandleDestroy(go))
				{
					return false;
				}
			}

			return HandleComponents(go);
		}

		internal bool HandleComponents(GameObject go)
		{
			if (!Component.CreateOn.Server || Server == PostProcessMode.Destroyed || _instance != null)
			{
				return false;
			}

			var type = AccessToolsEx.TypeByName(Component.Type);
			_instance = go.AddComponent(type);

			const BindingFlags _monoFlags = BindingFlags.Instance | BindingFlags.Public;

			if (Component.Members != null && Component.Members.Length > 0)
			{
				foreach (var member in Component.Members)
				{
					try
					{
						var field = type.GetField(member.Name, _monoFlags);
						var memberType = field.FieldType;
						var value = (object)null;

						if (memberType == typeof(LayerMask))
						{
							value = new LayerMask { value = member.Value.ToInt() };
						}
						else if (memberType.IsEnum)
						{
							value = Enum.Parse(memberType, member.Value);
						}
						else
						{
							value = Convert.ChangeType(member.Value, memberType);
						}

						if (field != null)
						{
							field?.SetValue(_instance, value);
						}
						else
						{
							Logger.Error($" Couldn't find member '{member.Name}' for '{Component.Type}' on '{go.transform.GetRecursiveName()}'");
						}
					}
					catch (Exception ex)
					{
						Logger.Error($"Failed assigning Rust component member '{member.Name}' to {go.transform.GetRecursiveName()}", ex);
					}
				}
			}

			switch (_instance)
			{
				case TriggerTemperature temperature:
					temperature.OnValidate();
					break;
			}

			return true;
		}
		internal bool HandleDisabled(GameObject go)
		{
			if (Server != PostProcessMode.Disabled)
			{
				return false;
			}

			go.SetActive(false);
			return true;
		}
		internal bool HandleDestroy(GameObject go)
		{
			if (Server != PostProcessMode.Destroyed)
			{
				return false;
			}

			go.SetActive(false);
			return true;
		}
	}
}
