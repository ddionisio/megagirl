using UnityEngine;
using System.Collections;

public class EnemyFollowNShoot : Enemy {
    public float rangeInner;
    public float range;
    public float rangeYScale = 1.0f;
    public float followForce = 40.0f;
    public float followStopRange = 1.0f;
    public float followMaxSpeed = 10.0f;

    public GameObject fireSignalGO;
    public float fireWait;
    public float fireDelay;
    public string fireProjType;

    private string fireRoutine = "DoFire";

    private Vector3 mCurFollowLocalPt; //relative to player's
    private bool mFiring;

    protected override void StateChanged() {
        switch((EntityState)prevState) {
            case EntityState.Normal:
                StopCoroutine(fireRoutine);
                mFiring = false;
                fireSignalGO.SetActive(false);
                break;
        }

        base.StateChanged();

        switch((EntityState)state) {
            case EntityState.Normal:
                StartCoroutine(fireRoutine);
                FollowChangePoint();
                break;
        }
    }

    protected override void Awake() {
        base.Awake();

        fireSignalGO.SetActive(false);
    }

    void FixedUpdate() {
        switch((EntityState)state) {
            case EntityState.Normal:
                if(!mFiring) {
                    Vector3 pos = collider.bounds.center;
                    Vector3 dest = Player.instance.transform.localToWorldMatrix.MultiplyPoint(mCurFollowLocalPt); dest.z = pos.z;
                    Vector3 dpos = dest - pos;
                    float distSqr = dpos.sqrMagnitude;
                    if(distSqr > followStopRange*followStopRange 
                       && (rigidbody.velocity.sqrMagnitude < followMaxSpeed*followMaxSpeed || Vector3.Angle(rigidbody.velocity, dpos) > 30.0f)) {
                        Vector3 dir = dpos/distSqr;
                        rigidbody.AddForce(dir*followForce);
                    }
                }
                break;
        }
    }

    IEnumerator DoFire() {
        WaitForSeconds fireW = new WaitForSeconds(fireWait);
        WaitForSeconds fireD = new WaitForSeconds(fireDelay);

        while((EntityState)state == EntityState.Normal) {
            yield return fireW;

            mFiring = true;

            rigidbody.velocity = Vector3.zero;

            fireSignalGO.SetActive(true);
            yield return fireD;
            fireSignalGO.SetActive(false);

            Vector3 pos = fireSignalGO.transform.position; pos.z = 0;
            Vector3 dir = Player.instance.collider.bounds.center - pos; dir.z = 0;
            dir.Normalize();

            Projectile proj = Projectile.Create(projGroup, fireProjType, pos, dir, null);
            proj.applyDirToUp.up = dir;

            mFiring = false;

            FollowChangePoint();
        }
    }

    void FollowChangePoint() {
        float r = Random.Range(rangeInner, range);
        float theta = Random.Range(-180.0f, 180.0f)*Mathf.Deg2Rad;
        Vector2 pt = new Vector2(r*Mathf.Cos(theta), r*Mathf.Sin(theta));
        mCurFollowLocalPt.x = pt.x;
        mCurFollowLocalPt.y = rangeYScale*Mathf.Abs(pt.y);
    }

    protected override void OnDrawGizmosSelected() {
        base.OnDrawGizmosSelected();

        if(rangeInner > 0) {
            Gizmos.color = Color.yellow*0.5f;
            Gizmos.DrawWireSphere(transform.position, rangeInner);
        }

        if(range > 0) {
            Gizmos.color = Color.yellow*0.65f;
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}
