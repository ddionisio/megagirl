using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Game")]
public class PlayerVictory : FsmStateAction {

    // Code that runs on entering the state.
    public override void OnEnter() {
        Player.instance.state = (int)EntityState.Victory;
        Finish();
    }


}
