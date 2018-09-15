using UnityEngine;
using HutongGames.PlayMaker;

namespace M8.PlayMaker {
    [ActionCategory("Mate Particle System")]
    [HutongGames.PlayMaker.Tooltip("Play the particle")]
    public class ParticleSystemPlay : FSMActionComponentBase<ParticleSystem> {
        public FsmBool withChildren;

        public override void Reset() {
            base.Reset();

            withChildren = false;
        }

        // Code that runs on entering the state.
        public override void OnEnter() {
            base.OnEnter();

            mComp.Play(withChildren.Value);

            Finish();
        }

    }
}
