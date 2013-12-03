using UnityEngine;
using System.Collections;

public class TimeWarpField : MonoBehaviour {
    public const int maxCount = 8;

    public float radius = 1.0f;
    public LayerMask masks;
    public float scale = 0.3f;

    private bool mStarted;
    private bool mUpdateActive;

    private int mCount;
    private TimeWarp[] mItems;
    private TimeWarp[] mColItems;

    void OnEnable() {
        if(mStarted && !mUpdateActive) {
            StartCoroutine(DoUpdate());
        }
    }

    void OnDisable() {
        mUpdateActive = false;

        if(mStarted) {
            for(int i = 0; i < mCount; i++) {
                if(mItems[i]) {
                    mItems[i].Restore();
                    mItems[i] = null;
                }
            }
            mCount = 0;
        }
    }

    void Awake() {
        mItems = new TimeWarp[maxCount];
        mColItems = new TimeWarp[maxCount];
    }

    void Start() {
        mStarted = true;
        OnEnable();
    }

    IEnumerator DoUpdate() {
        mUpdateActive = true;

        WaitForSeconds wait = new WaitForSeconds(0.2f);

        while(mUpdateActive) {
            yield return wait;

            //
            Collider[] cols = Physics.OverlapSphere(transform.position, radius, masks);

            int colCount = 0;
            for(int i = 0; i < cols.Length && colCount < maxCount; i++) {
                TimeWarp warp = cols[i].GetComponent<TimeWarp>();
                if(warp) {
                    mColItems[colCount] = warp;
                    colCount++;
                }
            }

            //check to see if colliders are already in data
            //if a collider in our data is not in the list, remove it
            for(int i = 0; i < mCount; i++) {
                TimeWarp item = mItems[i];

                bool doRemove = true;

                if(item != null && item.target.gameObject.activeInHierarchy) {
                    //check if it's in collisions
                    for(int j = 0; j < colCount; j++) {
                        if(item == mColItems[j]) {
                            //remove from cols, already in our items
                            if(colCount > 1) {
                                mColItems[j] = mColItems[colCount - 1];
                            }
                            colCount--;
                            doRemove = false;
                            break;
                        }
                    }
                }

                if(doRemove) {
                    //Debug.Log("remove: "+item.col.name);
                    if(item)
                        item.Restore();

                    if(mCount > 1) {
                        mItems[i] = mItems[mCount - 1];
                        mItems[mCount - 1] = null;
                    }

                    mCount--;
                    i--;
                }
            }

            if(colCount > 0) {
                //add new items
                for(int i = 0; i < colCount && mCount < mItems.Length; i++) {
                    if(mColItems[i]) {
                        //Debug.Log("add: " + cols[i].name);
                        mItems[mCount] = mColItems[i];
                        mItems[mCount].SetScale(scale);
                        mCount++;
                    }
                }
            }
        }
    }

    void OnDrawGizmos() {
        if(radius > 0.0f) {
            Gizmos.color = Color.magenta * 0.5f;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
