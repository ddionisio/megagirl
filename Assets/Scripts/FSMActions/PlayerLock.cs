using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Game")]
public class PlayerLock : FsmStateAction {
    public FsmBool val;

    // Code that runs on entering the state.
    public override void OnEnter() {
        Player.instance.state = (int)(val.Value ? EntityState.Lock : EntityState.Normal);
        Finish();
    }


}
