using UnityEngine;
using System.Collections;
using HutongGames.PlayMaker;

namespace M8.PlayMaker {
    [ActionCategory("Mate Audio")]
    [HutongGames.PlayMaker.Tooltip("Play SoundPlayer.")]
    public class SoundStop : FSMActionComponentBase<SoundPlayer> {

        public override void OnEnter() {
            base.OnEnter();
            
            mComp.Stop();
            Finish();
        }
    }
}