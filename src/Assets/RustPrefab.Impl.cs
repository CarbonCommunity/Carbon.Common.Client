using System;
using System.Collections.Generic;
using System.Linq;
using Carbon.Client.Assets;
using Carbon.Client.Packets;
using Network;
using Newtonsoft.Json.Serialization;
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

			// model.gameObject.SetActiveRecursively(false);

			AddonManager.Instance.CreateFromAssetAsync(Model.PrefabPath, AddonManager.Instance.FindAsset(Model.PrefabPath), model =>
			{
				model.transform.SetParent(entity.transform, true);
				model.transform.localPosition = Vector3.zero;
				model.transform.localRotation = Quaternion.identity;
			});
		}

		public class ServerModel : FacepunchBehaviour
		{
			public BaseEntity Entity;
			public ModelData Model;

			public static Dictionary<BaseEntity, ServerModel> Models = new();

			public void Setup(BaseEntity entity, ModelData model)
			{
				Entity = entity;
				Models.Add(entity, this);
				Model = model;

				var currentSubscribers = new List<Connection>();
				var subscribers = Entity.GetSubscribers();

				InvokeRepeating(() =>
				{
					foreach (Connection subscriber in subscribers.Where(subscriber => !currentSubscribers.Contains(subscriber)))
					{
						if (Community.Runtime.CarbonClientManager.Get(subscriber) is not CarbonClient client) continue;

						using var modelPacket = new EntityModel();
						modelPacket.EntityId = Entity.net.ID.Value;
						modelPacket.PrefabName = Model.PrefabPath;
						client.Send("entitymodel", modelPacket);

						currentSubscribers.Add(subscriber);
					}

					for(int i = 0; i < currentSubscribers.Count; i++)
					{
						var subscriber = currentSubscribers[i];

						if (subscribers.Contains(subscriber)) continue;

						currentSubscribers.RemoveAt(i);
						i--;
					}
				}, 2f, 5f);
			}

			public void OnDestroy()
			{
				Models.Remove(Entity);
			}
		}
	}
}
