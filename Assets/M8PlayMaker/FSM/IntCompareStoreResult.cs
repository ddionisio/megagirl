using UnityEngine;
using HutongGames.PlayMaker;

namespace M8.PlayMaker {
    [ActionCategory("Mate FSM")]
    [HutongGames.PlayMaker.Tooltip("Similar to IntCompare, but stores result.")]
    public class IntCompareStoreResult : FsmStateAction {
        [RequiredField]
        public FsmInt
            integer1;
        [RequiredField]
        public FsmInt
            integer2;

        [HutongGames.PlayMaker.Tooltip("Value: If Int1 == Int2 then 0, If Int1 > Int2 then 1, If Int1 < Int2 then -1")]
        [UIHint(UIHint.Variable)]
        public FsmInt result;

        public bool everyFrame;
        
        public override void Reset() {
            integer1 = 0;
            integer2 = 0;
            result = null;
            everyFrame = false;
        }
        
        public override void OnEnter() {
            DoIntCompare();
            
            if(!everyFrame)
                Finish();
        }
        
        public override void OnUpdate() {
            DoIntCompare();
        }
        
        void DoIntCompare() {
            if(integer1.Value == integer2.Value) {
                result.Value = 0;
                return;
            }
            
            if(integer1.Value < integer2.Value) {
                result.Value = -1;
                return;
            }
            
            if(integer1.Value > integer2.Value) {
                result.Value = 1;
            }
        }
        
        public override string ErrorCheck() {
            if(result.IsNone)
                return "Need a variable to store value.";
            return "";
        }
    }
}