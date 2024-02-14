using Carbon.Client.Assets;
using UnityEngine;

namespace Carbon.Client
{
	public partial class RustBundle
	{
		public void ProcessComponents(Asset asset)
		{
			foreach (var path in asset.CachedBundle.GetAllAssetNames())
			{
				var unityAsset = asset.CachedBundle.LoadAsset<UnityEngine.Object>(path);

				if (unityAsset is GameObject go)
				{
					Recurse(go.transform);

					void Recurse(Transform transform)
					{
						foreach (Transform subTransform in transform)
						{
							Recurse(subTransform);
						}

						if (Components.TryGetValue(transform.GetRecursiveName().ToLower(), out var components))
						{
							foreach (var component in components)
							{
								if (!component.Apply(transform.gameObject))
								{
									break;
								}
							}
						}
					}
				}
			}
		}
	}
}
