using UnityEngine;
using System.Collections;

public class ModalGameOver : UIController {
    public UIEventListener retry;
    public UIEventListener stageSelect;
    public UIEventListener main;

    public UIWidget retryWidget;
    public UIWidget stageSelectWidget;

    public Color disableColor = Color.gray;

    protected override void OnActive(bool active) {
        if(active) {
            retry.onClick = OnRetry;
            stageSelect.onClick = OnStageSelect;
            main.onClick = OnMain;

            if(SlotInfo.gameMode == SlotInfo.GameMode.Hardcore) {
                retryWidget.color = disableColor;
                stageSelectWidget.color = disableColor;

                UIButtonKeys mainBtns = main.GetComponent<UIButtonKeys>();
                mainBtns.selectOnUp = null;
                mainBtns.selectOnDown = null;
                UICamera.selectedObject = main.gameObject;
            }
            else {
                UICamera.selectedObject = retry.gameObject;
            }
        }
        else {
            retry.onClick = null;
            stageSelect.onClick = null;
            main.onClick = null;
        }
    }
    
    protected override void OnOpen() {
    }
    
    protected override void OnClose() {
    }

    void OnRetry(GameObject go) {
        if(!string.IsNullOrEmpty(LevelController.levelLoaded))
            Main.instance.sceneManager.LoadScene(LevelController.levelLoaded);
        else
            OnStageSelect(go);
    }

    void OnStageSelect(GameObject go) {
        Main.instance.sceneManager.LoadScene(Scenes.levelSelect);
    }

    void OnMain(GameObject go) {
        Main.instance.sceneManager.LoadScene(Scenes.main);
    }
}
