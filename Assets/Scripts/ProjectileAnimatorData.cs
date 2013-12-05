using UnityEngine;
using System.Collections;

public class ProjectileAnimatorData : Projectile {
    public AnimatorData target;
    public string take;

    public override void SpawnFinish() {
        AnimatorData animDat = target ? target : GetComponent<AnimatorData>();
        animDat.takeCompleteCallback += OnAnimationComplete;

        base.SpawnFinish();
    }

    public override void Release() {
        AnimatorData animDat = target ? target : GetComponent<AnimatorData>();
        animDat.takeCompleteCallback -= OnAnimationComplete;
        animDat.Stop();

        base.Release();
    }

    protected override void StateChanged() {
        AnimatorData animDat;
        switch((Projectile.State)state) {
            case State.Active:
                animDat = target ? target : GetComponent<AnimatorData>();
                animDat.Play(take);
                break;

            case State.Dying:
                animDat = target ? target : GetComponent<AnimatorData>();
                animDat.Pause();
                break;
        }

        base.StateChanged();
    }

    protected override void FixedUpdate() {

    }

    void OnAnimationComplete(AnimatorData anim, AMTake take) {
        Release();
    }
}
