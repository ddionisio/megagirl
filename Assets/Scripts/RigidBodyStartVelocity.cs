using UnityEngine;
using System.Collections;

public class RigidBodyStartVelocity : MonoBehaviour {
    public Vector3 velocity;
    public bool onEnable = true;

    private bool mStarted;

    void OnEnable() {
        if(mStarted && onEnable)
            rigidbody.AddForce(velocity, ForceMode.VelocityChange);
    }

	// Use this for initialization
	void Start () {
        mStarted = true;
        rigidbody.AddForce(velocity, ForceMode.VelocityChange);
	}
}
