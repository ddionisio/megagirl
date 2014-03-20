using UnityEngine;
using System.Collections;

public class MoveScaleOnActive : MonoBehaviour {
    public RigidBodyController target;
    public float mod = 0.2f;

    void OnEnable() {
        target.moveScale = target.moveScale + (target.moveScale*mod);
    }

    void OnDisable() {
        target.moveScale = 1.0f;
    }

    void Awake() {
        if(!target)
            target = GetComponent<RigidBodyController>();

    }
}
