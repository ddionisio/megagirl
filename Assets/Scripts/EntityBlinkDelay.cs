using UnityEngine;
using System.Collections;

public class EntityBlinkDelay : MonoBehaviour {
    public float delay;
    public float duration = 10.0f;

    private bool mStarted;

    private EntityBase mEnt;

    void OnEnable() {
        if(mStarted && !IsInvoking("DoBlink"))
            Invoke("DoBlink", delay);
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
        Invoke("DoBlink", delay);
	}
	
    void DoBlink() {
        mEnt.Blink(duration);
    }
}
