using UnityEngine;
using System.Collections;

public class EntitySensor : MonoBehaviour {
    public delegate void OnUpdate(EntitySensor sensor);
    public Vector3 ofs;
    public Vector3[] points;
    public LayerMask masks;
    public float delay = 0.2f;

    private RaycastHit mHit;
    private bool mIsHit;
    private bool mHFlip;

    public event OnUpdate updateCallback;

    public bool hFlip { get { return mHFlip; } set { mHFlip = value; } }
    public bool isHit { get { return mIsHit; } }
    public RaycastHit hit { get { return mHit; } }

    public void Activate(bool yes) {
        if(yes) {
            StartCoroutine(DoUpdate());
        }
        else {
            StopAllCoroutines();
        }
    }

    void OnDestroy() {
        updateCallback = null;
    }

    IEnumerator DoUpdate() {
        WaitForSeconds wait = new WaitForSeconds(delay);
        while(true) {
            yield return wait;
                        
            if(points != null) {
                Vector3 pos = transform.position;

                if(mHFlip) {
                    pos.x -= ofs.x;
                }
                else {
                    pos.x += ofs.x;
                }

                pos.y += ofs.y;

                for(int i = 0, max = points.Length; i < max; i++) {
                    Vector3 npos = points[i];
                    if(mHFlip) {
                        npos.x *= -1.0f;
                    }

                    Vector3 dir = npos;
                    float dist = dir.magnitude;
                    dir /= dist;

                    mIsHit = Physics.Raycast(pos, dir, out mHit, dist, masks);
                    if(mIsHit)
                        break;

                    pos += npos;
                }

                if(updateCallback != null)
                    updateCallback(this);
            }
        }
    }

    void OnDrawGizmosSelected() {
        Gizmos.color = Color.green;

        Vector3 pos = transform.position + ofs;

        if(points != null) {
            foreach(Vector3 p in points) {
                Gizmos.DrawLine(pos, pos + p);
                pos += p;
            }
        }
    }
}
