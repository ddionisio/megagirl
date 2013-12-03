using UnityEngine;
using System.Collections;

public class EnemyTurretLaser : Enemy {
    public enum RotateMode {
        Fixed,
        Scan,
        FollowPlayer
    }

    public RotateMode mode = RotateMode.Fixed;
    public bool alwaysActive; //if false, activate if player is detected

    public Transform dest;

    public Transform point;
    public GameObject pointActive;
    public float pointActiveAnimScale = 1.0f;
    public GameObject pointScan;

    public float degreeMax = 90.0f;

    public float scanStart = 1.0f;
    public float scanAngleDelay;
    public float scanWaitDelay;

    public float followDelay;

    public LayerMask colMask;

    public float deactivateDelay = 2.0f;

    private const string scanRoutine = "DoScan";
    private const string followRoutine = "DoFollow";

    private bool mActive;
    private Damage mDamage;
    private tk2dBaseSprite[]mSprites;
    private float mLastActiveTime;

    public void LaserActive(bool aActive) {
        mActive = aActive;
    }

    protected override void StateChanged() {
        switch((EntityState)prevState) {
            case EntityState.Normal:
                StopCoroutine(scanRoutine);
                StopCoroutine(followRoutine);
                point.gameObject.SetActive(false);
                mActive = false;
                break;
        }

        base.StateChanged();

        switch((EntityState)state) {
            case EntityState.Normal:
                mActive = false;

                point.gameObject.SetActive(true);

                switch(mode) {
                    case RotateMode.FollowPlayer:
                        StartCoroutine(followRoutine);
                        break;

                    case RotateMode.Scan:
                        StartCoroutine(scanRoutine);
                        break;
                }

                if(alwaysActive) {
                    pointScan.SetActive(false);
                    pointActive.SetActive(true);
                }
                else {
                    pointScan.SetActive(true);
                    pointActive.SetActive(false);
                }
                break;
        }
    }

    protected override void Awake() {
        base.Awake();

        mSprites = point.GetComponentsInChildren<tk2dBaseSprite>(true);

        point.gameObject.SetActive(false);
        dest.gameObject.SetActive(false);

        mDamage = GetComponent<Damage>();

        AnimatorData[] animDats = pointActive.GetComponentsInChildren<AnimatorData>(true);
        foreach(AnimatorData animDat in animDats)
            animDat.animScale = pointActiveAnimScale;
    }

    void Update() {
        switch((EntityState)state) {
            case EntityState.Normal:
                float dist = 1000.0f;
                RaycastHit hit;
                bool playerHit = false;

                if(Physics.Raycast(point.position, point.up, out hit, dist, colMask)) {
                    dist = hit.distance;
                    dest.position = hit.point;

                    //do damage
                    if(hit.collider.CompareTag("Player")) {
                        if(mActive) {
                            mDamage.CallDamageTo(hit.collider.gameObject, hit.point, hit.normal);
                        }
                        else {
                            pointScan.SetActive(false);
                            pointActive.SetActive(true);
                        }

                        mLastActiveTime = Time.time;
                        playerHit = true;
                    }
                }
                else {
                    dest.position = point.position;
                }

                if(!alwaysActive && !playerHit && mActive && Time.time - mLastActiveTime > deactivateDelay) {
                    pointScan.SetActive(true);
                    pointActive.SetActive(false);
                    mActive = false;
                }

                const float scaleConv = 4.0f/24.0f;
                for(int i = 0, max = mSprites.Length; i < max; i++) {
                    Vector3 s = mSprites[i].scale;
                    s.y = dist/scaleConv;
                    mSprites[i].scale = s;
                }
                break;
        }
    }

    IEnumerator DoScan() {
        WaitForSeconds idleWait = new WaitForSeconds(scanWaitDelay);
        WaitForFixedUpdate updateWait = new WaitForFixedUpdate();

        Vector3 angles = point.localEulerAngles;

        float s = scanStart*degreeMax*0.5f, e = -scanStart*degreeMax*0.5f;

        angles.z = s;
        point.localEulerAngles = angles;

        while((EntityState)state == EntityState.Normal) {
            yield return idleWait;

            float curT = 0.0f;
            while(curT < scanAngleDelay) {
                yield return updateWait;

                curT = Mathf.Clamp(curT + Time.fixedDeltaTime, 0.0f, scanAngleDelay);
                float rot = Holoville.HOTween.Core.Easing.Sine.EaseIn(curT, s, e - s, scanAngleDelay, 0.0f, 0.0f);
                angles.z = rot;
                point.localEulerAngles = angles;
            }

            float _s = s;
            s = e;
            e = _s;
        }

    }

    IEnumerator DoFollow() {
        WaitForFixedUpdate updateWait = new WaitForFixedUpdate();

        Vector3 vel = Vector3.zero;

        while((EntityState)state == EntityState.Normal) {
            yield return updateWait;

            Vector3 playerPos = Player.instance.collider.bounds.center;
            Vector3 delt = playerPos - transform.position; delt.z = 0;
            delt.Normalize();

            point.up = Vector3.SmoothDamp(point.up, delt, ref vel, followDelay, Mathf.Infinity, Time.fixedDeltaTime);

            Vector2 p2Up = point.up;
            M8.MathUtil.DirCap(transform.up, ref p2Up, degreeMax);
            point.up = p2Up;
        }
    }
}
