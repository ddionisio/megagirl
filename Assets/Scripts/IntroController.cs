using UnityEngine;
using System.Collections;

public class IntroController : MonoBehaviour {
    private bool mPressed;

    void OnDestroy() {
        if(Main.instance && Main.instance.input) {
            for(int i = 0; i < InputAction._count; i++) {
                Main.instance.input.RemoveButtonCall(0, i, OnInputEscape);
            }
        }
    }

	// Use this for initialization
	void Start () {
        for(int i = 0; i < InputAction._count; i++) {
            Main.instance.input.AddButtonCall(0, i, OnInputEscape);
        }
	}
	
	void OnInputEscape(InputManager.Info dat) {
        if(!mPressed && dat.state == InputManager.State.Pressed) {
            mPressed = true;
            Main.instance.sceneManager.LoadScene(Scenes.main);
        }
    }

    void Update() {
        if(!mPressed && (Input.anyKeyDown && !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1))) {
            mPressed = true;
            Main.instance.sceneManager.LoadScene(Scenes.main);
        }
    }
}
