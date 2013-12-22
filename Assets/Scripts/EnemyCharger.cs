using UnityEngine;
using System.Collections;

public class EnemyCharger : Enemy {
    public Transform wpHolder;

    public LayerMask angerObstructionMask;
    public float angerRadius = 10.0f;
    public float angerChargeDelay = 1.0f;
    public float angerChargeSpeed = 10.0f;
    public float angerEndDelay = 1.0f; //after this delay, check to see if player is still in vicinity.
    public ParticleSystem angerParticles;

    public float calmForce = 10.0f;
    public float calmMaxSpeed = 5.0f;
    public float calmWPApprox = 0.1f;
    public float calmStandByDelay = 0.5f;
    public ParticleSystem calmParticles;

    public AnimatorData wingsAnimDat;
    public float wingsAngerAnimScale = 3.0f;

    public tk2dSpriteAnimator bodyAnim;
    public string bodyCalmClip = "calm";
    public string bodyAngerClip = "hatred";

    private const string angerRoutine = "DoAnger";
    private const string calmRoutine = "DoCalm";

    private Vector3[] mWPs;
    private int mCurWPInd;
    private Vector3 mLastCalmPos;
    private Vector3 mLastPlayerPos;
    private Vector3 mLastPlayerDir;

    protected override void StateChanged() {
        switch((EntityState)prevState) {
            case EntityState.Normal:
                rigidbody.isKinematic = false;

                angerParticles.Stop();
                angerParticles.Clear();

                calmParticles.Stop();
                calmParticles.Clear();

                StopCoroutine(calmRoutine);
                StopCoroutine(angerRoutine);
                break;
        }

        base.StateChanged();

        switch((EntityState)state) {
            case EntityState.Normal:
                mCurWPInd = 0;

                //start with calm
                StartCoroutine(calmRoutine);
                break;
        }
    }

    protected override void Awake() {
        base.Awake();

        Transform[] wpChildren = new Transform[wpHolder.childCount];
        for(int i = 0; i < wpChildren.Length; i++) {
            wpChildren[i] = wpHolder.GetChild(i);
        }

        System.Array.Sort(wpChildren, delegate(Transform x, Transform y) {
            return x.name.CompareTo(y.name);
       });

        mWPs = new Vector3[wpChildren.Length];
        for(int i = 0; i < wpChildren.Length; i++) {
            Vector3 p = wpChildren[i].position; p.z = 0.0f;
            mWPs[i] = p;
        }
    }

    protected override void OnDrawGizmosSelected() {
        base.OnDrawGizmosSelected();

        if(angerRadius > 0) {
            Color clr = Color.red; clr *= 0.5f;
            Gizmos.color = clr;
            Gizmos.DrawWireSphere(transform.position, angerRadius);
        }

        if(wpHolder) {
            Color clr = Color.green; clr *= 0.5f;
            Gizmos.color = clr;

            Vector3 p = transform.position;
            for(int i = 0; i < wpHolder.childCount; i++) {
                Transform t = wpHolder.GetChild(i);
                Vector3 d = t.position;
                Gizmos.DrawLine(p, d);
                p = d;
            }
        }
    }

    bool isPlayerInRange() {
        mLastPlayerPos = Player.instance.transform.position;

        mLastPlayerDir = mLastPlayerPos - transform.position; mLastPlayerDir.z = 0;
        float dist = mLastPlayerDir.magnitude;

        if(dist > 0.0f && dist <= angerRadius) {
            mLastPlayerDir /= dist;
            return true;
        }

        return false;
    }

    IEnumerator DoAnger() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        WaitForSeconds waitEnd = new WaitForSeconds(angerEndDelay);

        rigidbody.isKinematic = true;

        //get angry
        angerParticles.Play();
        wingsAnimDat.animScale = wingsAngerAnimScale;
        bodyAnim.Play(bodyAngerClip);

        //wait a bit
        yield return new WaitForSeconds(angerChargeDelay);

        bool angry = true;
        while(angry && (EntityState)state == EntityState.Normal) {
            Vector3 sPos = transform.position;
            Vector3 ePos = mLastPlayerPos;

            float curTime = 0.0f;

            //move it
            float dist = (ePos - sPos).magnitude;
            if(dist > 0.0f) {
                float delay = dist/angerChargeSpeed;

                while(true) {
                    curTime += Time.fixedDeltaTime;
                    if(curTime >= delay) {
                        rigidbody.MovePosition(ePos);
                        break;
                    }
                    else {
                        float t = Holoville.HOTween.Core.Easing.Sine.EaseInOut(curTime, 0.0f, 1.0f, delay, 0, 0);
                        rigidbody.MovePosition(Vector3.Lerp(sPos, ePos, t));
                    }

                    yield return wait;
                }
            }

            //wait
            yield return waitEnd;

            angry = isPlayerInRange();
        }

        angerParticles.Stop();

        if((EntityState)state == EntityState.Normal)
            StartCoroutine(calmRoutine);
    }

    IEnumerator DoCalm() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        bool standby = false;
        float standbyTime = 0.0f;

        rigidbody.isKinematic = false;

        //get calm
        calmParticles.Play();
        wingsAnimDat.animScale = 1.0f;
        bodyAnim.Play(bodyCalmClip);

        while((EntityState)state == EntityState.Normal) {
            Vector3 pos = transform.position;

            //check for player
            if(isPlayerInRange()) {
                //check obstruction
                Vector3 playerPos = Player.instance.collider.bounds.center;
                Vector3 dir = playerPos - pos;
                float mag = dir.magnitude;
                if(mag == 0.0f || !Physics.Raycast(pos, dir/mag, mag, angerObstructionMask)) {
                    StartCoroutine(angerRoutine);
                    break;
                }
            }

            //go to dest
            Vector3 toPos = mWPs[mCurWPInd];
            Vector3 dPos = toPos - pos;
            float dist = dPos.magnitude;
            if(dist > 0.0f) {
                Vector3 dir = dPos/dist;

                if(standby) {
                    standbyTime += Time.fixedDeltaTime;
                    standby = standbyTime < calmStandByDelay;

                    //proceed
                    if(!standby) {
                        mCurWPInd++; 
                        if(mCurWPInd == mWPs.Length) mCurWPInd = 0;
                    }
                }
                else {
                    if(dist < calmWPApprox) {
                        standby = true;
                        standbyTime = 0.0f;
                    }
                }

                //speed cap, make sure to add some drag in rigidbody
                if(rigidbody.velocity.sqrMagnitude < calmMaxSpeed*calmMaxSpeed || Vector3.Angle(rigidbody.velocity, dir) > 30)
                    rigidbody.AddForce(dir*calmForce);
            }

            yield return wait;
        }

        calmParticles.Stop();
    }
}
