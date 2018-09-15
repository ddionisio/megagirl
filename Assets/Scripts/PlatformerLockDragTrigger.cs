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
                mCtrls[i].lockDragOverrideCount--;
        }

        mCurColCount = 0;
    }

    void OnTriggerEnter(Collider col) {
        PlatformerController ctrl = col.GetComponent<PlatformerController>();
        if(ctrl) {
            if(mCurColCount < maxCols) {
                ctrl.lockDragOverrideCount++;
                ctrl.GetComponent<Rigidbody>().drag = drag;
                mCtrls[mCurColCount] = ctrl;
                mCurColCount++;
            }
            else {
                Debug.LogWarning("exceeded count");
            }
        }
    }
    /*
    void OnTriggerStay(Collider col) {
        int ind = -1;
        for(int i = 0; i < mCurColCount; i++) {
            if(mCtrls[i].collider == col) {
                ind = i;
                if(mCtrls[i].lockDragOverrideCount == 0)
                    mCtrls[i].lockDragOverrideCount++;
                mCtrls[i].rigidbody.drag = drag;
                break;
            }
        }

        if(ind == -1) {
            OnTriggerEnter(col);
        }
    }*/

    void OnTriggerExit(Collider col) {
        for(int i = 0; i < mCurColCount; i++) {
            if(mCtrls[i].GetComponent<Collider>() == col) {
                mCtrls[i].lockDragOverrideCount--;

                if(mCurColCount > 1) {
                    mCtrls[i] = mCtrls[mCurColCount - 1];
                }

                mCurColCount--;
                break;
            }
        }
    }
}
