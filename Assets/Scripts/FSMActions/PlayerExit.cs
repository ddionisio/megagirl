using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Game")]
public class PlayerExit : FsmStateAction {
    
    // Code that runs on entering the state.
    public override void OnEnter() {
        Player.instance.state = (int)EntityState.Exit;
        Finish();
    }
    
    
}
