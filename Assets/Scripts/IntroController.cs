using UnityEngine;
using System.Collections;

public class IntroController : MonoBehaviour {

    void OnDestroy() {
        if(Main.instance && Main.instance.input) {
            Main.instance.input.RemoveButtonCall(0, InputAction.MenuEscape, OnInputEscape);
        }
    }

	// Use this for initialization
	void Start () {
        Main.instance.input.AddButtonCall(0, InputAction.MenuEscape, OnInputEscape);
	}
	
	void OnInputEscape(InputManager.Info dat) {
        if(dat.state == InputManager.State.Pressed) {
            Main.instance.sceneManager.LoadScene(Scenes.main);
        }
    }
}
