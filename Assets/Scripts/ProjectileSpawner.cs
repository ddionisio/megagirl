using UnityEngine;
using System.Collections;

/// <summary>
/// Uses the up vector as dir
/// </summary>
public class ProjectileSpawner : MonoBehaviour {
    public Transform target;

    public float angleOfs;

    public string projGroup = Enemy.projGroup;
    public string projType;
    public int maxCount;
    public float startDelay;
    public float repeatDelay;
    public float restartDelay;
    public float angleRandRange;

    public bool seekPlayer;
    public float seekActiveAngleLimit = 360.0f; //limit of start dir against dir towards seek

    public bool activateOnTrigger;

    public ParticleSystem fireParticle; //play upon firing

    public bool projSpriteVflip; //flip vertical based on dirX

    private int mCurCount;
    private bool mStarted;

    private GameObject[] mPlayers;

    void OnEnable() {
        if(!activateOnTrigger && mStarted) {
            StartCoroutine(DoThings());
        }
    }

    void OnDisable() {
        StopAllCoroutines();
    }

    void OnTriggerEnter(Collider col) {
        if(activateOnTrigger) {
            StartCoroutine(DoThings());
        }
    }

    void OnTriggerExit(Collider col) {
        if(activateOnTrigger) {
            StopAllCoroutines();
        }
    }

    void Awake() {
        if(!target)
            target = transform;

        //just make sure the group name of this object is unique
        if(string.IsNullOrEmpty(projGroup)) {
            PoolController pc = GetComponent<PoolController>();
            projGroup = pc.group;

            if(string.IsNullOrEmpty(projType)) {
                projType = pc.types[0].template.name;
            }
        }

        mPlayers = GameObject.FindGameObjectsWithTag("Player");
    }

	// Use this for initialization
	void Start () {
	    mStarted = true;
        OnEnable();
	}

    Transform NearestPlayer() {
        float nearDistX = Mathf.Infinity;
        Transform nearT = null;
        
        float x = target.position.x;
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

        return nearT;
    }

    IEnumerator DoThings() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        WaitForSeconds waitDelay = new WaitForSeconds(repeatDelay);
        WaitForSeconds waitRestartDelay = new WaitForSeconds(restartDelay);

        //mCurCount = 0;

        //resuming from current count
        if(mCurCount > 0) {
            if(mCurCount >= maxCount) {
                while(mCurCount > 0) {
                    yield return wait;
                }
                
                yield return waitRestartDelay;
            }
            else {
                yield return waitDelay;
            }
        }
        else {
            yield return new WaitForSeconds(startDelay);
        }

        while(true) {
            Vector3 up = target.up;
            up = Quaternion.AngleAxis(angleOfs, Vector3.forward) * up;

            for(int i = mCurCount; i < maxCount; i++) {
                Transform seek = seekPlayer ? NearestPlayer() : null;

                Vector3 pos = target.position; pos.z = 0.0f;

                Vector3 dir = up;

                bool doIt = seek && seekActiveAngleLimit < 360.0f ? Vector3.Angle(dir, seek.position - pos) <= seekActiveAngleLimit  : true;

                if(doIt) {
                    if(angleRandRange > 0.0f) {
                        dir = Quaternion.AngleAxis(Random.Range(-angleRandRange, angleRandRange), Vector3.forward) * dir;
                    }

                    Projectile proj = Projectile.Create(projGroup, projType, pos, dir, seek);
                    if(proj) {
                        if(projSpriteVflip) {
                            tk2dBaseSprite spr = proj.GetComponentInChildren<tk2dBaseSprite>();
                            spr.FlipY = dir.x > 0.0f;
                        }

                        proj.releaseCallback += OnProjRelease;
                        mCurCount++;

                        if(fireParticle)
                            fireParticle.Play();
                    }
                }

                yield return waitDelay;
            }

            //wait until count is back to 0
            while(mCurCount > 0) {
                yield return wait;
            }

            yield return waitRestartDelay;
        }
    }

    void OnProjRelease(EntityBase ent) {
        ent.releaseCallback -= OnProjRelease;

        if(mCurCount > 0)
            mCurCount--;
    }
	
    void OnDrawGizmos() {
        Color clr = Color.green; clr *= 0.5f;
        Transform t = target ? target : transform;
        M8.DebugUtil.DrawArrow(t.position, Quaternion.AngleAxis(angleOfs, Vector3.forward)*t.up, clr);
    }
}
