using System;
using System.Collections.Generic;
using System.Linq;
using Carbon.Client.Assets;
using Carbon.Client.Packets;
using Carbon.Extensions;
using Facepunch;
using Network;
using UnityEngine;
using UnityEngine.Serialization;

namespace Carbon.Client
{
	public partial class RustPrefab
	{
		public GameObject Lookup()
		{
			return GameManager.server.FindPrefab(RustPath);
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
			public Dictionary<int, string> HealthAnimations;

			#endregion

			#region Collision

			public bool EntitySolidCollision;
			public bool EntityTriggerCollision;
			public List<Collider> OriginalColliders = new();

			#endregion

			public static Dictionary<BaseEntity, ServerModel> Models = new();

			public void Setup(BaseEntity entity, ModelData model)
			{
				Entity = entity;
				EntitySolidCollision = model.EntitySolidCollision;
				EntityTriggerCollision = model.EntityTriggerCollision;

				if (!Models.ContainsKey(entity))
				{
					Models.Add(entity, this);
				}

				Model = model;

				OriginalColliders.AddRange(entity.GetComponents<Collider>().Concat(entity.GetComponentsInChildren<Collider>(true)));

				if (!model.EntitySolidCollision)
				{
					foreach (var collider in OriginalColliders)
					{
						if (model.EntityTriggerCollision && collider.isTrigger)
						{
							continue;
						}

						if (!collider.isTrigger)
						{
							Destroy(collider);
						}
					}
				}

				AddonManager.Instance.CreateFromCacheAsync(Model.PrefabPath, prefab =>
				{
					try
					{
						if (prefab == null)
						{
							return;
						}

						if (Model.NetworkAnimation)
						{
							Animation = prefab.GetComponent<Animation>();

							ProcessAnimations();
						}

						prefab.transform.SetParent(entity.transform, false);
						prefab.transform.localPosition = Vector3.zero;
						prefab.transform.localRotation = Quaternion.identity;
						prefab.transform.localScale = Vector3.one;

						OnCustomModelCreated?.Invoke(this);

						var currentSubscribers = new List<Connection>();
						var action = new Action(() =>
						{
							var subscribers = Entity.GetSubscribers();

							if (subscribers == null)
							{
								return;
							}

							EntityModelAnimSync animPacket = null;

							var isInvalid = false;

							if (Animation != null)
							{
								animPacket = new EntityModelAnimSync();
								var clip = Animation.clip;
								var state = Animation[clip.name];
								animPacket.EntityId = Entity.net.ID.Value;
								animPacket.Clip = clip.name;

								if (state != null)
								{
									animPacket.Time = state.time;
									animPacket.Speed = state.speed;
									animPacket.Replay = true;
								}
								else
								{
									isInvalid = true;
								}
							}

							if (isInvalid)
							{
								return;
							}

							using var modelPacket = new EntityModel
							{
								EntityId = Entity.net.ID.Value,
								PrefabName = Model.PrefabPath,
								EntitySolidCollision = Model.EntitySolidCollision,
								EntityTriggerCollision = Model.EntityTriggerCollision,
								AnimPacket = animPacket
							};

							foreach (var subscriber in subscribers.Where(subscriber =>
								         !currentSubscribers.Contains(subscriber)))
							{
								if (Community.Runtime.CarbonClientManager.Get(subscriber) is not CarbonClient client)
									continue;

								SendSync(modelPacket, client);

								currentSubscribers.Insert(0, subscriber);
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
						action.Invoke();

						if (Animation != null && model.SyncAnimation)
						{
							InvokeRepeating(SendAnimationUpdate, 1f, RandomEx.GetRandomFloat(4f, 8f));
						}
					}
					catch (Exception ex)
					{
						Logger.Error($"Failed setting up custom Server Model '{Model.PrefabPath}'", ex);
					}
				});
			}
			public void SendSync(EntityModel modelPacket, CarbonClient client)
			{
				client.Send("entitymodel", modelPacket);
			}
			public void ModifyAnimation(string clip = null, float? time = null, float? speed = null, bool replay = false, bool sendUpdate = true)
			{
				if (!string.IsNullOrEmpty(clip))
				{
					if (Animation.clip.name != clip)
					{
						foreach (AnimationState animState in Animation)
						{
							if (animState.clip.name == clip)
							{
								Animation.clip = animState.clip;
								break;
							}
						}

						Animation.Play(PlayMode.StopAll);
					}
					else if (replay)
					{
						Animation.Play(PlayMode.StopAll);
					}
				}
				else if (replay)
				{
					Animation.Play(PlayMode.StopAll);
				}

				var state = Animation[Animation.clip.name];

				if (time != null)
				{
					state.time = time.GetValueOrDefault();
				}

				if (speed != null)
				{
					state.speed = speed.GetValueOrDefault();
				}

				if (sendUpdate)
				{
					SendAnimationUpdate(replay);
				}
			}

			public void SendAnimationUpdate()
			{
				SendAnimationUpdate(false);
			}
			public void SendAnimationUpdate(bool replay)
			{
				if (Animation == null)
				{
					Entity.AdminKill();
					return;
				}

				var subscribers = Entity.GetSubscribers();

				if (subscribers == null)
				{
					return;
				}

				using var animation = new EntityModelAnimSync();
				var clip = Animation.clip;
				var state = Animation[clip.name];
				animation.EntityId = Entity.net.ID.Value;
				animation.Clip = clip.name;

				if (state != null)
				{
					animation.Time = state.time;
					animation.Speed = state.speed;
					animation.Replay = replay;
				}
				else
				{
					return;
				}

				foreach (var subscriber in subscribers)
				{
					var client = Community.Runtime.CarbonClientManager.Get(subscriber);

					if (!client.IsConnected || !client.HasCarbonClient) continue;

					client.Send("entitymodelanimsync", animation);
				}
			}

			const string healthStr = "health_";

			public void ProcessAnimations()
			{
				if (Animation == null)
				{
					return;
				}

				var animations = Pool.GetList<int>();

				foreach (AnimationState animState in Animation)
				{
					var name = animState.clip.name;
					var value = name.Replace(healthStr, string.Empty).ToInt(-1);

					if (value == -1)
					{
						continue;
					}

					animations.Add(value);
				}

				HealthAnimations = HealthAnimations ??= new();

				foreach (var animation in animations.OrderByDescending(x => x))
				{
					HealthAnimations.Add(animation, $"health_{animation}");
				}

				Pool.FreeList(ref animations);
			}

			public void OnDestroy()
			{
				Models.Remove(Entity);
			}
		}
	}
}
