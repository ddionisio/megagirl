using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Game")]
public class PlayerPause : FsmStateAction {
    public FsmBool pause;
    
    // Code that runs on entering the state.
    public override void OnEnter() {
        Player player = Player.instance;
        player.Pause(pause.Value);
        Finish();
    }
}
