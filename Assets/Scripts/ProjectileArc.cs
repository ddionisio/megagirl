using UnityEngine;
using System.Collections;

/// <summary>
/// Note: make sure to use rigidbody and with gravity
/// </summary>
public class ProjectileArc : Projectile {
    public float farVelocity;
    public float farDistance;

    public float nearVelocity;
    public float nearDistance;

    public float straightVelocity = 18.0f; //if theta is invalid
    public float randXRange;

    protected override void StateChanged() {
        base.StateChanged();

        switch((State)state) {
            case State.Seek:
                Vector3 pos = GetComponent<Collider>().bounds.center;
                Vector3 target = mSeek.position;
                if(randXRange > 0.0f) {
                    target.x += Random.Range(-randXRange, randXRange);
                }

                if(target.x != pos.x) {
                    float vel = seekVelocity;
                                       
                    float x = target.x - pos.x;
                    float y = target.y - pos.y;

                    float distSqr = target.y < pos.y ? x * x : (target - pos).sqrMagnitude;
                    if(distSqr > farDistance * farDistance)
                        vel = farVelocity;
                    else if(distSqr < nearDistance * nearDistance)
                        vel = nearVelocity;

                    //determine angle
                    GravityController gctrl = GetComponent<GravityController>();
                    float grav = Mathf.Abs(gctrl != null ? gctrl.gravity : Physics.gravity.magnitude);
                    float vSqr = vel * vel;

                    float theta = Mathf.Atan((vSqr + Mathf.Sqrt(vSqr * vSqr + grav * (grav * x * x + 2 * y * vSqr))) / (grav * x));
                    float theta2 = Mathf.Atan((vSqr - Mathf.Sqrt(vSqr * vSqr + grav * (grav * x * x + 2 * y * vSqr))) / (grav * x));

                    if(!float.IsNaN(theta)) {
                        mInitDir.Set(Mathf.Sign(x), 0, 0);
                        mInitDir = Quaternion.AngleAxis(Mathf.Rad2Deg * theta, Vector3.forward) * mInitDir;
                    }
                    else if(!float.IsNaN(theta2)) {
                        mInitDir.Set(Mathf.Sign(x), 0, 0);
                        mInitDir = Quaternion.AngleAxis(Mathf.Rad2Deg * theta2, Vector3.forward) * mInitDir;
                    }
                    else {
                        mInitDir = new Vector3(x, y, 0);
                        mInitDir.Normalize();
                        vel = straightVelocity;
                    }

                    GetComponent<Rigidbody>().velocity = mInitDir * vel;// .AddForce(mDir * vel, ForceMode.VelocityChange);
                    //Debug.Log("theta: " + (Mathf.Rad2Deg * theta));
                }
                else {
                    GetComponent<Rigidbody>().AddForce(new Vector3(0.0f, Mathf.Sign(target.y - pos.y), 0.0f) * seekVelocity, ForceMode.VelocityChange);
                }

                seek = null;
                mActiveForce = Vector3.zero;
                break;
        }
    }
}
