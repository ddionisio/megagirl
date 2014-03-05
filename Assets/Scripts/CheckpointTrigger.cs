using UnityEngine;
using System.Collections;

public class CheckpointTrigger : MonoBehaviour {
    public Transform point;

    public GameObject disableOnEnter;
    public GameObject releaseHolder; //call Release to all entities inside this holder

    void OnTriggerEnter(Collider col) {
        LevelController.CheckpointSet(point ? point.position : transform.position);

        if(releaseHolder) {
            EntityBase[] ents = releaseHolder.GetComponentsInChildren<EntityBase>(false);
            for(int i = 0; i < ents.Length; i++) {
                if(!ents[i].isReleased)
                    ents[i].Release();
            }
        }

        if(disableOnEnter)
            disableOnEnter.SetActive(false);
    }

    void OnDrawGizmos() {
        Color clr = Color.blue;
        clr.a = 0.5f;
        Gizmos.color = clr;

        Gizmos.DrawSphere(point ? point.position : transform.position, 0.3f);
    }
}
