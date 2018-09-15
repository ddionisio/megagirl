using UnityEngine;
using HutongGames.PlayMaker;

namespace M8.PlayMaker {
    [ActionCategory("Mate Audio")]
    public class MusicStop : FsmStateAction {
        public FsmBool fade;

        public override void Reset() {
            fade = true;
        }
        
        // Code that runs on entering the state.
        public override void OnEnter() {
            MusicManager.instance.Stop(fade.Value);
            Finish();
        }
    }
}