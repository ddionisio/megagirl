using UnityEngine;
using HutongGames.PlayMaker;
using M8.PlayMaker;

[ActionCategory("Game")]
public class EnemyGetVisibleGO : FSMActionComponentBase<Enemy> {
    [UIHint(UIHint.Variable)]
    public FsmGameObject output;
    // Code that runs on entering the state.
    public override void OnEnter() {
        base.OnEnter();
        output = mComp.visibleGO;
        Finish();
    }


}
