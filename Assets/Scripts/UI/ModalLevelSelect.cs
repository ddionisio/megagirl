using UnityEngine;
using System.Collections;

public class ModalLevelSelect : UIController {
    public const string levelSelectBossIntroUDKey = "bossIntro";

    public UILevelSelectItem gitgirl;
    public UILevelSelectItem finalLevel;

    public AnimatorData levelSelectAnimDat;
    public tk2dSpriteAnimator characterSpriteAnim;
    public UILabel characterSelectedNameLabel;

    public AnimatorData bossAlertAnimDat;

    private UILevelSelectItem[] mLevelItems;

    private bool mLockInput;

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
#if false
        //cheat
        Weapon.UnlockWeapon(1);
        Weapon.UnlockWeapon(2);
        Weapon.UnlockWeapon(3);
        Weapon.UnlockWeapon(4);
        Weapon.UnlockWeapon(5);
        Weapon.UnlockWeapon(6);

        for(int i = 0; i < mLevelItems.Length; i++) {
            if(!string.IsNullOrEmpty(mLevelItems[i].level)) {
                SceneState.instance.SetGlobalValue(mLevelItems[i].level, 1, true);
                mLevelItems[i].Init();
            }
        }
#endif
        mLockInput = false;

        //check if we need to play boss intro
        bool initFinalLevelItem = true;

        if(UserData.instance.GetInt(levelSelectBossIntroUDKey, 0) == 0) {
            int completeCount = 0;
            for(int i = 0; i < mLevelItems.Length; i++) {
                if(mLevelItems[i] != finalLevel && mLevelItems[i] != gitgirl && mLevelItems[i].isCompleted)
                    completeCount++;
            }

            if(completeCount == mLevelItems.Length - 2) {
                mLockInput = true;
                UserData.instance.SetInt(levelSelectBossIntroUDKey, 1);
                bossAlertAnimDat.Play("go"); //animator will re-open this modal after the intro
                initFinalLevelItem = false;
            }
        }

        if(initFinalLevelItem) {
            finalLevel.InitFinalLevel(mLevelItems, gitgirl);
        }

        //reset final stages
        LevelController.levelFinalCurrent = 0;
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
        if(mLockInput)
            return;

        for(int i = 0, max = mLevelItems.Length; i < max; i++) {
            if(mLevelItems[i].gameObject == go && mLevelItems[i] != gitgirl) {
                UILabel label = go.GetComponentInChildren<UILabel>();
                characterSelectedNameLabel.text = label.text;
                mLockInput = mLevelItems[i].Click(characterSpriteAnim, levelSelectAnimDat, "go");
                break;
            }
        }
    }

    void OnInputOptions(InputManager.Info dat) {
        if(mLockInput)
            return;

        if(dat.state == InputManager.State.Pressed) {
            UIModalManager.instance.ModalOpen("options");
        }
    }
}
