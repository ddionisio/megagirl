using UnityEngine;
using System.Collections;

public class ModalStart : UIController {
    public UIEventListener continueGame;
    public UIEventListener newGame;
    public UIEventListener options;
    public UIEventListener credits;

    protected override void OnActive(bool active) {
        if(active) {
            if(continueGame) {
                UICamera.selectedObject = continueGame.gameObject;

                continueGame.onClick = OnContinueGame;
            }
            else
                UICamera.selectedObject = newGame.gameObject;

            newGame.onClick = OnNewGame;
            options.onClick = OnOptions;
            credits.onClick = OnCredits;

        }
        else {
            if(continueGame)
                continueGame.onClick = null;

            newGame.onClick = null;
            options.onClick = null;
            credits.onClick = null;
        }
    }

    protected override void OnOpen() {
    }

    protected override void OnClose() {
    }

    void OnNewGame(GameObject go) {
        if(PlayerStats.isGameExists) {
            UIModalConfirm.Open(
                GameLocalize.GetText("newgame_confirm_title"),
                GameLocalize.GetText("newgame_confirm_desc"),
            delegate(bool yes) {
                if(yes) {
                    Debug.Log("clearing save");
                    SceneState.instance.ClearAllSavedData();
                    UserData.instance.Save();

                    PlayerStats.isGameExists = true;
                    Main.instance.sceneManager.LoadScene(Scenes.levelSelect);
                }
            });
        }
        else {
            PlayerStats.isGameExists = true;
            Main.instance.sceneManager.LoadScene(Scenes.levelSelect);
        }
    }

    void OnContinueGame(GameObject go) {
        Main.instance.sceneManager.LoadScene(Scenes.levelSelect);
    }

    void OnOptions(GameObject go) {
        UIModalManager.instance.ModalOpen("options");
    }

    void OnCredits(GameObject go) {
        UIModalManager.instance.ModalOpen("credits");
    }
}
