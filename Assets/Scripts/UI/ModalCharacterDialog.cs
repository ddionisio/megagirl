using UnityEngine;
using System.Collections;

public class ModalCharacterDialog : UIModalCharacterDialog {
    public UILabel nameLabel;
    public UILabel textLabel;
    public UISprite portrait;

    private bool mDoLayoutUpdate = false;

    protected override void OnActive(bool active) {
        InputManager input = Main.instance ? Main.instance.input : null;

        if(active) {
            if(input) {
                input.AddButtonCall(0, InputAction.MenuAccept, OnInput);
                input.AddButtonCall(0, InputAction.MenuEscape, OnInput);
                input.AddButtonCall(0, InputAction.Slide, OnInput);
                input.AddButtonCall(0, InputAction.Fire, OnInput);
                input.AddButtonCall(0, InputAction.Jump, OnInput);
            }
        }
        else {
            if(input) {
                input.RemoveButtonCall(0, InputAction.MenuAccept, OnInput);
                input.RemoveButtonCall(0, InputAction.MenuEscape, OnInput);
                input.RemoveButtonCall(0, InputAction.Slide, OnInput);
                input.RemoveButtonCall(0, InputAction.Fire, OnInput);
                input.RemoveButtonCall(0, InputAction.Jump, OnInput);
            }
        }
    }
    
    protected override void OnOpen() {
    }
    
    protected override void OnClose() {
    }

    public override void Apply(bool isLocalized, string text, string aName = null, string portraitSpriteRef = null, string[] choices = null) {
        nameLabel.text = isLocalized ? GameLocalize.GetText(aName) : aName;
        textLabel.text = isLocalized ? GameLocalize.GetText(text) : text;
        portrait.spriteName = portraitSpriteRef;
        portrait.MakePixelPerfect();

        mDoLayoutUpdate = true;
    }

    void OnInput(InputManager.Info dat) {
        if(dat.state == InputManager.State.Pressed) {
            Action(-1);
        }
    }

    void LateUpdate() {
        if(mDoLayoutUpdate) {
            NGUILayoutBase.RefreshNow(transform);
            mDoLayoutUpdate = false;
        }
    }
}
