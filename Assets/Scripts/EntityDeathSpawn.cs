using UnityEngine;
using System.Collections;

public class EntityDeathSpawn : MonoBehaviour {
    public string deathSpawnGroup = "deaths"; //spawn an effect on death
    public string deathSpawnType = "";

    void Awake() {
        EntityBase ent = GetComponent<EntityBase>();
        if(ent)
            ent.setStateCallback += OnEntityStateChange;
    }

    void OnEntityStateChange(EntityBase ent) {
        if((EntityState)ent.state == EntityState.Dead)
            PoolController.Spawn(deathSpawnGroup, deathSpawnType, deathSpawnType, null, ent.transform.position, Quaternion.identity);
    }
}
