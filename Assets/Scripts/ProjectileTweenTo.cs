using UnityEngine;
using System.Collections;

public class ProjectileTweenTo : Projectile {

    private bool mMoveActive;
    private Vector3 mMoveStart;
    private Vector3 mMoveEnd;
    private float mMoveCurTime;
    private float mMoveDelay;

    public bool isMoveActive { get { return mMoveActive; } }

    /// <summary>
    /// Call this to make the projectile move
    /// </summary>
    public void Move(Vector3 dest, float speed) {
        if(speed > 0) {
            mMoveActive = true;
            mMoveEnd = dest;
            mMoveStart = transform.position;
            mMoveDelay = (mMoveStart - mMoveEnd).magnitude/speed;
            mMoveCurTime = 0.0f;
        }
        else
            mMoveActive = false;
    }

    protected override void StateChanged() {
        switch((State)state) {
            case State.Dying:
            case State.Invalid:
                mMoveActive = false;
                break;
        }

        base.StateChanged();
    }

    protected override void FixedUpdate() {
        if(isAlive) {
            if(mMoveActive) {
                mMoveCurTime += Time.fixedDeltaTime;
                if(mMoveCurTime < mMoveDelay) {
                    float t = Holoville.HOTween.Core.Easing.Sine.EaseInOut(mMoveCurTime, 0.0f, 1.0f, mMoveDelay, 0, 0);
                    DoSimpleMove(Vector3.Lerp(mMoveStart, mMoveEnd, t) - transform.position);
                }
                else {
                    DoSimpleMove(mMoveEnd - transform.position);
                    mMoveActive = false;
                }
            }
            else {
                SimpleCheckContain();
            }
        }
    }
}
