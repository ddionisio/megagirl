using UnityEngine;
using System.Collections;

public class EnemyLightningTrigger : MonoBehaviour {

    public GameObject goActivate;

    private Stats mStats;

    void Awake() {
        mStats = GetComponent<Stats>();
        mStats.applyDamageCallback += OnApplyDamage;
    }

    void OnApplyDamage(Damage damage) {
        if(damage.type == Damage.Type.Lightning) {
            goActivate.SetActive(true);
        }
    }
}
