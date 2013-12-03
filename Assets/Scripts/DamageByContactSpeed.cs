using UnityEngine;
using System.Collections;

public class DamageByContactSpeed : MonoBehaviour {
    public float speed;
    public bool drop = true;
    private Damage mDamage;

    void Awake() {
        mDamage = GetComponent<Damage>();
    }

    void OnCollisionEnter(Collision col) {
        //Debug.Log("speed: "+col.relativeVelocity.magnitude);
        if(col.relativeVelocity.sqrMagnitude > speed*speed) {
            if(!drop || (collider.bounds.center.y >= col.contacts[0].point.y && Vector3.Angle(rigidbody.velocity, col.gameObject.transform.up) > 90.0f))
                mDamage.CallDamageTo(col.gameObject, col.contacts[0].point, col.contacts[0].normal);
        }
    }
}
