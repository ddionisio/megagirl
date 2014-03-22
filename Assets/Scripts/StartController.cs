using UnityEngine;
using System.Collections;

public class StartController : MonoBehaviour {

	// Use this for initialization
	void Start () {
        switch(Main.instance.platform) {
            case GamePlatform.Web:
                UIModalManager.instance.ModalOpen("start");
                break;

            default:
                UIModalManager.instance.ModalOpen("startWithExit");
                break;
        }
	}
}
