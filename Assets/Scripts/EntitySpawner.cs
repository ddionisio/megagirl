using UnityEngine;
using System.Collections;

public class EntitySpawner : MonoBehaviour {
    public Transform target;
    
    public string group;
    public string entType;
    public int maxCount;
    public float startDelay;
    public float repeatDelay;
    public float restartDelay;

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
        if(activateOnTrigger) {
            StopAllCoroutines();
        }
    }
    
    void Awake() {
        if(!target)
            target = transform;
        
        //just make sure the group name of this object is unique
        if(string.IsNullOrEmpty(group)) {
            PoolController pc = GetComponent<PoolController>();
            group = pc.group;
            
            if(string.IsNullOrEmpty(entType)) {
                entType = pc.types[0].template.name;
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
            for(int i = mCurCount; i < maxCount; i++) {
                Vector3 pos = target.position; pos.z = 0.0f;

                EntityBase ent = EntityBase.Spawn<EntityBase>(group, entType, pos);
                if(ent) {
                    ent.releaseCallback += OnEntityRelease;
                    mCurCount++;
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
    
    void OnEntityRelease(EntityBase ent) {
        ent.releaseCallback -= OnEntityRelease;
        
        if(mCurCount > 0)
            mCurCount--;
    }
    
    void OnDrawGizmos() {
        Color clr = Color.green; clr *= 0.5f;
        Transform t = target ? target : transform;
        M8.DebugUtil.DrawArrow(t.position, t.up, clr);
    }
}
