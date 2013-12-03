using UnityEngine;
using System.Collections;

public class ItemDrop : MonoBehaviour {
    public int groupIndex = 0;
    public Vector3 ofs;

    private EntityBase mEnt;
    private bool mDropActive;
    private int mDropRange;

    void Awake() {
        mEnt = GetComponent<EntityBase>();
        mEnt.setStateCallback += OnEntityState;
    }

    void OnEntityState(EntityBase ent) {
        switch((EntityState)ent.state) {
            case EntityState.Normal:
                mDropActive = true;
                break;

            case EntityState.Dead:
                if(mDropActive) {
                    ItemDropManager.instance.DoDrop(groupIndex, transform.position + ofs);
                    mDropActive = false;
                }
                break;
        }
    }

    void OnDrawGizmosSelected() {
        Color clr = Color.red;
        clr.a = 0.5f;
        Gizmos.color = clr;
        Gizmos.DrawSphere(transform.position + ofs, 0.15f);
    }
}
