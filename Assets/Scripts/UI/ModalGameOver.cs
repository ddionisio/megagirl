using UnityEngine;
using System.Collections;

public class ModalGameOver : UIController {
    public UIEventListener retry;
    public UIEventListener stageSelect;

    protected override void OnActive(bool active) {
        if(active) {
            retry.onClick = OnRetry;
            stageSelect.onClick = OnStageSelect;
        }
        else {
            retry.onClick = null;
            stageSelect.onClick = null;
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
}
