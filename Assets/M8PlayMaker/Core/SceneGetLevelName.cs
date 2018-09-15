using UnityEngine;
using HutongGames.PlayMaker;

namespace M8.PlayMaker {
    [ActionCategory("Mate Scene")]
    public class SceneGetLevelName : FsmStateAction {
        [RequiredField]
        [UIHint(UIHint.Variable)]
        public FsmString output;
                
        public override void Reset() {
            output = null;
        }
        
        public override void OnEnter() {
            output.Value = Application.loadedLevelName;
            Finish();
        }
    }
}
