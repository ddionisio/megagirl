using UnityEngine;
using HutongGames.PlayMaker;
using M8.PlayMaker;

[ActionCategory("Game")]
public class EnemyBossFillHP : FSMActionComponentBase<Enemy> {
    public FsmEvent finishEvent;
    private bool mDoCheck;

    // Code that runs on entering the state.
    public override void OnEnter() {
        base.OnEnter();
        mComp.BossHPFill();
        mDoCheck = true;
    }

    // Code that runs every frame.
    public override void OnUpdate() {
        if(mDoCheck && !HUD.instance.barBoss.isAnimating) {
            mDoCheck = false;
            Fsm.Event(finishEvent);
            Finish();
        }
    }


}
