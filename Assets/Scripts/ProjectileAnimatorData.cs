using UnityEngine;
using System.Collections;

public class ProjectileAnimatorData : Projectile {
    public AnimatorData target;
    public string take;

    private Transform mMover;

    protected override void SpawnStart() {
        base.SpawnStart();

        if(!mMover) {
            RigidBodyMoveToTarget rigidMover = stats.GetComponent<RigidBodyMoveToTarget>();
            if(rigidMover)
                mMover = rigidMover.target;
        }
    }

    public override void SpawnFinish() {
        AnimatorData animDat = target ? target : GetComponent<AnimatorData>();
        animDat.takeCompleteCallback += OnAnimationComplete;

        base.SpawnFinish();
    }

    public override void Release() {
        AnimatorData animDat = target ? target : GetComponent<AnimatorData>();
        animDat.takeCompleteCallback -= OnAnimationComplete;
        animDat.Stop();

        if(mMover) {
            mMover.localPosition = Vector3.zero;
            stats.transform.localPosition = Vector3.zero;
        }

        base.Release();
    }

    protected override void StateChanged() {
        Projectile.State s = (Projectile.State)state;
        AnimatorData animDat;

        base.StateChanged();

        switch(s) {
            case Projectile.State.Active:
                animDat = target ? target : GetComponent<AnimatorData>();
                animDat.Play(take);
                break;
                
            case Projectile.State.Dying:
                animDat = target ? target : GetComponent<AnimatorData>();
                animDat.Stop();
                break;
        }
    }

    protected override void FixedUpdate() {

    }

    void OnAnimationComplete(AnimatorData anim, AMTake take) {
        Release();
    }
}
