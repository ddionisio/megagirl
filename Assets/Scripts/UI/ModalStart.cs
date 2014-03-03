using UnityEngine;
using System.Collections;

public class ModalStart : UIController {
    public UIEventListener play;
    public UIEventListener options;
    public UIEventListener credits;

    protected override void OnActive(bool active) {
        if(active) {
            UICamera.selectedObject = play.gameObject;

            play.onClick = OnPlay;
            options.onClick = OnOptions;
            credits.onClick = OnCredits;

        }
        else {
            play.onClick = null;
            options.onClick = null;
            credits.onClick = null;
        }
    }

    protected override void OnOpen() {
    }

    protected override void OnClose() {
    }

    void OnPlay(GameObject go) {
        //Main.instance.sceneManager.LoadScene(Scenes.levelSelect);
        UIModalManager.instance.ModalOpen("slots");
    }

    void OnOptions(GameObject go) {
        UIModalManager.instance.ModalOpen("options");
    }

    void OnCredits(GameObject go) {
        UIModalManager.instance.ModalOpen("credits");
    }
}
