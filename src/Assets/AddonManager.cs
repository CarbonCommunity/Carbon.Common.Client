/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Carbon.Client.Packets;
using Carbon.Extensions;
using Network;
using UnityEngine;

namespace Carbon.Client.Assets;

public class AddonManager : IDisposable
{
	public static AddonManager Instance { get; internal set; } = new();

	public FacepunchBehaviour Persistence => Community.Runtime.CorePlugin.persistence;

	public List<Addon> Installed { get; } = new();
	public Dictionary<string, GameObject> InstalledCache { get; } = new();

	public List<GameObject> PrefabInstances { get; } = new();
	public List<BaseEntity> EntityInstances { get; } = new();
	public List<byte[]> CurrentChunk { get; } = new();

	internal void FixName(GameObject gameObject)
	{
		gameObject.name = gameObject.name.Replace("(Clone)", string.Empty);
	}

	public GameObject CreateFromAsset(string path, Asset asset)
	{
		if (asset == null)
		{
			Logger.Warn($"Couldn't find '{path}' as the asset provided is null. (CreateFromAsset)");
			return null;
		}

		if (string.IsNullOrEmpty(path))
		{
			Logger.Warn($"Couldn't find prefab from asset '{asset.Name}' as it's an empty string. (CreateFromAsset)");
			return null;
		}

		var prefab = asset.LoadPrefab<GameObject>(path);

		if (prefab != null)
		{
			return CreateBasedOnImpl(prefab);
		}
		else
		{
			Logger.Warn($"Couldn't find '{path}' in any addons or assets. (CreateFromAsset)");
		}

		return null;
	}
	public GameObject CreateFromCache(string path)
	{
		if (InstalledCache.TryGetValue(path, out var prefab))
		{
			return CreateBasedOnImpl(prefab);
		}

		return null;
	}
	public GameObject CreateRustPrefab(RustPrefab prefab)
	{
		var lookup = prefab.Lookup();

		if (lookup == null)
		{
			Logger.Warn($"Couldn't find '{prefab.Path}' as the asset provided is null. (CreateRustPrefab)");
			return null;
		}

		var entity = lookup.GetComponent<BaseEntity>();
		var isEntity = entity != null;

		if (isEntity && !prefab.Entity.EnforcePrefab)
		{
			var entityInstance = GameManager.server.CreateEntity(prefab.Path, prefab.Position.ToVector3(), prefab.Rotation.ToQuaternion());
			entityInstance.Spawn();

			if (prefab.Entity.Flags != 0)
			{
				var oldFlags = entityInstance.flags;
				entityInstance.flags = (BaseEntity.Flags)prefab.Entity.Flags;
				entityInstance.OnFlagsChanged(oldFlags, entityInstance.flags);
				entityInstance.SendNetworkUpdate_Flags();
			}

			EntityInstances.Add(entityInstance);
		}
		else
		{
			var instance = GameManager.server.CreatePrefab(prefab.Path, prefab.Position.ToVector3(), prefab.Rotation.ToQuaternion(), prefab.Scale.ToVector3());
			PrefabInstances.Add(instance);

			return instance;
		}

		return null;
	}
	public void CreateRustPrefabs(IEnumerable<RustPrefab> prefabs)
	{
		foreach(var prefab in prefabs)
		{
			CreateRustPrefab(prefab);
		}
	}
	public void CreateRustPrefabsFromAsset(Asset asset)
	{
		if(asset == null)
		{
			Logger.Warn($"Couldn't create Rust prefabs since asset is null. (CreateRustPrefabsFromAsset)");
			return;
		}

		if(asset.CachedRustBundle == null)
		{
			Logger.Warn($"Couldn't create Rust prefabs for '{asset.Name}' since Rust bundle is not cached. (CreateRustPrefabsFromAsset)");
			return;
		}

		CreateRustPrefabs(asset.CachedRustBundle.RustPrefabs);
	}

	public void CreateFromCacheAsync(string path, Action<GameObject> callback = null)
	{
		if (string.IsNullOrEmpty(path))
		{
			Logger.Warn($"Couldn't find '{path}' as it's an empty string. (CreateFromCacheAsync)");
			callback?.Invoke(null);
			return;
		}

		if (InstalledCache.TryGetValue(path, out var prefab))
		{
			Persistence.StartCoroutine(CreateBasedOnAsyncImpl(prefab, callback));
		}
		else
		{
			Logger.Warn($"Couldn't find '{path}' as it hasn't been cached yet. Use 'CreateFromAssetAsync'? (CreateFromCacheAsync)");
			callback?.Invoke(null);
		}
	}
	public void CreateFromAssetAsync(string path, Asset asset, Action<GameObject> callback = null)
	{
		if (asset == null)
		{
			Logger.Warn($"Couldn't find '{path}' as the asset provided is null. (CreateFromAssetAsync)");
			callback?.Invoke(null);
			return;
		}

		if (string.IsNullOrEmpty(path))
		{
			Logger.Warn($"Couldn't find '{path}' as it's an empty string. (CreateFromAssetAsync)");
			callback?.Invoke(null);
			return;
		}

		var prefab = asset.LoadPrefab<GameObject>(path);

		if (prefab != null)
		{
			Persistence.StartCoroutine(CreateBasedOnAsyncImpl(prefab, callback));
		}
		else
		{
			Logger.Warn($"Couldn't find '{path}' in any addons or assets. (CreateFromAssetAsync)");
			callback?.Invoke(null);
		}
	}
	public void CreateRustPrefabAsync(RustPrefab prefab)
	{
		var lookup = prefab.Lookup();

		if (lookup == null)
		{
			Logger.Warn($"Couldn't find '{prefab.Path}' as the asset provided is null. (CreateRustPrefabAsync)");
			return;
		}

		var entity = lookup.GetComponent<BaseEntity>();
		var isEntity = entity != null;

		if (isEntity && !prefab.Entity.EnforcePrefab)
		{
			var entityInstance = GameManager.server.CreateEntity(prefab.Path, prefab.Position.ToVector3(), prefab.Rotation.ToQuaternion());
			entityInstance.Spawn();

			if (prefab.Entity.Flags != 0)
			{
				var oldFlags = entityInstance.flags;
				entityInstance.flags = (BaseEntity.Flags)prefab.Entity.Flags;
				entityInstance.OnFlagsChanged(oldFlags, entityInstance.flags);
				entityInstance.SendNetworkUpdate_Flags();
			}

			prefab.ApplyModel(entityInstance);

			EntityInstances.Add(entityInstance);
		}
		else
		{
			Persistence.StartCoroutine(CreateBasedOnAsyncImpl(lookup, go =>
			{
				prefab.Apply(go);
				// prefab.ApplyModel(go, go.GetComponent<Model>() ?? go.GetComponentInChildren<Model>());
			}));
		}
	}
	public void CreateRustPrefabsAsync(IEnumerable<RustPrefab> prefabs)
	{
		Persistence.StartCoroutine(CreateBasedOnPrefabsAsyncImpl(prefabs));
	}
	public void CreateRustPrefabsFromAssetAsync(Asset asset)
	{
		if (asset == null)
		{
			Logger.Warn($"Couldn't create Rust prefabs since asset is null. (CreateRustPrefabsFromAsset)");
			return;
		}

		if (asset.CachedRustBundle == null)
		{
			Logger.Warn($"Couldn't create Rust prefabs for '{asset.Name}' since Rust bundle is not cached. (CreateRustPrefabsFromAsset)");
			return;
		}

		CreateRustPrefabsAsync(asset.CachedRustBundle.RustPrefabs);
	}

	#region Helpers

	internal GameObject CreateBasedOnImpl(GameObject source)
	{
		if (source == null)
		{
			return null;
		}

		var result = UnityEngine.Object.Instantiate(source);
		PrefabInstances.Add(result);

		FixName(result);

		return result;
	}
	internal IEnumerator CreateBasedOnAsyncImpl(GameObject gameObject, Action<GameObject> callback = null)
	{
		var result = (GameObject)null;

		yield return result = UnityEngine.Object.Instantiate(gameObject);
		PrefabInstances.Add(result);

		FixName(result);

		callback?.Invoke(result);
	}
	internal IEnumerator CreateBasedOnPrefabsAsyncImpl(IEnumerable<RustPrefab> prefabs)
	{
		foreach (var prefab in prefabs)
		{
			var lookup = prefab.Lookup();

			if(lookup == null)
			{
				Logger.Warn($"Couldn't find '{prefab.Path}' as the asset provided is null. (CreateBasedOnPrefabsAsyncImpl)");
				continue;
			}

			var entity = lookup.GetComponent<BaseEntity>();
			var isEntity = entity != null;

			if (isEntity && !prefab.Entity.EnforcePrefab)
			{
				var entityInstance = GameManager.server.CreateEntity(prefab.Path, prefab.Position.ToVector3(), prefab.Rotation.ToQuaternion());
				entityInstance.Spawn();

				if (prefab.Entity.Flags != 0)
				{
					var oldFlags = entityInstance.flags;
					entityInstance.flags = (BaseEntity.Flags)prefab.Entity.Flags;
					entityInstance.OnFlagsChanged(oldFlags, entityInstance.flags);
					entityInstance.SendNetworkUpdate_Flags();
				}

				prefab.ApplyModel(entityInstance);

				EntityInstances.Add(entityInstance);
			}
			else
			{
				var instance = (GameObject)null;

				yield return instance = GameManager.server.CreatePrefab(prefab.Path, prefab.Position.ToVector3(), prefab.Rotation.ToQuaternion(), prefab.Scale.ToVector3());

				PrefabInstances.Add(instance);
			}
		}
	}
	internal IEnumerator CreateBasedOnEnumerableAsyncImpl(IEnumerable<GameObject> gameObjects, Action<GameObject> callback = null)
	{
		foreach(var gameObject in gameObjects)
		{
			yield return CreateBasedOnAsyncImpl(gameObject, callback);
		}
	}

	#endregion

	public void Dispose()
	{
		foreach (var prefab in PrefabInstances)
		{
			try
			{
				if (prefab == null) continue;

				UnityEngine.Object.Destroy(prefab);
			}
			catch (Exception ex)
			{
				Logger.Warn($"[AddonManager] Failed destroying cached prefab ({ex.Message})\n{ex.StackTrace}");
			}
		}

		foreach (var addon in Installed)
		{
			foreach (var asset in addon.Assets)
			{
				try
				{
					asset.Value.Dispose();
				}
				catch (Exception ex)
				{
					Logger.Warn($"[AddonManager] Failed disposing of asset '{asset.Key}' (of addon {addon.Name}) ({ex.Message})\n{ex.StackTrace}");
				}
			}
		}
	}

	public void ProcessChunk(AddonDownload download)
	{
		switch (download.Format)
		{
			case AddonDownload.Formats.Whole:
			case AddonDownload.Formats.First:
				Console.WriteLine($"Initial chunk");
				CurrentChunk.Clear();
				break;
		}

		CurrentChunk.Add(download.BufferChunk);

		switch (download.Format)
		{
			case AddonDownload.Formats.Whole:
			case AddonDownload.Formats.Last:
				Console.WriteLine($"Finalized chunk");

				var completeBuffer = CurrentChunk.SelectMany(x => x).ToArray();
				Installed.Add(Addon.ImportFromBuffer(completeBuffer));
				Array.Clear(completeBuffer, 0, completeBuffer.Length);
				break;
		}

		download.Dispose();
	}

	public async void Deliver(CarbonClient client, bool uninstallAll, List<Addon> addons, bool loadingScreen)
	{
		client.Send("addonrequest", new AddonRequest
		{
			AddonCount = addons.Count,
			BufferSize = addons.Sum(x => x.Buffer.Length),
			LoadingScreen = loadingScreen
		});

		Logger.Log($"Sent download request to {client.Connection} with {addons.Count:n0} addons...");

		var sentMain = true;
		foreach (var addon in addons)
		{
			var buffer = addon.Buffer;
			var chunks = buffer.Chunkify(4000000);

			Logger.Warn($" Processing {chunks.Length} chunks (total of {buffer.Length} or {ByteEx.Format(buffer.Length)})");

			for (int i = 0; i < chunks.Length; i++)
			{
				client.Send("addondownload", new AddonDownload
				{
					BufferChunk = chunks[i],
					Format = chunks.Length == 1 ? AddonDownload.Formats.Whole : i == 0 ? AddonDownload.Formats.First : i == chunks.Length - 1 ? AddonDownload.Formats.Last : AddonDownload.Formats.Content,
					UninstallAll = sentMain
				});

				if (sentMain)
				{
					sentMain = false;
				}

				await AsyncEx.WaitForSeconds(0.1f);
			}

			await AsyncEx.WaitForSeconds(0.75f);
		}

		client.Send("addonfinalized");
	}
	public void Deliver(CarbonClient client, bool uninstallAll, bool loadingScreen, params string[] urls)
	{
		client.Send("addonrequest", new AddonRequest
		{
			AddonCount = urls.Length,
			IsUrlDownload = true,
			LoadingScreen = loadingScreen
		});

		Logger.Log($"Sent download request to {client.Connection} with {urls.Length:n0} addon URLs...");

		client.Send("addondownloadurl", new AddonDownloadUrl
		{
			Urls = urls,
			UninstallAll = uninstallAll
		});
	}

	public void Install(List<Addon> addons)
	{
		foreach (var addon in addons)
		{
			foreach (var asset in addon.Assets)
			{
				asset.Value.UnpackBundle();
			}

			Installed.Add(addon);
		}
	}
	public void Uninstall(bool prefabs = true, bool customPrefabs = true, bool entities = true)
	{
		if (prefabs)
		{
			ClearPrefabs();
		}
		if (customPrefabs)
		{
			ClearCustomPrefabs();
		}
		if (entities)
		{
			ClearEntities();
		}

		foreach (var addon in Installed)
		{
			foreach (var asset in addon.Assets)
			{
				try
				{
					asset.Value.Dispose();
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Failed disposing asset '{asset.Key}' of addon {addon.Name} ({ex.Message})\n{ex.StackTrace}");
				}
			}
		}

		Installed.Clear();
	}

	public void ClearPrefabs()
	{
		foreach (var prefab in PrefabInstances)
		{
			try
			{
				UnityEngine.Object.Destroy(prefab);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed disposing a prefab ({ex.Message})\n{ex.StackTrace}");
			}
		}

		PrefabInstances.Clear();
	}
	public void ClearCustomPrefabs()
	{
		foreach (var prefab in InstalledCache)
		{
			try
			{
				UnityEngine.Object.Destroy(prefab.Value);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed disposing asset '{prefab.Key}' ({ex.Message})\n{ex.StackTrace}");
			}
		}

		InstalledCache.Clear();
	}
	public void ClearEntities()
	{
		foreach (var entity in EntityInstances)
		{
			try
			{
				if (entity.isServer && !entity.IsDestroyed)
				{
					entity.AdminKill();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed disposing a prefab ({ex.Message})\n{ex.StackTrace}");
			}
		}

		EntityInstances.Clear();
	}
}
