using UnityEngine;
using System.Collections;

public class EntityBlinkDelay : MonoBehaviour {
    public float delay;
    public float duration = 10.0f;
    public bool relativeToProjDecay = false;

    private bool mStarted;

    private EntityBase mEnt;

    void OnEnable() {
        if(mStarted && !IsInvoking("DoBlink")) {
            float d;
            if(relativeToProjDecay)
                d = GetComponent<Projectile>().decayDelay - duration;
            else
                d = delay;

            Invoke("DoBlink", d);
        }
    }

    void OnDisable() {
        CancelInvoke();
    }

    void Awake() {
        mEnt = GetComponent<EntityBase>();
    }

	// Use this for initialization
	void Start () {
        mStarted = true;
        OnEnable();
	}
	
    void DoBlink() {
        mEnt.Blink(duration);
    }
}
