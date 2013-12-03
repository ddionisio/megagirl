using UnityEngine;
using System.Collections;

public class DamageTrigger : MonoBehaviour {
    public delegate void GenericCallback(DamageTrigger trigger, GameObject victim);

    public event GenericCallback damageCallback;

    private Damage mDmg;

    public Damage damage { get { return mDmg; } }

    void OnDestroy() {
        damageCallback = null;
    }

    void Awake() {
        mDmg = GetComponent<Damage>();
    }

    void OnTriggerStay(Collider col) {
        mDmg.CallDamageTo(col.gameObject, transform.position, (col.bounds.center - transform.position).normalized);

        if(damageCallback != null) {
            damageCallback(this, col.gameObject);
        }
    }
}
