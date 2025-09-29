using System;
using System.Collections;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using InControl;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ThisSlaps;

// TODO - adjust the plugin guid as needed
[BepInAutoPlugin(id: "dev.hdelc.ThisSlaps")]
public partial class ThisSlapsPlugin : BaseUnityPlugin
{
    public static SlapActionSet inputActions;
    public static ManualLogSource Log;
    
    private AsyncOperationHandle<AudioClip> _slapAudioHandle;
    private AudioClip _slapAudio;
    private SlapController _slapController;
    private Harmony _harmony;
    private void Awake()
    {
        // Put your initialization logic here
        Log = Logger;
        Log.LogInfo($"Plugin {Name} ({Id}) has loaded!");
        _harmony = new Harmony(Id);
        _harmony.PatchAll(typeof(HeroPatches));
        
        SetupInputs();
        StartCoroutine(LoadSlapAudio());

        // var myLoadedAssetBundle 
        //     = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "aa/StandaloneLinux64/sfxstatic_assets_areabellareacoralareagreymoorareashellwoodareasong"));
        // Logger.LogInfo(Application.streamingAssetsPath);
        // if (myLoadedAssetBundle == null) {
        //     Debug.Log("Failed to load AssetBundle!");
        //     return;
        // }
        // _slapAudio = myLoadedAssetBundle.LoadAsset<AudioClip>("hornet_slap_B_2");

        // PlayMakerFSM fsm = new PlayMakerFSM()
    }

    private void SetupInputs()
    {
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
        inputActions.slapAction.AddBinding(new KeyBindingSource(Key.Semicolon));
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
        _harmony.UnpatchSelf();
        AssetManager.UnloadManualBundles();
    }

    private void Update()
    {
        // if (Input.GetKeyDown(KeyCode.F9))
        // {
        //     Logger.LogInfo("Patching silkSpecialFSM...");
        //     PatchSilkFSM();
        // }

        // if (Input.GetKeyDown(KeyCode.Semicolon))
        // {
        // var hc = GameManager.instance.hero_ctrl;
        // var clip = hc.animCtrl.GetClip("Idle Slap");
        // AudioSource playerSource = hc.audioSource;
        // hc.StopAnimationControl();
        // hc.RelinquishControl();
        // hc.animCtrl.animator.AnimationCompleted += ReturnAnimationControlHandler;
        // hc.animCtrl.animator.Play(clip);
        // playerSource.PlayOneShot(_slapAudio);
        // }
    }

    private void ReturnAnimationControlHandler(tk2dSpriteAnimator animator, tk2dSpriteAnimationClip clip)
    {
        var hc = GameManager.instance.hero_ctrl;
        hc.StartAnimationControl();
        hc.RegainControl();
        hc.animCtrl.animator.AnimationCompleted -= ReturnAnimationControlHandler;
    }

    
}