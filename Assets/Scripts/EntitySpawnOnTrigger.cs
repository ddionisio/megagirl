using UnityEngine;
using System.Collections;

public class EntitySpawnOnTrigger : MonoBehaviour {
    public string entGroup;
    public string entType;

    public bool onEnter;
    public bool onExit;
    public bool useColliderPos;

    public Vector3 ofs;

    void OnTriggerEnter(Collider col) {
        if(onEnter)
            DoSpawn(col);
    }

    void OnTriggerExit(Collider col) {
        if(onExit)
            DoSpawn(col);
    }

    void DoSpawn(Collider col) {
        Vector3 pos = useColliderPos ? col.bounds.center : col.transform.position;
        PoolController.Spawn(entGroup, entType, entType, null, pos+ofs, Quaternion.identity);
    }
}
