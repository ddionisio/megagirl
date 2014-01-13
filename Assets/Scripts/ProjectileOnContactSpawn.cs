using UnityEngine;
using System.Collections;

public class ProjectileOnContactSpawn : MonoBehaviour {

    public string projGroup;
    public string projType;

    public int spawnCount;
    public float spawnAngleRange;

    public bool spawnAtContactPoint;
    public float spawnNormalOfs;

    private Transform mSeek;

    public Transform seek { get { return mSeek; } set { mSeek = value; } }

    void OnDisable() {
        mSeek = null;
    }

    void OnCollisionEnter(Collision col) {
        Vector3 pos = spawnAtContactPoint ? col.contacts[0].point : collider ? collider.bounds.center : transform.position;
        pos.z = 0.0f;

        Vector3 dir = col.contacts[0].normal;
        dir.z = 0; dir.Normalize();

        pos += dir*spawnNormalOfs;

        Quaternion rot;

        if(spawnCount > 1) {
            rot = Quaternion.Euler(0, 0, -spawnAngleRange*(1.0f/((float)(spawnCount-1))));
            dir = Quaternion.Euler(0, 0, spawnAngleRange*0.5f)*dir;
        }
        else
            rot = Quaternion.identity;

        for(int i = 0; i < spawnCount; i++) {
            Projectile.Create(projGroup, projType, pos, dir, mSeek);
            dir = rot*dir;
        }
    }
}
