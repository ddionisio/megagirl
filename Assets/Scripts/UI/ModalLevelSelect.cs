using UnityEngine;
using System.Collections;

public class ModalLevelSelect : UIController {
    public UILevelSelectItem gitgirl;
    public UILevelSelectItem finalLevel;

    public AnimatorData levelSelectAnimDat;
    public tk2dSpriteAnimator characterSpriteAnim;
    public UILabel characterSelectedNameLabel;

    private UILevelSelectItem[] mLevelItems;

    protected override void OnActive(bool active) {
        if(active) {
            UICamera.selectedObject = finalLevel.isFinalUnlock ? finalLevel.gameObject : gitgirl.gameObject;

            foreach(UILevelSelectItem item in mLevelItems) {
                if(item.gameObject.activeSelf) {
                    item.listener.onClick = OnLevelClick;
                    item.listener.onSelect = OnLevelSelect;
                }
            }

            Main.instance.input.AddButtonCall(0, InputAction.MenuEscape, OnInputOptions);
        }
        else {
            foreach(UILevelSelectItem item in mLevelItems) {
                if(item.gameObject.activeSelf) {
                    item.listener.onClick = null;
                    item.listener.onSelect = null;
                }
            }

            Main.instance.input.RemoveButtonCall(0, InputAction.MenuEscape, OnInputOptions);
        }
    }

    protected override void OnOpen() {
    }

    protected override void OnClose() {
    }

    void Awake() {
        mLevelItems = GetComponentsInChildren<UILevelSelectItem>(true);

        //init items
        foreach(UILevelSelectItem item in mLevelItems) {
            if(item.gameObject.activeSelf) {
                item.Init();
            }
        }

        finalLevel.InitFinalLevel(mLevelItems);
    }

    void OnLevelSelect(GameObject go, bool s) {
        if(s) {
            for(int i = 0, max = mLevelItems.Length; i < max; i++) {
                if(mLevelItems[i].gameObject == go) {
                    gitgirl.portrait.spriteName = mLevelItems[i].gitGirlPortraitRef;
                    break;
                }
            }
        }
    }

    void OnLevelClick(GameObject go) {
        for(int i = 0, max = mLevelItems.Length; i < max; i++) {
            if(mLevelItems[i].gameObject == go && mLevelItems[i] != gitgirl) {
                UILabel label = go.GetComponentInChildren<UILabel>();
                characterSelectedNameLabel.text = label.text;
                mLevelItems[i].Click(characterSpriteAnim, levelSelectAnimDat, "go");
                break;
            }
        }
    }

    void OnInputOptions(InputManager.Info dat) {
        if(dat.state == InputManager.State.Pressed) {
            UIModalManager.instance.ModalOpen("options");
        }
    }
}
