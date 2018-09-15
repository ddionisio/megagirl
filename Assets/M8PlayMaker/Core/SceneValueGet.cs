using UnityEngine;
using HutongGames.PlayMaker;

namespace M8.PlayMaker {
    [ActionCategory("Mate Scene")]
    public class SceneValueGet : FsmStateAction {
        [RequiredField]
        public FsmString name;

        public bool global;

        [RequiredField]
        [UIHint(UIHint.Variable)]
        public FsmVar toValue;

        public bool everyFrame;

        public override void Reset() {
            name = null;
            global = false;
            toValue = null;
            everyFrame = false;
        }

        public override void OnEnter() {
            if(SceneState.instance != null) {
                if(toValue.Type == VariableType.Int)
                    toValue.SetValue(global ? SceneState.instance.GetGlobalValue(name.Value) : SceneState.instance.GetValue(name.Value));
                else if(toValue.Type == VariableType.Float)
                    toValue.SetValue(global ? SceneState.instance.GetGlobalValueFloat(name.Value) : SceneState.instance.GetValueFloat(name.Value));
            }

            if(!everyFrame)
                Finish();
        }

        public override void OnUpdate() {
            if(toValue.Type == VariableType.Int)
                toValue.SetValue(global ? SceneState.instance.GetGlobalValue(name.Value) : SceneState.instance.GetValue(name.Value));
            else if(toValue.Type == VariableType.Float)
                toValue.SetValue(global ? SceneState.instance.GetGlobalValueFloat(name.Value) : SceneState.instance.GetValueFloat(name.Value));
        }
    }
}
