using System;
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

			#region Collision

			public bool OriginalCollision;
			public List<Collider> OriginalColliders = new();

			#endregion

			public static Dictionary<BaseEntity, ServerModel> Models = new();

			public void Setup(BaseEntity entity, ModelData model)
			{
				Entity = entity;
				OriginalCollision = model.OriginalCollision;

				if (!Models.ContainsKey(entity))
				{
					Models.Add(entity, this);
				}

				Model = model;

				OriginalColliders.AddRange(entity.GetComponents<Collider>().Concat(entity.GetComponentsInChildren<Collider>()));

				if (!model.OriginalCollision)
				{
					foreach (var collider in OriginalColliders)
					{
						Destroy(collider);
					}
				}

				AddonManager.Instance.CreateFromCacheAsync(Model.PrefabPath, model =>
				{
					if (model == null)
					{
						return;
					}

					if (Model.NetworkAnimation)
					{
						Animation = model.GetComponent<Animation>();
					}

					model.transform.SetParent(entity.transform, false);
					model.transform.localPosition = Vector3.zero;
					model.transform.localRotation = Quaternion.identity;
					model.transform.localScale = Vector3.one;

					OnCustomModelCreated?.Invoke(this);

					var currentSubscribers = new List<Connection>();
					var subscribers = Entity.GetSubscribers();

					var action = new Action(() =>
					{
						if (subscribers == null)
						{
							return;
						}

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
					action.Invoke();

					if (Animation != null)
					{
						InvokeRepeating(SendAnimationUpdate, 1f, RandomEx.GetRandomFloat(4f, 8f));
					}
				});
			}

			public void SendSync(CarbonClient client)
			{
				using var model = new EntityModel();
				model.EntityId = Entity.net.ID.Value;
				model.PrefabName = Model.PrefabPath;
				model.OriginalCollision = Model.OriginalCollision;
				client.Send("entitymodel", model);
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
				animation.Time = state.time;
				animation.Speed = state.speed;
				animation.Replay = replay;

				foreach (var subscriber in subscribers)
				{
					var client = Community.Runtime.CarbonClientManager.Get(subscriber);

					if (!client.IsConnected || !client.HasCarbonClient) continue;

					client.Send("entitymodelanimsync", animation);
				}
			}

			public void OnDestroy()
			{
				Models.Remove(Entity);
			}
		}
	}
}
