﻿using UnityEngine;
using System.Collections;

public class PlayerSceneSkipToFinal : MonoBehaviour {
    public float wait = 1.5f;

    void OnDestroy() {
        InputManager input = Main.instance ? Main.instance.input : null;
        if(input) {
            input.RemoveButtonCall(0, InputAction.MenuEscape, OnInput);
        }
    }
    
    void Awake() {
        Invoke("EnableInput", wait);
    }
    
    void EnableInput() {
        Main.instance.input.AddButtonCall(0, InputAction.MenuEscape, OnInput);
    }
    
    void OnInput(InputManager.Info dat) {
        if(!UIModalManager.instance.ModalIsInStack(UIModalConfirm.modalName)) {
            if(dat.state == InputManager.State.Pressed) {
                UIModalConfirm.Open(GameLocalize.GetText("skip_confirm_title"), GameLocalize.GetText("skip_confirm_desc"),
                                    delegate(bool yes) {
                    if(yes) {
                        Main.instance.input.RemoveButtonCall(0, InputAction.MenuEscape, OnInput);
                        Player.instance.state = (int)EntityState.Final;
                    }
                });
            }
        }
    }
}
