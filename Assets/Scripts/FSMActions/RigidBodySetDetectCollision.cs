using UnityEngine;
using HutongGames.PlayMaker;
using M8.PlayMaker;

[ActionCategory("Game")]
public class RigidBodySetDetectCollision : FSMActionComponentBase<Rigidbody> {
    public FsmBool detectCollision;

    // Code that runs on entering the state.
    public override void OnEnter() {
        base.OnEnter();
        mComp.detectCollisions = detectCollision.Value;
        Finish();
    }


}
