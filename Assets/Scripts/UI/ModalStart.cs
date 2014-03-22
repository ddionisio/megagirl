using UnityEngine;
using System.Collections;

public class ModalStart : UIController {
    public UIEventListener play;
    public UIEventListener trial;
    public UIEventListener options;
    public UIEventListener credits;
    public UIEventListener exit;

    public GameObject activeGODefault;
    public GameObject activeGOCleared;

    protected override void OnActive(bool active) {
        if(active) {
            UICamera.selectedObject = play.gameObject;

            play.onClick = OnPlay;
            trial.onClick = OnTrial;
            options.onClick = OnOptions;
            credits.onClick = OnCredits;

            if(exit)
                exit.onClick = OnExit;

        }
        else {
            play.onClick = null;
            trial.onClick = null;
            options.onClick = null;
            credits.onClick = null;

            if(exit)
                exit.onClick = null;
        }
    }

    protected override void OnOpen() {
        SlotInfo.ClearCurrentSlotLoaded();
        ((UserSlotData)UserData.instance).SetSlot(-1, false);
        SceneState.instance.SetGlobalValue(LevelController.timeTrialKey, 0, false);

        bool hasCleared = false;

        ModalSaveSlots modalSlots = UIModalManager.instance.ModalGetController<ModalSaveSlots>("slots");
        for(int i = 0; i < modalSlots.slots.Length; i++) {
            if(SlotInfo.HasClearTime(i)) {
                hasCleared = true;
                break;
            }
        }

        if(hasCleared)
            activeGOCleared.SetActive(true);
        else
            activeGODefault.SetActive(true);
    }

    protected override void OnClose() {
    }

    void OnExit(GameObject go) {
        Application.Quit();
    }

    void OnPlay(GameObject go) {
        //Main.instance.sceneManager.LoadScene(Scenes.levelSelect);
        UIModalManager.instance.ModalOpen("slots");
    }

    void OnTrial(GameObject go) {
        UIModalManager.instance.ModalOpen("trial");
    }

    void OnOptions(GameObject go) {
        UIModalManager.instance.ModalOpen("options");
    }

    void OnCredits(GameObject go) {
        UIModalManager.instance.ModalOpen("credits");
    }
}
