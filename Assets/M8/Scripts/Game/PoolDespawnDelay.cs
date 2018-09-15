﻿using UnityEngine;
using System.Collections;

[AddComponentMenu("M8/Game/PoolDespawnDelay")]
public class PoolDespawnDelay : MonoBehaviour {
    public delegate void DespawnCall(GameObject go);

    public float delay = 1.0f;

    public event DespawnCall despawnCallback;

    void OnDestroy() {
        despawnCallback = null;
    }

    void OnSpawned() {
        Invoke("DoDespawn", delay);
    }

    void DoDespawn() {
        if(despawnCallback != null)
            despawnCallback(gameObject);

        PoolController.ReleaseAuto(transform);
    }
}
