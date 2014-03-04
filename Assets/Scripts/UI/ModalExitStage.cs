using UnityEngine;
using System.Collections;

public class ModalExitStage : UIController {
    public UIEventListener cancel;
    public UIEventListener toLevelSelect;
    public UIEventListener toMain;

    public UIWidget levelSelectWidget;
    public Color disableColor = Color.gray;

    private UIButtonKeys mCancelKeys;
    private UIButtonKeys mLevelSelectKeys;
    private UIButtonKeys mMainKeys;

    protected override void OnActive(bool active) {
        if(active) {
            cancel.onClick = OnCancel;
            toLevelSelect.onClick = OnLevelSelect;
            toMain.onClick = OnMain;

            bool isHardcore = SlotInfo.gameMode == SlotInfo.GameMode.Hardcore;

            if(isHardcore && !LevelController.isLevelLoadedComplete) {
                mCancelKeys.selectOnDown = mMainKeys;
                mMainKeys.selectOnUp = mCancelKeys;

                levelSelectWidget.color = disableColor;
            }
            else {
                mCancelKeys.selectOnDown = mLevelSelectKeys;
                mMainKeys.selectOnUp = mLevelSelectKeys;

                levelSelectWidget.color = Color.white;
            }

            UICamera.selectedObject = cancel.gameObject;
        }
        else {
            cancel.onClick = null;
            toLevelSelect.onClick = null;
            toMain.onClick = null;
        }
    }
    
    protected override void OnOpen() {
    }
    
    protected override void OnClose() {
    }

    void OnCancel(GameObject go) {
        UIModalManager.instance.ModalCloseTop();
    }

    void OnLevelSelect(GameObject go) {
        Main.instance.sceneManager.LoadScene(Scenes.levelSelect);
    }

    void OnMain(GameObject go) {
        Main.instance.sceneManager.LoadScene(Scenes.main);
    }

    void Awake() {
        mCancelKeys = cancel.GetComponent<UIButtonKeys>();
        mLevelSelectKeys = toLevelSelect.GetComponent<UIButtonKeys>();
        mMainKeys = toMain.GetComponent<UIButtonKeys>();
    }
}
