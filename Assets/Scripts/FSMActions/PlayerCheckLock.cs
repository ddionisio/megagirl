using UnityEngine;
using HutongGames.PlayMaker;
using M8.PlayMaker;

[ActionCategory("Game")]
public class PlayerCheckLock : FsmStateAction {
    public FsmEvent isTrue;
    public FsmEvent isFalse;
    public bool everyFrame = false;

    // Code that runs on entering the state.
    public override void OnEnter() {
        base.OnEnter();
        DoCheck();
        if(!everyFrame) {
            Finish();
        }
    }

    void DoCheck() {
        if(Player.instance.state == (int)EntityState.Lock)
            Fsm.Event(isTrue);
        else
            Fsm.Event(isFalse);
    }

    // Code that runs every frame.
    public override void OnUpdate() {
        DoCheck();
    }
}
