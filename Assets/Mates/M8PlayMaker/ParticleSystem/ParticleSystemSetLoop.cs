using UnityEngine;
using HutongGames.PlayMaker;

namespace M8.PlayMaker {
    [ActionCategory("Mate Particle System")]
    [HutongGames.PlayMaker.Tooltip("Set the loop of the particle")]
    public class ParticleSystemSetLoop : FSMActionComponentBase<ParticleSystem> {
        public FsmBool loop;

        public override void Reset() {
            base.Reset();

            loop = false;
        }

        // Code that runs on entering the state.
        public override void OnEnter() {
            base.OnEnter();

            mComp.loop = loop.Value;

            Finish();
        }
    }
}
