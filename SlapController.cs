using System;
using System.Collections;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using TeamCherry.SharedUtils;
using UnityEngine;
using UnityEngine.Events;
using Object = System.Object;

namespace ThisSlaps;

[RequireComponent(typeof(HeroController))]
internal class SlapController : MonoBehaviour
{
    private HeroController _heroController = null!;
    private PlayMakerFSM _silkSpecial = null!;
    private GameObject _slapDamager = null!;
    private AudioClip _slapAudio = null!;

    private bool _isSlapping = false;

    private void Awake()
    {
        GetComponents();
        GetAssetRefs();
    }

    private void Start()
    {
        _slapDamager = new GameObject("Slap Damager");
        var collider = _slapDamager.AddComponent<CircleCollider2D>();
        collider.radius = 0.8f;
        collider.isTrigger = true;
        collider.includeLayers = LayerMask.GetMask("Enemies", "Projectiles");

        _slapDamager.transform.parent = gameObject.transform;
        _slapDamager.transform.localPosition = new Vector3(-1, 0);
        var damage = _slapDamager.AddComponent<DamageEnemies>();
        InitializeDamage(damage);
        damage.damageDealt = 1;
        damage.WillDamageEnemyOptions += EnemyDamageLog;
        damage.WillDamageEnemy += PlaySlapAudio;
        UpdateSlapProperties();
        ThisSlapsPlugin.instance.SlapDamageConfig.SettingChanged += UpdateSlapPropertiesEventHandler;
        ThisSlapsPlugin.instance.SlapKnockbackConfig.SettingChanged += UpdateSlapPropertiesEventHandler;
        _slapDamager.SetActive(false);
        PatchSilkFSM();
    }

    private void OnDestroy()
    {
        Destroy(_slapDamager);
        ThisSlapsPlugin.instance.SlapDamageConfig.SettingChanged -= UpdateSlapPropertiesEventHandler;
        ThisSlapsPlugin.instance.SlapKnockbackConfig.SettingChanged -= UpdateSlapPropertiesEventHandler;
    }

    private void UpdateSlapProperties()
    {
        var damage = _slapDamager.GetComponent<DamageEnemies>();
        damage.damageDealt = ThisSlapsPlugin.instance.SlapDamageConfig.Value;
        damage.magnitudeMult = ThisSlapsPlugin.instance.SlapKnockbackConfig.Value;
    }

    private void UpdateSlapPropertiesEventHandler(object sender, EventArgs args)
    {
        UpdateSlapProperties();
    }

    private void PlaySlapAudio()
    {
        _heroController.audioSource.PlayOneShot(_slapAudio);
    }

    private static void InitializeDamage(DamageEnemies damage)
    {
        damage.contactFSMEvent = "";
        damage.damageFSMEvent = "";
        damage.lagHitOptions = new LagHitOptions();
        damage.corpseDirection = new OverrideFloat();
        damage.corpseMagnitudeMult = new OverrideFloat();
        damage.currencyMagnitudeMult = new OverrideFloat();
        damage.damageMultPerHit = [];
        damage.DealtDamage = new UnityEvent(); // TODO: Is there a specific event needed?
        damage.dealtDamageFSMEvent = "";
        damage.deathEvent = "";
        damage.slashEffectOverrides = [];
        damage.targetRecordedFSMEvent = "";
        damage.Tinked = new UnityEvent(); // TODO: Is there a specific event needed?
    }

    private void EnemyDamageLog(HealthManager health, HitInstance hit)
    {
        ThisSlapsPlugin.Log.LogInfo($"Should damage enemy: {health.name}");
    }

    private void GetComponents()
    {
        _heroController = gameObject.GetComponent<HeroController>();
        _silkSpecial = _heroController.silkSpecialFSM;
    }

    private void GetAssetRefs()
    {
        _slapAudio = AssetManager.Get<AudioClip>("hornet_slap_B_2");
    }

    public void CancelSlap()
    {
        if (_isSlapping)
        {
            _isSlapping = false;
            _heroController.StartAnimationControl();
            _heroController.RegainControl();
        }
    }

    public static bool CanSlap()
    {
        HeroController hc = HeroController.instance;
        return hc.CanCast() && hc.cState.onGround && !hc.IsHardLanding();
    }

    private void PatchSilkFSM()
    {
        var slapAudio = AssetManager.Get<AudioClip>("hornet_slap_B_2");
        var owner = new FsmOwnerDefault()
            { gameObject = gameObject, ownerOption = OwnerDefaultOption.SpecifyGameObject };
        var slapOwner = new FsmOwnerDefault()
            { GameObject = _slapDamager, OwnerOption = OwnerDefaultOption.SpecifyGameObject };
        var ensureGroundedAction = new CheckIsCharacterGrounded()
        {
            Target = owner,
            RayCount = new FsmInt() { Value = 3 },
            GroundDistance = new FsmFloat() { Value = 0.2f },
            SkinWidth = new FsmFloat() { Value = -0.05f },
            SkinHeight = new FsmFloat() { Value = 0.1f },
            StoreResult = false,
            NotGroundedEvent = FsmEvent.GetFsmEvent("CANCEL TAUNT"),
            EveryFrame = true,
            BlocksFinish = false
        };

        var checkState = new FsmState(_silkSpecial.Fsm) { name = "Slap Check", isSequence = true };
        checkState.Actions =
        [
            new CheckCanSlap()
            {
                isTrue = FsmEvent.Finished,
                isFalse = FsmEvent.GetFsmEvent("CANCEL")
            }
        ];

        FsmState slapAnticState = new FsmState(_silkSpecial.Fsm) { name = "Slap Antic", isSequence = true };
        slapAnticState.actions =
        [
            new SendMessage()
            {
                gameObject = owner,
                delivery = HutongGames.PlayMaker.Actions.SendMessage.MessageType.SendMessage,
                options = SendMessageOptions.DontRequireReceiver,
                functionCall = new FunctionCall() { FunctionName = "RelinquishControl" }
            },
            new SendMessage()
            {
                gameObject = owner,
                delivery = HutongGames.PlayMaker.Actions.SendMessage.MessageType.SendMessage,
                options = SendMessageOptions.DontRequireReceiver,
                functionCall = new FunctionCall() { FunctionName = "StopAnimationControl" }
            },
            new Tk2dPlayAnimationWithEvents()
            {
                gameObject = owner,
                clipName = "Idle Slap",
                animationTriggerEvent = FsmEvent.Finished
            },
            ensureGroundedAction
        ];
        FsmState slapState = new FsmState(_silkSpecial.Fsm) { name = "Slap" };
        slapState.actions =
        [
            new ActivateGameObject()
            {
                gameObject = slapOwner,
                activate = new FsmBool() { Value = true },
                recursive = new FsmBool() { Value = false }
            },
            new ActivateGameObjectDelay()
            {
                gameObject = slapOwner,
                activate = new FsmBool() { Value = false },
                delay = new FsmFloat() { Value = 0.2f },
            },
            ensureGroundedAction
        ];
        FsmState slapWaitState = new FsmState(_silkSpecial.Fsm) { name = "Slap Wait" };
        slapWaitState.actions =
        [
            // new AudioPlaySimple()
            // {
            //     gameObject = owner,
            //     oneShotClip = slapAudio,
            //     volume = 1f
            // },
            // new Tk2dWatchAnimationEvents()
            // {
            //     gameObject = owner,
            //     animationCompleteEvent = FsmEvent.Finished
            // },
            ensureGroundedAction
        ];

        var checkInitState = _silkSpecial.FsmStates.First(state => state.Name == "Check Init");
        var specialEndState = _silkSpecial.FsmStates.First(state => state.Name == "Special End");
        var cancelAllState = _silkSpecial.FsmStates.First(state => state.Name == "Cancel All");
        var deactivateDamager = new ActivateGameObject()
        {
            gameObject = slapOwner,
            activate = new FsmBool() { Value = false },
            recursive = new FsmBool() { Value = false }
        };
        checkState.Transitions =
        [
            new FsmTransition()
            {
                FsmEvent = FsmEvent.GetFsmEvent("CANCEL"),
                ToState = checkInitState.Name,
                ToFsmState = checkInitState
            },
            new FsmTransition()
            {
                FsmEvent = FsmEvent.Finished,
                ToState = slapAnticState.Name,
                ToFsmState = slapAnticState
            }
        ];
        slapAnticState.Transitions =
        [
            new FsmTransition()
            {
                FsmEvent = FsmEvent.Finished,
                ToState = slapState.Name,
                ToFsmState = slapState
            }
        ];
        slapState.Transitions =
        [
            new FsmTransition()
            {
                FsmEvent = FsmEvent.Finished,
                ToState = slapWaitState.Name,
                ToFsmState = slapWaitState
            }
        ];
        slapWaitState.Transitions =
        [
            new FsmTransition()
            {
                FsmEvent = FsmEvent.Finished,
                ToState = specialEndState.Name,
                ToFsmState = specialEndState
            }
        ];
        _silkSpecial.Fsm.States = _silkSpecial.Fsm.States.Concat([checkState, slapAnticState, slapState, slapWaitState])
            .ToArray();
        var idleState = _silkSpecial.FsmStates.First(state => state.Name == "Idle");
        idleState.Actions = idleState.Actions.Prepend(new ListenForSlap()
        {
            eventTarget = FsmEventTarget.Self,
            wasPressed = FsmEvent.GetFsmEvent("SLAP"),
            delayBeforeActive = 0
        }).ToArray();
        idleState.Transitions = idleState.Transitions.Append(new FsmTransition()
        {
            FsmEvent = FsmEvent.GetFsmEvent("SLAP"),
            ToState = checkState.Name,
            ToFsmState = checkState
        }).ToArray();
        specialEndState.Actions = specialEndState.Actions.Append(deactivateDamager).ToArray();
        cancelAllState.Actions = cancelAllState.Actions.Append(deactivateDamager).ToArray();
    }
}