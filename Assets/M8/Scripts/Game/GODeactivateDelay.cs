using UnityEngine;
using System.Collections;

[AddComponentMenu("M8/Game Object/Deactivate Delay")]
public class GODeactivateDelay : MonoBehaviour {
    public delegate void OnDeactivate();

    public GameObject target;
    public bool resetActive;

	public float delay = 1.0f;

    public event OnDeactivate deactivateCallback;

    void OnDestroy() {
        deactivateCallback = null;
    }
	
	void OnEnable() {
        if(resetActive && target)
            target.SetActive(true);

		Invoke("OnDeactive", delay);
	}

    void OnDisable() {
        CancelInvoke();
    }
	
	void OnDeactive() {
        if(target)
            target.SetActive(false);
        else
		    gameObject.SetActive(false);

        if(deactivateCallback != null)
            deactivateCallback();
	}
}
