using UnityEngine;
using System.Collections;

public class SceneSkip : MonoBehaviour {
    public float wait = 1.5f;
    public string toScene;

    void OnDestroy() {
        InputManager input = Main.instance ? Main.instance.input : null;
        if(input) {
            for(int i = 0; i < InputAction._count; i++)
                input.RemoveButtonCall(0, i, OnInput);
        }
    }

    void Awake() {
        Invoke("EnableInput", wait);
    }

    void EnableInput() {
        for(int i = 0; i < InputAction._count; i++)
            Main.instance.input.AddButtonCall(0, i, OnInput);
    }

    void OnInput(InputManager.Info dat) {
        if(!UIModalManager.instance.ModalIsInStack(UIModalConfirm.modalName)) {
            if(dat.state == InputManager.State.Pressed) {
                UIModalConfirm.Open(GameLocalize.GetText("skip_confirm_title"), GameLocalize.GetText("skip_confirm_desc"),
                                    delegate(bool yes) {
                    if(yes) {
                        for(int i = 0; i < InputAction._count; i++)
                            Main.instance.input.RemoveButtonCall(0, i, OnInput);

                        Main.instance.sceneManager.LoadScene(toScene);
                    }
                });
            }
        }
    }
}
