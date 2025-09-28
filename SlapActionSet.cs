using InControl;

namespace ThisSlaps;

public class SlapActionSet : PlayerActionSet
{
    public readonly PlayerAction slapAction;

    public SlapActionSet()
    {
        slapAction = CreatePlayerAction("Slap");
    }
}