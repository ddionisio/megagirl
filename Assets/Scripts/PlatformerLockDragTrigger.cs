using UnityEngine;
using System.Collections;

public class PlatformerLockDragTrigger : MonoBehaviour {
    public const int maxCols = 8;

    public string[] tags;
    public float drag;

    private PlatformerController[] mCtrls = new PlatformerController[maxCols];
    private int mCurColCount;

    void OnDisable() {
        for(int i = 0; i < mCurColCount; i++) {
            if(mCtrls[i])
                mCtrls[i].lockDrag = false;
        }

        mCurColCount = 0;
    }

    void OnTriggerEnter(Collider col) {
        PlatformerController ctrl = col.GetComponent<PlatformerController>();
        if(ctrl) {
            if(mCurColCount < maxCols) {
                ctrl.lockDrag = true;
                ctrl.rigidbody.drag = drag;
                mCtrls[mCurColCount] = ctrl;
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
                ind = i;
                mCtrls[i].lockDrag = true;
                mCtrls[i].rigidbody.drag = drag;
                break;
            }
        }

        if(ind == -1) {
            OnTriggerEnter(col);
        }
    }

    void OnTriggerExit(Collider col) {
        for(int i = 0; i < mCurColCount; i++) {
            if(mCtrls[i].collider == col) {
                mCtrls[i].lockDrag = false;

                if(mCurColCount > 1) {
                    mCtrls[i] = mCtrls[mCurColCount - 1];
                }

                mCurColCount--;
                break;
            }
        }
    }
}
