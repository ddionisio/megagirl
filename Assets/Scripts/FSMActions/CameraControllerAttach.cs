using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Game")]
public class CameraControllerAttach : FsmStateAction {
    [UIHint(UIHint.Variable)]
    public FsmGameObject go;
    
    // Code that runs on entering the state.
    public override void OnEnter() {
        CameraController.instance.attach = go.IsNone || go.Value == null ? null : go.Value.transform;
        if(CameraController.instance.attach) {
        }
        else {
            CameraController.instance.mode = CameraController.Mode.Lock;
        }

        Finish();
    }
    
    
}
