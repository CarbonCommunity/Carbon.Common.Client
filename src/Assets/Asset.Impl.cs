/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using System;
using System.Collections;
using System.IO;
using Newtonsoft.Json;
using ProtoBuf;
using UnityEngine;
using UnityEngine.Video;

namespace Carbon.Client.Assets;

public partial class Asset : IDisposable
{
	public IEnumerator UnpackBundleAsync()
	{
		if (IsUnpacked)
		{
			Logger.Log($"Already unpacked Asset '{Name}'");
			yield break;
		}

		var request = (AssetBundleCreateRequest)null;
		using var stream = new MemoryStream(Data);
		yield return request = AssetBundle.LoadFromStreamAsync(stream);

		CachedBundle = request.assetBundle;
		Logger.Debug($"Unpacked bundle '{Name}'", 2);

		using var stream2 = new MemoryStream(AdditionalData);
		CachedRustBundle = Serializer.Deserialize<RustBundle>(stream2);

		CachedRustBundle.ProcessComponents(this);

		CacheAssets();
	}
	public void UnpackBundle()
	{
		if(IsUnpacked)
		{
			Logger.Log($" Already unpacked '{Name}'");
			return;
		}

		using var stream = new MemoryStream(Data);
		CachedBundle = AssetBundle.LoadFromStream(stream);
		Logger.Debug($"Unpacked bundle '{Name}'", 2);

		using var stream2 = new MemoryStream(AdditionalData);
		CachedRustBundle = Serializer.Deserialize<RustBundle>(stream2);

		CachedRustBundle.ProcessComponents(this);

		CacheAssets();
	}

	public void CacheAssets()
	{
		foreach(var asset in CachedBundle.GetAllAssetNames())
		{
			var processedAssetPath = asset.ToLower();

			if (!AddonManager.Instance.InstalledCache.ContainsKey(processedAssetPath))
			{
				AddonManager.CachePrefab cache = default;
				cache.Object = CachedBundle.LoadAsset<GameObject>(asset);

				ProcessClientObjects(cache.Object.transform);

				if (CachedRustBundle.RustPrefabs.TryGetValue(processedAssetPath, out var rustPrefabs))
				{
					cache.RustPrefabs = rustPrefabs;
				}

				AddonManager.Instance.InstalledCache.Add(processedAssetPath, cache);
			}
		}
	}

	public T LoadPrefab<T>(string path) where T : UnityEngine.Object
	{
		if (!IsUnpacked)
		{
			UnpackBundle();
		}

		return CachedBundle.LoadAsset<T>(path);
	}
	public T[] LoadAllPrefabs<T>() where T : UnityEngine.Object
	{
		if (!IsUnpacked)
		{
			UnpackBundle();
		}

		return CachedBundle.LoadAllAssets<T>();
	}

	public void ProcessClientObjects(Transform transform)
	{
		void ClearComponent<T>() where T : Component
		{
			var component = transform.GetComponent<T>();

			if (component != null)
			{
				GameObject.Destroy(component);
			}
		}

		ClearComponent<MeshRenderer>();
		ClearComponent<SkinnedMeshRenderer>();
		ClearComponent<AudioSource>();
		ClearComponent<VideoPlayer>();

		foreach (Transform child in transform)
		{
			ProcessClientObjects(child);
		}
	}
}
