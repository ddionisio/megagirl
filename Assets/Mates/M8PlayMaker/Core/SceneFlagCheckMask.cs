using UnityEngine;
using HutongGames.PlayMaker;

namespace M8.PlayMaker {
    [ActionCategory("Mate Scene")]
    public class SceneFlagCheckMask : FsmStateAction {
        [RequiredField]
        public FsmString name;
        
        public bool global;

        public FsmBool[] bits;
        
        public FsmEvent isTrue;
        public FsmEvent isFalse;
        
        public bool everyFrame;
        
        public override void Reset() {
            name = null;
            bits = null;
            isTrue = null;
            isFalse = null;
            everyFrame = false;
        }
        
        public override void OnEnter() {
            if(SceneState.instance != null) {
                DoCheck();
                if(!everyFrame)
                    Finish();
            }
            else {
                Finish();
            }
        }
        
        public override void OnUpdate() {
            DoCheck();
        }
        
        void DoCheck() {
            int mask = 0;
            for(int i = 0, max = bits.Length; i < max;i++) {
                if(bits[i] != null && bits[i].Value)
                    mask |= (1<<i);
            }

            if(SceneState.instance != null) {
                if(global ? SceneState.instance.CheckGlobalFlagMask(name.Value, mask) : SceneState.instance.CheckFlag(name.Value, mask)) {
                    Fsm.Event(isTrue);
                }
                else {
                    Fsm.Event(isFalse);
                }
            }
            else {
                Finish();
            }
        }
        
        public override string ErrorCheck() {
            if(everyFrame &&
               FsmEvent.IsNullOrEmpty(isTrue) &&
               FsmEvent.IsNullOrEmpty(isFalse))
                return "Action sends no events!";
            return "";
        }
    }
}
