using UnityEngine;
using System.Collections;

public class GOActiveSwapInterval : MonoBehaviour {
    public GameObject target1;
    public GameObject target2;

    public float startDelay;
    public float delay;

    private bool mStarted;

    void OnEnable() {
        if(mStarted && !IsInvoking("DoIt"))
            InvokeRepeating("DoIt", startDelay, delay);
    }

    void OnDisable() {
        CancelInvoke();
    }

	// Use this for initialization
	void Start () {
        mStarted = true;
        InvokeRepeating("DoIt", startDelay, delay);
	}
	
    void DoIt() {
        target1.SetActive(!target1.activeSelf);
        target2.SetActive(!target2.activeSelf);
    }
}
