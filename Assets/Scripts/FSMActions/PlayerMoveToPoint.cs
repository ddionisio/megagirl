using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Game")]
public class PlayerMoveToPoint : FsmStateAction {
    [UIHint(UIHint.Variable)]
    public FsmGameObject target;

    public FsmEvent finishEvent;

    // Code that runs on entering the state.
    public override void OnEnter() {
        Player player = Player.instance;
        Transform t = target.Value.transform;
        float deltaX = t.position.x - player.transform.position.x;
        player.controller.moveSide = Mathf.Sign(deltaX);
    }

    // Code that runs every frame.
    public override void OnUpdate() {
        Player player = Player.instance;
        Transform t = target.Value.transform;

        float deltaX = t.position.x - player.transform.position.x;

        player.controller.moveSideLock = true;

        if(Mathf.Abs(deltaX) < 0.1f || Mathf.Sign(deltaX) != player.controller.moveSide) {
            player.controller.moveSide = 0.0f;

            Fsm.Event(finishEvent);
            Finish();
        }
    }

    public override void OnExit() {
    }
}
