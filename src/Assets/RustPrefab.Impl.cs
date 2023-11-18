﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Carbon.Client.Assets;
using Carbon.Client.Packets;
using Carbon.Extensions;
using Network;
using UnityEngine;

namespace Carbon.Client
{
	public partial class RustPrefab
	{
		public GameObject Lookup()
		{
			return GameManager.server.FindPrefab(Path);
		}
		public void Apply(GameObject target)
		{
			target.transform.SetPositionAndRotation(Position.ToVector3(), Rotation.ToQuaternion());
			target.transform.localScale = Scale.ToVector3();
		}
		public void ApplyModel(BaseEntity entity)
		{
			if (Model == null || string.IsNullOrEmpty(Model.PrefabPath)) return;

			var serverModel = entity.gameObject.AddComponent<ServerModel>();
			serverModel.Setup(entity, Model);
		}

		public class ServerModel : FacepunchBehaviour
		{
			public static Action<ServerModel> OnCustomModelCreated;

			public BaseEntity Entity;
			public ModelData Model;

			#region Animation

			public Animation Animation;

			#endregion

			public static Dictionary<BaseEntity, ServerModel> Models = new();

			public void Setup(BaseEntity entity, ModelData model)
			{
				Entity = entity;
				Models.Add(entity, this);
				Model = model;

				AddonManager.Instance.CreateFromCacheAsync(Model.PrefabPath, model =>
				{
					Animation = model.GetComponent<Animation>();

					model.transform.SetParent(entity.transform, false);
					model.transform.localPosition = Vector3.zero;
					model.transform.localRotation = Quaternion.identity;
					model.transform.localScale = Vector3.one;

					OnCustomModelCreated?.Invoke(this);

					Carbon.Logger.Log($"Anim? {Animation == null}");

					var currentSubscribers = new List<Connection>();
					var subscribers = Entity.GetSubscribers();

					var action = new Action(() =>
					{
						foreach (var subscriber in subscribers.Where(subscriber => !currentSubscribers.Contains(subscriber)))
						{
							if (Community.Runtime.CarbonClientManager.Get(subscriber) is not CarbonClient client) continue;

							SendSync(client);

							currentSubscribers.Add(subscriber);
						}

						for (int i = 0; i < currentSubscribers.Count; i++)
						{
							var subscriber = currentSubscribers[i];

							if (subscribers.Contains(subscriber)) continue;

							currentSubscribers.RemoveAt(i);
							i--;
						}
					});

					InvokeRepeating(action, 1f, RandomEx.GetRandomFloat(1f, 3f));

					StartCoroutine(HandleAnimationSync(10f));

					action?.Invoke();
				});
			}

			public void SendSync(CarbonClient client)
			{
				using var model = new EntityModel();
				model.EntityId = Entity.net.ID.Value;
				model.PrefabName = Model.PrefabPath;
				client.Send("entitymodel", model);

			}

			public IEnumerator HandleAnimationSync(float syncTime)
			{
				var subscribers = Entity.GetSubscribers();

				while (true)
				{
					foreach (var subscriber in subscribers)
					{
						var client = Community.Runtime.CarbonClientManager.Get(subscriber);

						if (!client.IsConnected || !client.HasCarbonClient) continue;

						using var animation = new EntityModelAnimSync();
						animation.EntityId = Entity.net.ID.Value;
						animation.Time = Animation == null ? 0 : Animation[Animation.clip.name].normalizedTime;
						client.Send("entitymodelanimsync", animation);

						yield return null;
					}

					yield return CoroutineEx.waitForSeconds(syncTime);
					yield return null;
				}
			}

			public void OnDestroy()
			{
				Models.Remove(Entity);
			}
		}
	}
}
