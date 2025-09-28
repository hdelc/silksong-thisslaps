using HutongGames.PlayMaker;

namespace ThisSlaps;

public class CheckCanSlap : FsmStateAction
{
    public FsmEvent? isTrue;

    public FsmEvent? isFalse;

    public override void OnEnter()
    {
        if (SlapController.CanSlap())
        {
            base.Fsm.Event(isTrue);
        }
        else
        {
            base.Fsm.Event(isFalse);
        }
    }
}