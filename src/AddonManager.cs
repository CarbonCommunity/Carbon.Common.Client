/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * All rights reserved.
 *
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Carbon.Client.Packets;
using Carbon.Extensions;
using Network;
using Oxide.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace Carbon.Client.Assets;

public class AddonManager : IDisposable
{
	public static AddonManager Instance { get; internal set; } = new();

	public FacepunchBehaviour Persistence => Community.Runtime.CorePlugin.persistence;

	public Dictionary<Addon, CacheAddon> LoadedAddons { get; } = new();
	public Dictionary<string, CachePrefab> Prefabs { get; } = new();

	public List<GameObject> CreatedPrefabs { get; } = new();
	public List<GameObject> CreatedRustPrefabs { get; } = new();
	public List<BaseEntity> CreatedEntities { get; } = new();

	public struct CacheAddon
	{
		public string Url;
		public Asset Scene;
		public Asset Models;
		public string[] ScenePrefabs;

		public bool HasScene()
		{
			return Scene != null && Scene != Models;
		}
	}
	public struct CachePrefab
	{
		public string Path;
		public GameObject Object;
		public List<RustPrefab> RustPrefabs;
	}

	internal void FixName(GameObject gameObject)
	{
		gameObject.name = gameObject.name.Replace("(Clone)", string.Empty);
	}
	internal void ProcessEntity(BaseEntity entity, RustPrefab source)
	{
		entity.Spawn();
		entity.EnableSaving(false);
		entity.skinID = source.Entity.Skin;

		if (source.Entity.Flags != 0)
		{
			entity.SetFlag((BaseEntity.Flags)source.Entity.Flags, true);
		}

		if (entity is BaseCombatEntity combatEntity)
		{
			var combat = source.Entity.Combat;

			if (combat != null)
			{
				if (combat.MaxHealth != -1)
				{
					combatEntity.SetMaxHealth(combat.MaxHealth);
				}

				if (combat.Health != -1)
				{
					combatEntity.SetHealth(combat.Health);
				}
			}
			else
			{
				Logger.Log($"Combat is null for {combatEntity.transform.GetRecursiveName()}");
			}
		}

		source.ApplyModel(entity);
	}

	public Transform LookupParent(Transform origin, string parent)
	{
		return origin == null ? null : origin.Find(parent);
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
			var prefabInstance = CreateBasedOnImpl(prefab);

			if (asset.CachedRustBundle.RustPrefabs.TryGetValue(path, out var rustPrefabs))
			{
				CreateRustPrefabs(prefabInstance.transform, rustPrefabs);
			}

			return prefabInstance;
		}
		else
		{
			Logger.Warn($"Couldn't find '{path}' in any addons or assets. (CreateFromAsset)");
		}

		return null;
	}
	public GameObject CreateFromCache(string path)
	{
		if (Prefabs.TryGetValue(path, out var prefab))
		{
			var prefabInstance = CreateBasedOnImpl(prefab.Object);

			CreateRustPrefabs(prefabInstance.transform, prefab.RustPrefabs);
			return prefabInstance;
		}

		return null;
	}
	public GameObject CreateRustPrefab(Transform target, RustPrefab prefab)
	{
		var lookup = prefab.Lookup();

		if (lookup == null)
		{
			Logger.Warn($"Couldn't find '{prefab.RustPath}' as the asset provided is null. (CreateRustPrefab)");
			return null;
		}

		var entity = lookup.GetComponent<BaseEntity>();
		var isEntity = entity != null;

		if (isEntity && !prefab.Entity.EnforcePrefab)
		{
			var entityInstance = GameManager.server.CreateEntity(prefab.RustPath, prefab.Position.ToVector3(), prefab.Rotation.ToQuaternion());

			if (entityInstance == null) return null;

			ProcessEntity(entityInstance, prefab);

			if (prefab.Parent)
			{
				var parent = LookupParent(target, prefab.ParentPath);

				if (parent != null)
				{
					entityInstance.transform.SetParent(parent, true);
				}
			}

			CreatedEntities.Add(entityInstance);

			return entityInstance.gameObject;
		}
		else
		{
			var instance = GameManager.server.CreatePrefab(prefab.RustPath, prefab.Position.ToVector3(), prefab.Rotation.ToQuaternion(), prefab.Scale.ToVector3());

			if (instance == null) return null;

			CreatedRustPrefabs.Add(instance);

			if (prefab.Parent)
			{
				var parent = LookupParent(target, prefab.ParentPath);

				if (parent != null)
				{
					instance.transform.SetParent(parent, true);
				}
			}

			return instance;
		}
	}
	public void CreateRustPrefabs(Transform target, IEnumerable<RustPrefab> prefabs)
	{
		if (prefabs == null)
		{
			return;
		}

		foreach(var prefab in prefabs)
		{
			CreateRustPrefab(target, prefab);
		}
	}

	public void CreateFromCacheAsync(string path, Action<GameObject> callback = null)
	{
		if (string.IsNullOrEmpty(path))
		{
			Logger.Warn($"Couldn't find '{path}' as it's an empty string. (CreateFromCacheAsync)");
			callback?.Invoke(null);
			return;
		}

		if (Prefabs.TryGetValue(path, out var prefab))
		{
			callback += go =>
			{
				CreateRustPrefabsAsync(go.transform, prefab.RustPrefabs);
			};

			Persistence.StartCoroutine(CreateBasedOnAsyncImpl(prefab.Object, callback));
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
			callback += go =>
			{
				if (asset.CachedRustBundle.RustPrefabs.TryGetValue(path, out var rustPrefabs))
				{
					CreateRustPrefabsAsync(go.transform, rustPrefabs);
				}
			};
			Persistence.StartCoroutine(CreateBasedOnAsyncImpl(prefab, callback));
		}
		else
		{
			Logger.Warn($"Couldn't find '{path}' in any addons or assets. (CreateFromAssetAsync)");
			callback?.Invoke(null);
		}
	}
	public void CreateRustPrefabAsync(Transform target, RustPrefab prefab)
	{
		var lookup = prefab.Lookup();

		if (lookup == null)
		{
			Logger.Warn($"Couldn't find '{prefab.RustPath}' as the asset provided is null. (CreateRustPrefabAsync)");
			return;
		}

		var entity = lookup.GetComponent<BaseEntity>();
		var isEntity = entity != null;

		if (isEntity && !prefab.Entity.EnforcePrefab)
		{
			var entityInstance = GameManager.server.CreateEntity(prefab.RustPath, prefab.Position.ToVector3(), prefab.Rotation.ToQuaternion());
			ProcessEntity(entityInstance, prefab);

			if (prefab.Parent)
			{
				var parent = LookupParent(target, prefab.ParentPath);

				if (parent != null)
				{
					entityInstance.transform.SetParent(parent, true);
				}
			}

			CreatedEntities.Add(entityInstance);
		}
		else
		{
			Persistence.StartCoroutine(CreateBasedOnAsyncImpl(lookup, instance =>
			{
				prefab.Apply(instance);

				if (prefab.Parent)
				{
					var parent = LookupParent(target, prefab.ParentPath);

					if (parent != null)
					{
						instance.transform.SetParent(parent, true);
					}
				}

				// prefab.ApplyModel(go, go.GetComponent<Model>() ?? go.GetComponentInChildren<Model>());
			}));
		}
	}
	public void CreateRustPrefabsAsync(Transform target, IEnumerable<RustPrefab> prefabs)
	{
		if (prefabs == null)
		{
			return;
		}

		Persistence.StartCoroutine(CreateBasedOnPrefabsAsyncImpl(target, prefabs));
	}

	#region Helpers

	internal GameObject CreateBasedOnImpl(GameObject source)
	{
		if (source == null)
		{
			return null;
		}

		var result = UnityEngine.Object.Instantiate(source);
		CreatedPrefabs.Add(result);

		FixName(result);

		return result;
	}
	internal IEnumerator CreateBasedOnAsyncImpl(GameObject gameObject, Action<GameObject> callback = null)
	{
		var result = (GameObject)null;

		yield return result = UnityEngine.Object.Instantiate(gameObject);
		CreatedPrefabs.Add(result);

		FixName(result);

		callback?.Invoke(result);
	}
	internal IEnumerator CreateBasedOnPrefabsAsyncImpl(Transform target, IEnumerable<RustPrefab> prefabs)
	{
		foreach (var prefab in prefabs)
		{
			var lookup = prefab.Lookup();

			if(lookup == null)
			{
				Logger.Warn($"Couldn't find '{prefab.RustPath}' as the asset provided is null. (CreateBasedOnPrefabsAsyncImpl)");
				continue;
			}

			var entity = lookup.GetComponent<BaseEntity>();
			var isEntity = entity != null;

			if (isEntity && !prefab.Entity.EnforcePrefab)
			{
				var entityInstance = GameManager.server.CreateEntity(prefab.RustPath, prefab.Position.ToVector3(), prefab.Rotation.ToQuaternion());
				ProcessEntity(entityInstance, prefab);

				if (prefab.Parent)
				{
					var parent = LookupParent(target, prefab.ParentPath);

					if (parent != null)
					{
						entityInstance.transform.SetParent(parent, true);
					}
				}

				CreatedEntities.Add(entityInstance);
			}
			else
			{
				var instance = (GameObject)null;

				yield return instance = GameManager.server.CreatePrefab(prefab.RustPath, prefab.Position.ToVector3(), prefab.Rotation.ToQuaternion(), prefab.Scale.ToVector3());

				if (prefab.Parent)
				{
					var parent = LookupParent(target, prefab.ParentPath);

					if (parent != null)
					{
						instance.transform.SetParent(parent, true);
					}
				}

				CreatedRustPrefabs.Add(instance);
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
		foreach (var prefab in CreatedPrefabs)
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

		foreach (var addon in LoadedAddons)
		{
			foreach (var asset in addon.Key.Assets)
			{
				try
				{
					asset.Value.Dispose();
				}
				catch (Exception ex)
				{
					Logger.Warn($"[AddonManager] Failed disposing of asset '{asset.Key}' (of addon {addon.Key.Name}) ({ex.Message})\n{ex.StackTrace}");
				}
			}
		}
	}

	public void Deliver(CarbonClient client, bool uninstallAll, bool asynchronous)
	{
		using var request = new AddonRequest
		{
			Manifests = LoadedAddons.Select(x => x.Key.GetManifest()).ToArray(),
			UninstallAll = uninstallAll,
			Asynchronous = asynchronous
		};

		client.Send("addonrequest", request);

		Logger.Log($"{client.Connection} received addon download request");
	}

	public void Install(List<Addon> addons)
	{
		foreach (var addon in addons)
		{
			foreach (var asset in addon.Assets)
			{
				asset.Value.UnpackBundle();
			}

			if (!LoadedAddons.ContainsKey(addon))
			{
				Logger.Log($" C4C: Installed addon '{addon.Name} v{addon.Version}' by {addon.Author}");
				LoadedAddons.Add(addon, GetAddonCache(addon));
			}
		}

		CreateScenePrefabs(false);
	}
	public IEnumerator InstallAsync(List<Addon> addons, Action callback = null)
	{
		foreach (var addon in addons)
		{
			foreach (var asset in addon.Assets)
			{
				yield return asset.Value.UnpackBundleAsync();
			}

			if (!LoadedAddons.ContainsKey(addon))
			{
				Logger.Log($" C4C: Installed addon '{addon.Name} v{addon.Version}' by {addon.Author}");
				LoadedAddons.Add(addon, GetAddonCache(addon));
			}
		}

		CreateScenePrefabs(true);

		callback?.Invoke();
	}
	public void Uninstall(bool prefabs = true, bool rustPrefabs = true, bool customPrefabs = true, bool entities = true)
	{
		if (rustPrefabs)
		{
			if(CreatedRustPrefabs.Count != 0) Console.WriteLine($" C4C: Cleared {CreatedRustPrefabs.Count:n0} Rust {CreatedRustPrefabs.Count.Plural("prefab", "prefabs")}");
			ClearRustPrefabs();
		}
		if (prefabs)
		{
			if(CreatedPrefabs.Count != 0) Console.WriteLine($" C4C: Cleared {CreatedPrefabs.Count:n0} {CreatedPrefabs.Count.Plural("prefab", "prefabs")}");
			ClearPrefabs();
		}
		if (customPrefabs)
		{
			if(Prefabs.Count != 0) Console.WriteLine($" C4C: Cleared {Prefabs.Count:n0} custom prefab cache {Prefabs.Count.Plural("element", "elements")}");
			ClearCustomPrefabs();
		}
		if (entities)
		{
			if(CreatedEntities.Count != 0) Console.WriteLine($" C4C: Cleared {CreatedEntities.Count:n0} {CreatedEntities.Count.Plural("entity", "entities")}");
			ClearEntities();
		}

		if(LoadedAddons.Count != 0) Console.WriteLine($" C4C: Done disposing total of {LoadedAddons.Count:n0} {LoadedAddons.Count.Plural("addon", "addons")} with {LoadedAddons.Sum(x => x.Key.Assets.Count):n0} assets from memory");
		foreach (var addon in LoadedAddons)
		{
			foreach (var asset in addon.Key.Assets)
			{
				try
				{
					asset.Value.Dispose();
				}
				catch (Exception ex)
				{
					Console.WriteLine($" C4C: Failed disposing asset '{asset.Key}' of addon {addon.Key.Name} ({ex.Message})\n{ex.StackTrace}");
				}
			}
		}

		LoadedAddons.Clear();
	}

	public void CreateScenePrefabs(bool async)
	{
		foreach (var addon in LoadedAddons)
		{
			if (addon.Value.ScenePrefabs == null) continue;

			foreach (var prefab in addon.Value.ScenePrefabs)
			{
				if (async)
				{
					CreateFromCacheAsync(prefab, prefabInstance =>
					{
						OnPrefabInstanceCreated(prefabInstance);
					});
				}
				else
				{
					OnPrefabInstanceCreated(CreateFromCache(prefab));
				}

				void OnPrefabInstanceCreated(GameObject prefabInstance)
				{
					if (prefabInstance == null)
					{
						return;
					}

					// OnCustomScenePrefab(GameObject, string, CacheAddon, Addon)
					HookCaller.CallStaticHook(3209444769, prefabInstance, prefab, addon.Value, addon.Key);

					Logger.Debug($" C4C: Created prefab '{prefab}'");
				}
			}
		}
	}

	internal WebClient _client = new();

	public async Task<List<Addon>> LoadAddons(string[] addons, bool async = true)
	{
		var addonResults = new List<Addon>();

		foreach (var addon in addons)
		{
			if (addon.StartsWith("http"))
			{
				if (async)
				{
					await Community.Runtime.CorePlugin.webrequest.EnqueueDataAsync(addon, null, (code, data) =>
					{
						OnData(data);
					}, Community.Runtime.CorePlugin);
				}
				else
				{
					var data = _client.DownloadData(addon);
					OnData(data);
				}

				void OnData(byte[] data)
				{
					Logger.Warn(
						$" C4C: Content downloaded '{Path.GetFileName(addon)}' ({ByteEx.Format(data.Length, stringFormat: "{0}{1}").ToLower()})");

					try
					{
						var instance = Addon.ImportFromBuffer(data);
						instance.Url = addon;

						addonResults.Add(instance);
					}
					catch (Exception ex)
					{
						Logger.Error($" C4C: Addon file protocol out of date or invalid.", ex);
					}
				}
			}
			else
			{
				if (OsEx.File.Exists(addon))
				{
					try
					{
						var data = OsEx.File.ReadBytes(addon);
						Logger.Warn($" C4C: Content loaded locally '{Path.GetFileName(addon)}' ({ByteEx.Format(data.Length, stringFormat: "{0}{1}").ToLower()})");

						var instance = Addon.ImportFromBuffer(data);
						instance.Url = addon;
                        addonResults.Add(instance);
					}
					catch(Exception ex)
					{
						Logger.Error($" C4C: Addon file protocol out of date or invalid.", ex);
					}
				}
				else
				{
					Logger.Warn($" C4C: Couldn't find Addon file at path: {addon}");
				}
			}
		}

		return addonResults;
	}

	public CacheAddon GetAddonCache(Addon addon)
	{
		CacheAddon cache = default;
		cache.Url = addon.Url;
		cache.Scene = addon.Assets.FirstOrDefault(x => x.Key == "scene").Value;
		cache.Models = addon.Assets.FirstOrDefault(x => x.Key == "models").Value;

		if(cache.Scene != null)
		{
			cache.ScenePrefabs = cache.Scene.CachedBundle.GetAllAssetNames();
		}

		return cache;
	}

	public void ClearPrefabs()
	{
		foreach (var prefab in CreatedPrefabs)
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

		CreatedPrefabs.Clear();
	}
	public void ClearRustPrefabs()
	{
		foreach (var prefab in CreatedRustPrefabs)
		{
			try
			{
				UnityEngine.Object.Destroy(prefab);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed disposing a Rust prefab ({ex.Message})\n{ex.StackTrace}");
			}
		}

		CreatedRustPrefabs.Clear();
	}
	public void ClearCustomPrefabs()
	{
		foreach (var prefab in Prefabs)
		{
			try
			{
				UnityEngine.Object.Destroy(prefab.Value.Object);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed disposing asset '{prefab.Key}' ({ex.Message})\n{ex.StackTrace}");
			}
		}

		Prefabs.Clear();
	}
	public void ClearEntities()
	{
		foreach (var entity in CreatedEntities)
		{
			try
			{
				if (entity.isServer && !entity.IsDestroyed)
				{
					entity.Kill();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed disposing a prefab ({ex.Message})\n{ex.StackTrace}");
			}
		}

		CreatedEntities.Clear();
	}
}
