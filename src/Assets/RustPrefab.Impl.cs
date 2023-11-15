using Carbon.Client.Assets;
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
		public void ApplyModel(GameObject target, Model model)
		{
			if (model == null)
			{
				return;
			}

			if (Model != null && !string.IsNullOrEmpty(Model.PrefabPath))
			{
				model.gameObject.SetActiveRecursively(false);

				AddonManager.Instance.CreateFromCacheAsync(Model.PrefabPath, model =>
				{
					model.transform.SetParent(target.transform, false);
					model.transform.localPosition = Vector3.zero;
					model.transform.localRotation = Quaternion.identity;
				});
			}
		}
	}
}
