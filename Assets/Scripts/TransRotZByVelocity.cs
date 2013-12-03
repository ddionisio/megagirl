using UnityEngine;
using System.Collections;

public class TransRotZByVelocity : MonoBehaviour {
    public Rigidbody target;
    public float rotatePerMeter;
    public float scale = 1.0f;

    void Awake() {
        if(target == null)
            target = rigidbody;
    }
    	
	// Update is called once per frame
	void Update () {
        Vector3 angles = transform.localEulerAngles;
        angles.z += rotatePerMeter * target.velocity.x * Time.deltaTime * scale;
        transform.localEulerAngles = angles;
	}
}
