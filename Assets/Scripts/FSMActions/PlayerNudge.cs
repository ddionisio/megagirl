using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Game")]
public class PlayerNudge : FsmStateAction {
    public Vector3 amount;

    // Code that runs on entering the state.
    public override void OnEnter() {
        Player player = Player.instance;
        Vector3 pos = player.transform.position;
        player.transform.position = pos + amount;
        Finish();
    }
}
