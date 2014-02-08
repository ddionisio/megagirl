using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Game")]
public class PlayerSetFace : FsmStateAction {
    public FsmBool left;
    
    // Code that runs on entering the state.
    public override void OnEnter() {
        Player player = Player.instance;
        player.controllerSprite.isLeft = left.Value;
        Finish();
    }
}
