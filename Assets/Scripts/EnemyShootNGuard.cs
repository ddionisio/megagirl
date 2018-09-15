using UnityEngine;
using System.Collections;

public class EnemyShootNGuard : Enemy {
    public string projType = projCommonType;

    public GameObject shieldGO;
    public GameObject gunGO;

    public float guardDelay = 3.0f;

    public AnimatorData shootAnim;
    public Transform shootPt;

    public int shootCount = 3;
    public float shootReadyDelay = 0.5f;
    public float shootRepeatDelay = 0.5f;

    public float faceDelay = 1.0f;

    public float jumpDistX = 8.0f;

    public SoundPlayer shootSfx;

    private const string ActiveRoutine = "DoStuff";

    private float mLastFaceTime;

    private GameObject[] mPlayers;

    protected override void StateChanged() {
        switch((EntityState)prevState) {
            case EntityState.Normal:
                StopCoroutine(ActiveRoutine);

                shieldGO.SetActive(true);
                gunGO.SetActive(false);
                break;
        }
        
        base.StateChanged();
        
        switch((EntityState)state) {
            case EntityState.Normal:
                bodyCtrl.moveEnabled = true;
                StartCoroutine(ActiveRoutine);
                mLastFaceTime = 0.0f;
                break;
        }
    }

    protected override void Awake() {
        base.Awake();

        mPlayers = GameObject.FindGameObjectsWithTag("Player");

        shieldGO.SetActive(true);
        gunGO.SetActive(false);

        bodyCtrl.landCallback += OnLanded;
    }

    Transform NearestPlayer(out float dirX) {
        float nearDistX = Mathf.Infinity;
        Transform nearT = null;
        
        float x = transform.position.x;
        for(int i = 0, max = mPlayers.Length; i < max; i++) {
            if(mPlayers[i] && mPlayers[i].activeSelf) {
                Transform t = mPlayers[i].transform;
                float distX = Mathf.Abs(t.position.x - x);
                if(distX < nearDistX) {
                    nearT = t;
                    nearDistX = distX;
                }
            }
        }

        dirX = nearT ? Mathf.Sign(nearT.position.x - transform.position.x) : 0.0f;
        
        return nearT;
    }

    void FixedUpdate() {
        switch((EntityState)state) {
            case EntityState.Normal:
                float dirX;
                Transform nearest = NearestPlayer(out dirX);
                if(nearest) {
                    if(Time.fixedTime - mLastFaceTime > faceDelay) {
                        bodySpriteCtrl.isLeft = dirX < 0.0f;
                        mLastFaceTime = Time.fixedTime;
                    }

                    if(bodyCtrl.isGrounded && Mathf.Abs(nearest.position.x - transform.position.x) <= jumpDistX) {
                        if(nearest.GetComponent<Collider>() && nearest.GetComponent<Collider>().bounds.min.y > GetComponent<Collider>().bounds.center.y) {
                            Jump(0.0f);
                            Jump(1.0f);
                        }
                    }
                }
                break;
        }
    }

    void OnLanded(PlatformerController ctrl) {
        Vector2 p = transform.position;
        PoolController.Spawn("fxp", "landdust", "landdust", null, p);
    }

    IEnumerator DoStuff() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        WaitForSeconds shootReadyWait = new WaitForSeconds(shootReadyDelay);
        WaitForSeconds shootRepeatWait = new WaitForSeconds(shootRepeatDelay);
        WaitForSeconds guardWait = new WaitForSeconds(guardDelay);

        while((EntityState)state == EntityState.Normal) {
            shieldGO.SetActive(true);
            gunGO.SetActive(false);

            yield return guardWait;

            shieldGO.SetActive(false);
            gunGO.SetActive(true);

            //wait till we are on ground
            while(!bodyCtrl.isGrounded)
                yield return wait;

            yield return shootReadyWait;

            for(int i = 0; i < shootCount; i++) {
                Vector3 pos = shootPt.position; pos.z = 0.0f;
                Vector3 dir = new Vector3(bodySpriteCtrl.isLeft ? -1.0f : 1.0f, 0.0f, 0.0f);

                shootAnim.Play("shoot");

                Projectile.Create(projGroup, projType, pos, dir, null);

                if(shootSfx)
                    shootSfx.Play();

                yield return shootRepeatWait;
            }
        }
    }
}
