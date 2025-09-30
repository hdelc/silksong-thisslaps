using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using InControl;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ThisSlaps;

[BepInAutoPlugin(id: "dev.hdelc.ThisSlaps")]
public partial class ThisSlapsPlugin : BaseUnityPlugin
{
    public static SlapActionSet? inputActions;
    public static ManualLogSource Log = null!;
    public static ThisSlapsPlugin instance = null!;
    public ConfigEntry<int> SlapDamageConfig;
    public ConfigEntry<float> SlapKnockbackConfig;

    private AsyncOperationHandle<AudioClip> _slapAudioHandle;
    private AudioClip _slapAudio;
    private SlapController _slapController;
    private Harmony _harmony;
    private ConfigEntry<Key> _slapKeybindConfig;
    

    private void Awake()
    {
        instance = this;
        Log = Logger;
        Log.LogInfo($"Plugin {Name} ({Id}) has loaded!");
        _harmony = new Harmony(Id);
        _harmony.PatchAll(typeof(HeroPatches));

        SetupSlapConfig();
        SetupInputs();
        StartCoroutine(LoadSlapAudio());
    }

    private void SetupSlapConfig()
    {
        SlapDamageConfig = Config.Bind(
            "Slap Properties",
            "Damage",
            15,
            "How much damage the slap deals");
        SlapKnockbackConfig = Config.Bind(
            "Slap Properties",
            "Knockback",
            0f,
            "How much knockback the slap applies"
        );
    }

    private void SetupInputs()
    {
        _slapKeybindConfig = Config.Bind(
            "Inputs",
            "Slap Binding",
            Key.Semicolon,
            "Slap action binding"
        );
        _slapKeybindConfig.SettingChanged += (_, _) => UpdateSlapBind();
        if (!InputManager.IsSetup)
        {
            InputManager.OnSetupCompleted += SetupInputsAction;
        }
        else
        {
            SetupInputsAction();
        }
    }

    private void SetupInputsAction()
    {
        inputActions = new SlapActionSet();
        UpdateSlapBind();
    }

    private void UpdateSlapBind()
    {
        if (inputActions is null)
        {
            return;
        }

        inputActions.slapAction.ClearBindings();
        inputActions.slapAction.AddBinding(new KeyBindingSource(_slapKeybindConfig.Value));
    }

    private IEnumerator LoadSlapAudio()
    {
        // Assets/Audio/SFX/hornet_slap_B_2
        // _slapAudioHandle = Addressables.LoadAssetAsync<AudioClip>("Assets/Audio/SFX/hornet_slap_B_2");
        // Logger.LogInfo("Loading slap audio...");
        // yield return _slapAudioHandle;
        // _slapAudio = _slapAudioHandle.Result;
        yield return AssetManager.ManuallyLoadBundles();
        yield return AssetManager.Initialize();
        _slapAudio = AssetManager.Get<AudioClip>("hornet_slap_B_2");
        if (_slapAudio is null)
        {
            Logger.LogInfo("Slap audio load failed.");
        }
        else
        {
            Logger.LogInfo("Slap audio loaded!");
        }
    }

    private void OnDestroy()
    {
        inputActions?.Destroy();
        Destroy(_slapController);
        _harmony.UnpatchSelf();
        AssetManager.UnloadManualBundles();
    }
}