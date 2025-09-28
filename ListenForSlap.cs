using HutongGames.PlayMaker;
using InControl;
using UnityEngine;

namespace ThisSlaps;

[ActionCategory("Controls")]
public class ListenForSlap : FsmStateAction
{
    public FsmEventTarget eventTarget;

    public FsmEvent wasPressed;

    public FsmEvent wasReleased;

    public FsmEvent isPressed;

    public FsmEvent isNotPressed;

    public FsmFloat delayBeforeActive;

    private GameManager gm;

    private SlapActionSet slapActions;

    private float timer;

    public override void Reset()
    {
        eventTarget = null;
    }

    public override void OnEnter()
    {
        gm = GameManager.instance;
        slapActions = ThisSlapsPlugin.inputActions;
        timer = delayBeforeActive.Value;
    }

    public override void OnUpdate()
    {
        if (gm.isPaused)
        {
            return;
        }
        if (timer > 0f)
        {
            timer -= Time.deltaTime;
            return;
        }
        if (slapActions.slapAction.WasPressed)
        {
            ThisSlapsPlugin.Log.LogInfo("Send SLAP");
            base.Fsm.Event(wasPressed);
        }
        if (slapActions.slapAction.WasReleased)
        {
            base.Fsm.Event(wasReleased);
        }
        if (slapActions.slapAction.IsPressed)
        {
            base.Fsm.Event(isPressed);
        }
        if (!slapActions.slapAction.IsPressed)
        {
            base.Fsm.Event(isNotPressed);
        }
    }
}