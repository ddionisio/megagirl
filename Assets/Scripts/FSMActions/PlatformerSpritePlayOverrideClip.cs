using UnityEngine;
using HutongGames.PlayMaker;
using M8.PlayMaker;

[ActionCategory("Game")]
public class PlatformerSpritePlayOverrideClip : FSMActionComponentBase<PlatformerSpriteController> {
    public FsmString clip;
    public FsmEvent finishEvent;

    // Code that runs on entering the state.
    public override void OnEnter() {
        base.OnEnter();
        mComp.PlayOverrideClip(clip.Value);
        if(FsmEvent.IsNullOrEmpty(finishEvent))
            Finish();
        else
            mComp.clipFinishCallback += FinishCallback;
    }

    public override void OnExit() {
        if(!FsmEvent.IsNullOrEmpty(finishEvent))
            mComp.clipFinishCallback -= FinishCallback;
    }

    void FinishCallback(PlatformerSpriteController aCtrl, tk2dSpriteAnimationClip aClip) {
        if(aClip.name == clip.Value) {
            Finish();
        }
    }
}
