using UnityEngine;
using System.Collections;

public class StatsGameObjectDeath : MonoBehaviour {
    public float releaseDelay = 1.0f;
    private Stats mStats;

    void OnEnable() {
        mStats.Reset();
    }

    void OnDisable() {
        StopAllCoroutines();
    }

    void Awake() {
        mStats = GetComponent<Stats>();
        mStats.changeHPCallback += OnHPChange;
    }

    void OnHPChange(Stats stats, float delta) {
        if(stats.curHP == 0.0f) {
            StartCoroutine(DoRelease());
        }
    }

    IEnumerator DoRelease() {
        bool kinematic = false;
        if(GetComponent<Rigidbody>()) {
            kinematic = GetComponent<Rigidbody>().isKinematic;
            GetComponent<Rigidbody>().velocity = Vector3.zero;
            GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
            GetComponent<Rigidbody>().isKinematic = true;
        }

        if(releaseDelay > 0)
            yield return new WaitForSeconds(releaseDelay);
        else
            yield return new WaitForFixedUpdate();

        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        if(GetComponent<Rigidbody>()) {
            GetComponent<Rigidbody>().isKinematic = kinematic;
        }
                        
        PoolController.ReleaseAuto(transform);
    }
}
