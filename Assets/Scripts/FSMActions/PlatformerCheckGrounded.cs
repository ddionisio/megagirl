using UnityEngine;
using HutongGames.PlayMaker;
using M8.PlayMaker;

[ActionCategory("Game")]
public class PlatformerCheckGrounded : FSMActionComponentBase<PlatformerController> {
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
        if(mComp.isGrounded)
            Fsm.Event(isTrue);
        else
            Fsm.Event(isFalse);
    }

    // Code that runs every frame.
    public override void OnUpdate() {
        DoCheck();
    }


}
