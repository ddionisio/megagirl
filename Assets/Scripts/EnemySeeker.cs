using UnityEngine;
using System.Collections;

public class EnemySeeker : Enemy {
    public float force = 50.0f;
    public float speedMax = 4.0f;
    public float dirAdjustDelay = 0.15f;

    public float seekRadius;
    public LayerMask seekMask;

    public float updateDelay = 0.2f;

    private float mDirAngle;
    private float mDirAngleDest;
    private Vector3 mDirCur;
    private Vector3 mDirRotAxis;
    private float mDirAngleVel;

    protected override void StateChanged() {
        switch((EntityState)prevState) {
            case EntityState.Normal:
                CancelInvoke("UpdateDelay");
                break;
        }

        base.StateChanged();

        switch((EntityState)state) {
            case EntityState.Normal:
                if(!IsInvoking("UpdateDelay"))
                    InvokeRepeating("UpdateDelay", updateDelay, updateDelay);

                //set starting dir
                Vector3 playerPos = Player.instance.collider.bounds.center;
                Vector3 dPos = playerPos - collider.bounds.center; dPos.z = 0;
                mDirCur = dPos.normalized;
                mDirAngle = mDirAngleDest = 0.0f;
                mDirAngleVel = 0.0f;
                break;
        }
    }

    void FixedUpdate() {
        switch((EntityState)state) {
            case EntityState.Normal:
                Vector3 dir = mDirCur;

                if(mDirAngleDest > 0.0f && mDirAngle != mDirAngleDest) {
                    mDirAngle = Mathf.SmoothDampAngle(mDirAngle, mDirAngleDest, ref mDirAngleVel, dirAdjustDelay, Mathf.Infinity, Time.fixedDeltaTime);

                    dir = Quaternion.AngleAxis(mDirAngle, mDirRotAxis) * dir;

                    if(mDirAngle == mDirAngleDest) {
                        mDirAngle = mDirAngleDest = 0.0f;
                        mDirCur = dir;
                    }
                }

                float spdSqr = rigidbody.velocity.sqrMagnitude;
                if(spdSqr < speedMax * speedMax) {
                    rigidbody.AddForce(dir * force);
                }
                else if(Vector3.Angle(rigidbody.velocity, dir) > 30.0f) {
                    rigidbody.velocity = rigidbody.velocity.normalized * speedMax;
                    rigidbody.AddForce(dir * force);
                }
                break;
        }
    }

    void UpdateDelay() {
        switch((EntityState)state) {
            case EntityState.Normal:
                Vector3 pos = collider.bounds.center;
                Collider[] cols = Physics.OverlapSphere(pos, seekRadius, seekMask);
                if(cols.Length > 0) {
                    float nearestDistSqr = Mathf.Infinity;
                    Vector3 nearestDPos = Vector3.up;
                    for(int i = 0, max = cols.Length; i < max; i++) {
                        Vector3 colPos = cols[i].bounds.center;
                        Vector3 dpos = colPos - pos;
                        float distSqr = dpos.sqrMagnitude;
                        if(distSqr > 0.0f && distSqr < nearestDistSqr) {
                            nearestDPos = dpos;
                            nearestDPos.z = 0;
                        }
                    }

                    mDirAngleDest = Vector3.Angle(mDirCur, nearestDPos);
                    mDirRotAxis = Vector3.Cross(mDirCur, nearestDPos);
                    mDirAngleVel = 0.0f;
                }
                break;
        }
    }

    protected override void OnDrawGizmosSelected() {
        base.OnDrawGizmosSelected();

        if(seekRadius > 0) {
            Gizmos.color = Color.cyan * 0.5f;
            Gizmos.DrawWireSphere(transform.collider.bounds.center, seekRadius);
        }
    }
}
