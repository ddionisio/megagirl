using UnityEngine;
using HutongGames.PlayMaker;
using M8.PlayMaker;

[ActionCategory("Game")]
public class NGUILabelTypewriterCheckPlaying : FSMActionComponentBase<NGUILabelTypewrite>
{

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
        if(mComp.isPlaying)
            Fsm.Event(isTrue);
        else if(mComp.hasEnded)
            Fsm.Event(isFalse);
    }
    
    // Code that runs every frame.
    public override void OnUpdate() {
        DoCheck();
    }
}
