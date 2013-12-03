using UnityEngine;
using HutongGames.PlayMaker;
using M8.PlayMaker;

[ActionCategory("Game")]
public class EnemyBossEnableHP : FSMActionComponentBase<Enemy> {

    // Code that runs on entering the state.
    public override void OnEnter() {
        base.OnEnter();
        mComp.BossHPEnable();
        Finish();
    }


}
