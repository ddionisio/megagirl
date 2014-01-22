using UnityEngine;
using System.Collections;

public class PlatformerJumpSettingTrigger : MonoBehaviour {
    [System.Serializable]
    public class Info {
        public bool jumpReleaseClearVelocity;
        public float jumpDelay;
    }

    public string spawnGrp;
    public string spawnType;

    public const int maxCols = 8;

    public Info info;
    
    public string[] tags;

    private struct Data {
        private PlatformerController mCtrl;

        private bool mLastJumpReleaseClearVelocity;
        private float mLastJumpDelay;

        public PlatformerController ctrl { get { return mCtrl; } }
        public Collider collider { get { return mCtrl ? mCtrl.collider : null; } }
        public bool lastJumpReleaseClearVelocity { get { return mLastJumpReleaseClearVelocity; } }
        public float lastJumpDelay { get { return mLastJumpDelay; } }

        public void Apply(Collider triggerCol, Info inf, PlatformerController aCtrl) {
            Revert(triggerCol);

            mCtrl = aCtrl;

            bool isNew = true;
            foreach(Collider col in mCtrl.triggers) {
                PlatformerJumpSettingTrigger pt = col.GetComponent<PlatformerJumpSettingTrigger>();
                if(pt) {
                    foreach(PlatformerJumpSettingTrigger.Data dat in pt.mCtrls) {
                        if(dat.collider == aCtrl.collider) {
                            mLastJumpReleaseClearVelocity = dat.mLastJumpReleaseClearVelocity;
                            mLastJumpDelay = dat.mLastJumpDelay;
                            isNew = false;
                            break;
                        }
                    }

                    if(!isNew)
                        break;
                }
            }

            if(isNew) {
                mLastJumpReleaseClearVelocity = mCtrl.jumpReleaseClearVelocity;
                mLastJumpDelay = mCtrl.jumpDelay;
            }

            mCtrl.jumpReleaseClearVelocity = inf.jumpReleaseClearVelocity;
            mCtrl.jumpDelay = inf.jumpDelay;

            mCtrl.triggers.Add(triggerCol);
        }

        public void Refresh(Collider triggerCol, Info inf) {
            if(mCtrl) {
                mCtrl.jumpReleaseClearVelocity = inf.jumpReleaseClearVelocity;
                mCtrl.jumpDelay = inf.jumpDelay;
            }
        }

        public void Revert(Collider triggerCol) {
            if(mCtrl) {
                mCtrl.jumpReleaseClearVelocity = mLastJumpReleaseClearVelocity;
                mCtrl.jumpDelay = mLastJumpDelay;
                mCtrl.triggers.Remove(triggerCol);
                mCtrl = null;
            }
        }
    }
    
    private Data[] mCtrls = new Data[maxCols];
    private int mCurColCount;
    
    void OnDisable() {
        for(int i = 0; i < mCurColCount; i++) {
            mCtrls[i].Revert(collider);
        }
        
        mCurColCount = 0;
    }
    
    void OnTriggerEnter(Collider col) {
        PlatformerController ctrl = col.GetComponent<PlatformerController>();
        if(ctrl && M8.ArrayUtil.Contains(tags, col.tag)) {
            if(mCurColCount < maxCols) {
                mCtrls[mCurColCount].Apply(collider, info, ctrl);

                if(mCurColCount == 0 && !string.IsNullOrEmpty(spawnGrp) && !string.IsNullOrEmpty(spawnType)) {
                    PoolController.Spawn(spawnGrp, spawnType, spawnType, null, col.transform.position, Quaternion.identity);
                }

                mCurColCount++;
            }
            else {
                Debug.LogWarning("exceeded count");
            }
        }
    }
    
    void OnTriggerStay(Collider col) {
        int ind = -1;
        for(int i = 0; i < mCurColCount; i++) {
            if(mCtrls[i].collider == col) {
                mCtrls[i].Refresh(collider, info);
                ind = i;
                break;
            }
        }
        
        if(ind == -1 && M8.ArrayUtil.Contains(tags, col.tag)) {
            OnTriggerEnter(col);
        }
    }
    
    void OnTriggerExit(Collider col) {
        for(int i = 0; i < mCurColCount; i++) {
            if(mCtrls[i].collider == col) {
                mCtrls[i].Revert(collider);
                
                if(mCurColCount > 1) {
                    mCtrls[i] = mCtrls[mCurColCount - 1];
                }

                if(mCurColCount == 1 && !string.IsNullOrEmpty(spawnGrp) && !string.IsNullOrEmpty(spawnType)) {
                    PoolController.Spawn(spawnGrp, spawnType, spawnType, null, col.transform.position, Quaternion.identity);
                }
                
                mCurColCount--;
                break;
            }
        }
    }
}
