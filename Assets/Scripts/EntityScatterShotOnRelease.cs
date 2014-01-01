using UnityEngine;
using System.Collections;

/// <summary>
/// Explode into a bunch of projectiles in a circle
/// </summary>
public class EntityScatterShotOnRelease : MonoBehaviour {
    public string projSubGroup = Enemy.projGroup;
    public string projSubType = Enemy.projCommonType;
    public float angleStart = 0;
    public int projSubCount = 8;
    public bool seekPlayer;

    void Awake() {
        EntityBase ent = GetComponent<EntityBase>();
        ent.releaseCallback += OnEntityRelease;
    }

    void OnEntityRelease(EntityBase ent) {
        Transform seek = seekPlayer ? Player.instance.transform : null;
        Vector3 pt = transform.position; pt.z = 0.0f;
        Vector3 dir = Vector3.up;
        float angleInc = 360.0f/((float)projSubCount);

        dir = Quaternion.AngleAxis(angleStart, Vector3.forward)*dir;

        for(int i = 0; i < projSubCount; i++) {
            Projectile.Create(projSubGroup, projSubType, pt, dir, seek);
            dir = Quaternion.AngleAxis(angleInc, Vector3.forward)*dir;
        }
    }
}
