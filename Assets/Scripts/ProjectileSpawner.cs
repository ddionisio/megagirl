using UnityEngine;
using System.Collections;

/// <summary>
/// Uses the up vector as dir
/// </summary>
public class ProjectileSpawner : MonoBehaviour {
    public string projGroup = Enemy.projGroup;
    public string projType;
    public int maxCount;
    public float startDelay;
    public float repeatDelay;
    public float restartDelay;
    public float angleRandRange;

    public bool seekPlayer;

    public bool activateOnTrigger;

    private int mCurCount;
    private bool mStarted;

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
        StopAllCoroutines();
    }

    void Awake() {
        //just make sure the group name of this object is unique
        if(string.IsNullOrEmpty(projGroup)) {
            PoolController pc = GetComponent<PoolController>();
            projGroup = pc.group;

            if(string.IsNullOrEmpty(projType)) {
                projType = pc.types[0].template.name;
            }
        }
    }

	// Use this for initialization
	void Start () {
	    mStarted = true;
        OnEnable();
	}

    IEnumerator DoThings() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        WaitForSeconds waitDelay = new WaitForSeconds(repeatDelay);
        WaitForSeconds waitRestartDelay = new WaitForSeconds(restartDelay);

        mCurCount = 0;

        yield return new WaitForSeconds(startDelay);

        while(true) {
            for(int i = 0; i < maxCount; i++) {
                Transform seek = seekPlayer ? Player.instance.transform : null;

                Vector3 pos = transform.position; pos.z = 0.0f;

                Vector3 dir = transform.up;
                if(angleRandRange > 0.0f) {
                    dir = Quaternion.AngleAxis(Random.Range(-angleRandRange, angleRandRange), Vector3.forward) * dir;
                }

                Projectile proj = Projectile.Create(projGroup, projType, pos, dir, seek);
                proj.releaseCallback += OnProjRelease;
                mCurCount++;
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
        M8.DebugUtil.DrawArrow(transform.position, transform.up, clr);
    }
}
