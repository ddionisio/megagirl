using UnityEngine;
using System.Collections;

public class ModalLevelSelect : UIController {
    public const string levelSelectBossIntroUDKey = "bossIntro";

    public UILevelSelectItem gitgirl;
    public UILevelSelectItem finalLevel;

    public AnimatorData levelSelectAnimDat;
    public tk2dSpriteAnimator characterSpriteAnim;
    public NGUILabelTypewrite characterSelectedNameLabel;

    public AnimatorData bossAlertAnimDat;

    public GameObject infoActiveGO;

    private UILevelSelectItem[] mLevelItems;
    private GameObject mCurInfoSubActiveGO;

    private bool mLockInput;

    protected override void OnActive(bool active) {
        if(active) {
            UILevelSelectItem levelSelected = finalLevel.isFinalUnlock ? finalLevel : gitgirl;
            UICamera.selectedObject = levelSelected.gameObject;

            mCurInfoSubActiveGO = levelSelected.infoSubActiveGO ? levelSelected.infoSubActiveGO : gitgirl.infoSubActiveGO;
            mCurInfoSubActiveGO.SetActive(true);

            foreach(UILevelSelectItem item in mLevelItems) {
                if(item.gameObject.activeSelf) {
                    item.listener.onClick = OnLevelClick;
                    item.listener.onSelect = OnLevelSelect;
                }
            }

            if(Main.instance && Main.instance.input) {
                Main.instance.input.AddButtonCall(0, InputAction.MenuEscape, OnInputOptions);
                Main.instance.input.AddButtonCall(0, InputAction.Fire, OnInputInfo);
            }
        }
        else {
            foreach(UILevelSelectItem item in mLevelItems) {
                if(item.gameObject.activeSelf) {
                    item.listener.onClick = null;
                    item.listener.onSelect = null;
                }
            }

            if(Main.instance && Main.instance.input) {
                Main.instance.input.RemoveButtonCall(0, InputAction.MenuEscape, OnInputOptions);
                Main.instance.input.RemoveButtonCall(0, InputAction.Fire, OnInputInfo);
            }
        }
    }

    protected override void OnOpen() {
        infoActiveGO.SetActive(false);

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
    }

    protected override void OnClose() {
        if(mCurInfoSubActiveGO) {
            mCurInfoSubActiveGO.SetActive(false);
            mCurInfoSubActiveGO = null;
        }

        infoActiveGO.SetActive(false);
    }

    void Awake() {
        mLevelItems = GetComponentsInChildren<UILevelSelectItem>(true);

        //init items
        foreach(UILevelSelectItem item in mLevelItems) {
            if(item.gameObject.activeSelf) {
                item.Init();
            }
        }

        infoActiveGO.SetActive(false);
    }

    void OnLevelSelect(GameObject go, bool s) {
        if(s) {
            for(int i = 0, max = mLevelItems.Length; i < max; i++) {
                if(mLevelItems[i].gameObject == go) {
                    gitgirl.portrait.spriteName = mLevelItems[i].gitGirlPortraitRef;

                    mLevelItems[i].Selected();

                    //infoSubActiveGO
                    if(mLevelItems[i].infoSubActiveGO && mLevelItems[i].infoSubActiveGO != mCurInfoSubActiveGO) {
                        if(mCurInfoSubActiveGO)
                            mCurInfoSubActiveGO.SetActive(false);

                        mCurInfoSubActiveGO = mLevelItems[i].infoSubActiveGO;
                        mCurInfoSubActiveGO.SetActive(true);
                    }
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

    void OnInputInfo(InputManager.Info dat) {
        if(mLockInput)
            return;

        if(dat.state == InputManager.State.Pressed) {
            infoActiveGO.SetActive(!infoActiveGO.activeSelf);
        }
    }
}
