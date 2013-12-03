using UnityEngine;
using System.Collections;

public class CheckpointTrigger : MonoBehaviour {
    public Transform point;

    void OnTriggerEnter(Collider col) {
        LevelController.CheckpointSet(point ? point.position : transform.position);
    }

    void OnDrawGizmos() {
        Color clr = Color.blue;
        clr.a = 0.5f;
        Gizmos.color = clr;

        Gizmos.DrawSphere(point ? point.position : transform.position, 0.3f);
    }
}
