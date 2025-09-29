using System;
using System.Collections;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using TeamCherry.SharedUtils;
using UnityEngine;
using UnityEngine.Events;

namespace ThisSlaps;

[RequireComponent(typeof(HeroController))]
internal class SlapController : MonoBehaviour
{
    public static SlapController? instance;
    private HeroController _heroController = null!;
    private PlayMakerFSM _silkSpecial = null!;
    private tk2dSpriteAnimator _animator = null!;
    private tk2dSpriteAnimationClip _slapAnimation = null!;
    private GameObject slapDamager = null!;
    private AudioClip _slapAudio = null!;

    private bool _isSlapping = false;

    private void Awake()
    {
        instance = this;

        GetComponents();
        GetAssetRefs();
    }

    private void Start()
    {
        slapDamager = new GameObject("Slap Damager");
        var collider = slapDamager.AddComponent<CircleCollider2D>();
        collider.radius = 0.8f;
        collider.isTrigger = true;
        collider.includeLayers = LayerMask.GetMask("Enemies", "Projectiles");
        // collider.includeLayers = new LayerMask() {m_Mask = -2042750632}; // TODO: Fix the mask
        // collider.enabled = false;
        
        // collider.excludeLayers = LayerMask.GetMask("Player");
        slapDamager.transform.parent = gameObject.transform;
        slapDamager.transform.localPosition = new Vector3(-1, 0);
        var damage = slapDamager.AddComponent<DamageEnemies>();
        InitializeDamage(damage);
        damage.damageDealt = 1;
        damage.WillDamageEnemyOptions += EnemyDamageLog;
        damage.WillDamageEnemy += PlaySlapAudio;
        slapDamager.SetActive(false);
        PatchSilkFSM();
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

    public IEnumerator TriggerSlap()
    {
        var colliders = slapDamager.GetComponents<Collider2D>();
        foreach (var collider in colliders)
        {
            collider.enabled = true;
        }
        yield return new WaitForSecondsRealtime(0.1f);
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }
    }
    

    private void EnemyDamageLog(HealthManager health, HitInstance hit)
    {
        ThisSlapsPlugin.Log.LogInfo($"Should damage enemy: {health.name}");
    }

    private void GetComponents()
    {
        _heroController = gameObject.GetComponent<HeroController>();
        _animator = _heroController.animCtrl.animator;
        _silkSpecial = _heroController.silkSpecialFSM;
    }

    private void GetAssetRefs()
    {
        _slapAnimation = _heroController.animCtrl.GetClip("Idle Slap");
        _slapAudio = AssetManager.Get<AudioClip>("hornet_slap_B_2");
    }

    public void DoSlap()
    {
        if (CanSlap())
        {
            DoSlapForce();
        }
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
    
    private void DoSlapForce()
    {
        _isSlapping = true;
        // AudioSource playerSource = hc.audioSource;
        // AudioEvent audioEvent = default(AudioEvent);
        // audioEvent.Clip = _slapAudio;
        _heroController.StopAnimationControl();
        _heroController.RelinquishControl();
        //hc.animCtrl.PlayClipForced("Idle Slap");
        var animator = _heroController.animCtrl.animator;
        animator.Play(_slapAnimation);
        animator.AnimationCompleted = ReturnAnimationControlHandler;
        // _heroController.audioSource.PlayOneShot(_slapAudio);
        // audioEvent.SpawnAndPlayOneShot(hc.transform.position);
    }
    
    private void ReturnAnimationControlHandler(tk2dSpriteAnimator animator, tk2dSpriteAnimationClip clip)
    {
        _isSlapping = false;
        _heroController.StartAnimationControl();
        _heroController.RegainControl();
        // _heroController.animCtrl.animator.AnimationCompleted -= ReturnAnimationControlHandler;
    }

    private void PatchSilkFSM()
    {
        var slapAudio = AssetManager.Get<AudioClip>("hornet_slap_B_2");
        var owner = new FsmOwnerDefault() { gameObject = gameObject, ownerOption = OwnerDefaultOption.SpecifyGameObject };
        var slapOwner = new FsmOwnerDefault()
            { GameObject = slapDamager, OwnerOption = OwnerDefaultOption.SpecifyGameObject };
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
            // new CallMethodProper()
            // {
            //     gameObject = owner,
            //     behaviour = "HeroController",
            //     methodName = "CanCast",
            //     storeResult = new FsmVar(_silkSpecial.fsm.GetFsmBool("Can Do")),
            // },
            // new BoolTest()
            // {
            //     boolVariable = _silkSpecial.Fsm.GetFsmBool("Can Do"),
            //     isFalse = FsmEvent.GetFsmEvent("CANCEL")
            // },
            // new CallMethodProper()
            // {
            //     gameObject = owner,
            //     behaviour = "HeroController",
            //     methodName = "IsHardLanding",
            //     storeResult = new FsmVar(_silkSpecial.Fsm.GetFsmBool("Hard Landing")),
            // },
            // new BoolTest()
            // {
            //     boolVariable = _silkSpecial.Fsm.GetFsmBool("Hard Landing"),
            //     isTrue = FsmEvent.GetFsmEvent("CANCEL")
            // },
            // new CallMethodProper()
            // {
            //     behaviour = "HeroController",
            //     methodName = "GetState",
            //     parameters = [new FsmVar(typeof(string)) { stringValue = "onGround" }],
            //     storeResult = new FsmVar(_silkSpecial.Fsm.GetFsmBool("On Ground")),
            // },
            // new BoolTest()
            // {
            //     boolVariable = _silkSpecial.Fsm.GetFsmBool("On Ground"),
            //     isFalse = FsmEvent.GetFsmEvent("CANCEL"),
            //     isTrue = FsmEvent.Finished
            // }
            new CheckCanSlap()
            {
                isTrue = FsmEvent.Finished,
                isFalse = FsmEvent.GetFsmEvent("CANCEL")
            }
        ];

        FsmState slapAnticState = new FsmState(_silkSpecial.Fsm) { name = "Slap Antic", isSequence = true };
        slapAnticState.actions =
        [
            // new HeroControllerMethods()
            // {
            //     method = HeroControllerMethods.Method.RelinquishControl,
            // },
            // new HeroControllerMethods()
            // {
            //     method = HeroControllerMethods.Method.StopAnimationControl
            // },
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
            // new TriggerSlapDamager(),
            // new EnableColliders()
            // {
            //     gameObject = slapOwner,
            //     enabled = true
            // },
            // new EnableColliders()
            // {
            //     gameObject = slapOwner,
            //     enabled = false,
            //     delay = 0.1f
            // },
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
        // var deactivateDamager = new EnableColliders()
        // {
        //     gameObject = slapOwner,
        //     enabled = false
        // };
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
        _silkSpecial.Fsm.States = _silkSpecial.Fsm.States.Concat([checkState, slapAnticState, slapState, slapWaitState]).ToArray();
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