using HutongGames.PlayMaker;

namespace ThisSlaps;

public class TriggerSlapDamager : FsmStateAction
{
    public override void OnEnter()
    {
        var sc = SlapController.instance;
        if (sc is null)
        {
            return;
        }
        StartCoroutine(sc.TriggerSlap());
        Finish();
    }
}