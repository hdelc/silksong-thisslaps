// Mostly copied from: https://github.com/jngo102/Silksong.LostSinner/blob/393a75bbfd44234246eb8cf97cb4703397227e0d/Source/AssetManager.cs

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace ThisSlaps;

/// <summary>
/// Manages all loaded assets in the mod.
/// </summary>
internal static class AssetManager {
    private static string[] _bundleNames = new[] {
        "sfxstatic_assets_areabellareacoralareagreymoorareashellwoodareasong"
    };

    private static List<AssetBundle> _manuallyLoadedBundles = new();

    private static string[] _assetNames = new[] {
        "hornet_slap_B_2"
    };

    private static readonly Dictionary<Type, Dictionary<string, Object>> Assets = new();

    private static bool _initialized;

    /// <summary>
    /// Load all desired assets from loaded asset bundles.
    /// </summary>
    internal static IEnumerator Initialize() {
        if (_initialized) {
            yield break;
        }

        _initialized = true;

        foreach (var bundle in AssetBundle.GetAllLoadedAssetBundles()) {
            foreach (var assetPath in bundle.GetAllAssetNames()) {
                if (_assetNames.Any(objName => assetPath.Contains(objName))) {
                    var assetLoadHandle = bundle.LoadAssetAsync(assetPath);
                    yield return assetLoadHandle;

                    var loadedAsset = assetLoadHandle.asset;
                    if (loadedAsset != null) {
                        Type assetType = loadedAsset.GetType();
                        string assetName = loadedAsset.name;
                        if (Assets.ContainsKey(assetType)) {
                            var existingAssetSubDict = Assets[assetType];
                            if (existingAssetSubDict != null) {
                                if (existingAssetSubDict.ContainsKey(assetName)) {
                                    var existingAsset = existingAssetSubDict[assetName];
                                    if (existingAsset != null) {
                                        ThisSlapsPlugin.Log.LogWarning($"There is already an asset \"{assetName}\" of type \"{assetType}\"!");
                                    } else {
                                        ThisSlapsPlugin.Log.LogInfo(
                                            $"Key \"{assetName}\" for sub-dictionary of type \"{assetType}\" exists, but its value is null; Replacing with new asset...");
                                        Assets[assetType][assetName] = loadedAsset;
                                    }
                                } else {
                                    ThisSlapsPlugin.Log.LogDebug($"Adding asset {assetName} of type {assetType}...");
                                    Assets[assetType].Add(assetName, loadedAsset);
                                }
                            } else {
                                Assets.Add(assetType, new Dictionary<string, Object>());
                            }
                        } else {
                            Assets.Add(assetType, new Dictionary<string, Object> { [assetName] = loadedAsset });
                            ThisSlapsPlugin.Log.LogDebug(
                                $"Added new sub-dictionary of type {assetType} with initial asset {assetName}.");
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Manually load asset bundles.
    /// </summary>
    internal static IEnumerator ManuallyLoadBundles() {
        foreach (string bundleName in _bundleNames) {
            if (AssetBundle.GetAllLoadedAssetBundles().Any(bundle => bundle.name == bundleName)) {
                continue;
            }
            
            string platformFolder = Application.platform switch {
                RuntimePlatform.WindowsPlayer => "StandaloneWindows64",
                RuntimePlatform.OSXPlayer => "StandaloneOSX",
                RuntimePlatform.LinuxPlayer => "StandaloneLinux64",
                _ => ""
            };

            string bundlePath = Path.Combine(Addressables.RuntimePath, platformFolder, $"{bundleName}.bundle");
            var bundleLoadRequest = AssetBundle.LoadFromFileAsync(bundlePath);
            yield return bundleLoadRequest;

            AssetBundle bundle = bundleLoadRequest.assetBundle;
            _manuallyLoadedBundles.Add(bundle);
            foreach (var assetPath in bundle.GetAllAssetNames()) {
                foreach (var assetName in _assetNames) {
                    if (assetPath.Contains(assetName)) {
                        var assetLoadRequest = bundle.LoadAssetAsync(assetPath);
                        assetLoadRequest.completed += _ => {
                            var loadedAsset = assetLoadRequest.asset;

                            if (loadedAsset is GameObject prefab &&
                                loadedAsset.name == "Lost Lace Ground Tendril") {
                                var tendrilConstraints = prefab.GetComponent<ConstrainPosition>();
                                tendrilConstraints.xMax = 100;
                                tendrilConstraints.yMin = 0;
                                tendrilConstraints.yMax = 100;
                                var tendrilControl = prefab.LocateMyFSM("Control");
                                foreach (var tendrilState in tendrilControl.FsmStates) {
                                    if (tendrilState.Name == "Antic") {
                                        var anticActions = tendrilState.Actions;
                                        foreach (var action in anticActions) {
                                            switch (action) {
                                                case SetFloatValue setFloat:
                                                    setFloat.floatValue.Value = 13 - 2;
                                                    break;
                                                case EaseFloat easeFloat:
                                                    easeFloat.fromValue.Value = 13 - 2;
                                                    easeFloat.toValue.Value = 13;
                                                    break;
                                            }
                                        }

                                        tendrilState.Actions = anticActions;
                                    } else if (tendrilState.Name == "Recycle") {
                                        var recycleActions = tendrilState.Actions;
                                        foreach (var action in recycleActions) {
                                            if (action is SetPosition2D setPosition) {
                                                setPosition.Y.Value = 13 - 2;
                                            }
                                        }

                                        tendrilState.Actions = recycleActions;
                                    }
                                }
                            }

                            var assetType = loadedAsset.GetType();
                            if (Assets.ContainsKey(assetType) && Assets[assetType] != null) {
                                var assetEntry = Assets[assetType];
                                if (assetEntry.ContainsKey(assetName)) {
                                    if (!assetEntry[assetName]) {
                                        ThisSlapsPlugin.Log.LogInfo(
                                            $"Asset \"{assetName}\" of type \"{assetType}\" already exists but is null, replacing it with the newly-loaded asset.");
                                        assetEntry[assetName] = loadedAsset;
                                    } else {
                                        ThisSlapsPlugin.Log.LogWarning(
                                            $"There is already an asset \"{assetName}\" of type \"{assetType}\"!");
                                    }
                                } else {
                                    ThisSlapsPlugin.Log.LogDebug($"Adding asset {assetName} of type {assetType}");
                                    assetEntry.Add(assetName, loadedAsset);
                                }
                            } else {
                                Assets.Add(assetType, new Dictionary<string, Object> { [assetName] = loadedAsset });
                                ThisSlapsPlugin.Log.LogDebug(
                                    $"Added new sub-dictionary of type {assetType} with initial asset {assetName}");
                            }
                        };
                    }
                }
            }
        }
    }

    /// <summary>
    /// Unload all saved assets.
    /// </summary>
    internal static void UnloadAll() {
        foreach (var assetDict in Assets.Values) {
            foreach (var asset in assetDict.Values) {
                Object.DestroyImmediate(asset);
            }
        }

        Assets.Clear();
        GC.Collect();
    }

    /// <summary>
    /// Unload bundles that were manually loaded for this mod.
    /// </summary>
    internal static void UnloadManualBundles() {
        foreach (var bundle in _manuallyLoadedBundles) {
            string bundleName = bundle.name;
            var unloadBundleHandle = bundle.UnloadAsync(true);
            unloadBundleHandle.completed += _ => { ThisSlapsPlugin.Log.LogInfo($"Successfully unloaded bundle \"{bundleName}\""); };
        }

        _manuallyLoadedBundles.Clear();

        foreach (var (_, obj) in Assets[typeof(GameObject)]) {
            if (obj is GameObject gameObject && gameObject.activeSelf) {
                ThisSlapsPlugin.Log.LogInfo($"Recycling all instances of prefab \"{gameObject.name}\"");
                gameObject.RecycleAll();
            }
        }
    }

    /// <summary>
    /// Fetch an asset.
    /// </summary>
    /// <param name="assetName">The name of the asset to fetch.</param>
    /// <typeparam name="T">The type of asset to fetch.</typeparam>
    /// <returns>The fetched object if it exists, otherwise returns null.</returns>
    internal static T? Get<T>(string assetName) where T : Object {
        Type assetType = typeof(T);
        if (Assets.ContainsKey(assetType)) {
            var subDict = Assets[assetType];
            if (subDict != null) {
                if (subDict.ContainsKey(assetName)) {
                    var assetObj = subDict[assetName];
                    if (assetObj != null) {
                        return assetObj as T;
                    }

                    ThisSlapsPlugin.Log.LogError($"Failed to get asset \"{assetName}\"; asset is null!");
                    return null;;
                }

                ThisSlapsPlugin.Log.LogError($"Sub-dictionary for type \"{assetType}\" does not contain key \"{assetName}\"!");
                return null;;
            }

            ThisSlapsPlugin.Log.LogError($"Failed to get asset \"{assetName}\"; sub-dictionary of key \"{assetType}\" is null!");
            return null;
        }

        ThisSlapsPlugin.Log.LogError($"Could not find a sub-dictionary of type \"{assetType}\"!");
        return null;
    }
}