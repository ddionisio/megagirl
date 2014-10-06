using UnityEngine;
using System.Collections;

public class PlatformerJumpSettingTrigger : MonoBehaviour {
    [System.Serializable]
    public class Info {
        public bool jumpDelayApply = true;
        public float jumpDelay;
        public float fallSnapSpeedMin;
        public float fallSnapSpeed;
    }

    public string spawnGrp;
    public string spawnType;

    public const int maxCols = 8;

    public Info info;
    
    public string[] tags;

    private struct Data {
        private PlatformerController mCtrl;

        private float mLastJumpDelay;
        private float mLastFallSnapSpeedMin;
        private float mLastFallSnapSpeed;

        public PlatformerController ctrl { get { return mCtrl; } }
        public Collider collider { get { return mCtrl ? mCtrl.collider : null; } }
        public float lastJumpDelay { get { return mLastJumpDelay; } }

        public float lastFallSnapSpeedMin { get { return mLastFallSnapSpeedMin; } }

        public float lastFallSnapSpeed { get { return mLastFallSnapSpeed; } }

        public void Apply(Collider triggerCol, Info inf, PlatformerController aCtrl) {
            Revert(triggerCol, inf);

            mCtrl = aCtrl;

            bool isNew = true;
            foreach(Collider col in mCtrl.triggers) {
                PlatformerJumpSettingTrigger pt = col.GetComponent<PlatformerJumpSettingTrigger>();
                if(pt) {
                    foreach(PlatformerJumpSettingTrigger.Data dat in pt.mCtrls) {
                        if(dat.collider == aCtrl.collider) {
                            mLastJumpDelay = dat.mLastJumpDelay;
                            mLastFallSnapSpeedMin = dat.mLastFallSnapSpeedMin;
                            mLastFallSnapSpeed = dat.mLastFallSnapSpeed;
                            isNew = false;
                            break;
                        }
                    }

                    if(!isNew)
                        break;
                }
            }

            if(isNew) {
                mLastJumpDelay = mCtrl.jumpDelay;
                mLastFallSnapSpeedMin = mCtrl.fallSnapSpeedMin;
                mLastFallSnapSpeed = mCtrl.fallSnapSpeed;
            }

            if(inf.jumpDelayApply) mCtrl.jumpDelay = inf.jumpDelay;
            mCtrl.fallSnapSpeedMin = inf.fallSnapSpeedMin;
            mCtrl.fallSnapSpeed = inf.fallSnapSpeed;

            mCtrl.triggers.Add(triggerCol);
        }

        public void Refresh(Collider triggerCol, Info inf) {
            if(mCtrl) {
                if(inf.jumpDelayApply) mCtrl.jumpDelay = inf.jumpDelay;
                mCtrl.fallSnapSpeedMin = inf.fallSnapSpeedMin;
                mCtrl.fallSnapSpeed = inf.fallSnapSpeed;
            }
        }

        public void Revert(Collider triggerCol, Info inf) {
            if(mCtrl) {
                if(inf.jumpDelayApply) mCtrl.jumpDelay = mLastJumpDelay;
                mCtrl.fallSnapSpeedMin = mLastFallSnapSpeedMin;
                mCtrl.fallSnapSpeed = mLastFallSnapSpeed;
                mCtrl.triggers.Remove(triggerCol);
                mCtrl = null;
            }
        }
    }
    
    private Data[] mCtrls = new Data[maxCols];
    private int mCurColCount;
    
    void OnDisable() {
        for(int i = 0; i < mCurColCount; i++) {
            mCtrls[i].Revert(collider, info);
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
                mCtrls[i].Revert(collider, info);
                
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
