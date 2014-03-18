using UnityEngine;
using System.Collections;

public class ModalStart : UIController {
    public UIEventListener play;
    public UIEventListener options;
    public UIEventListener credits;

    public GameObject activeGODefault;
    public GameObject activeGOCleared;

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
        SlotInfo.ClearCurrentSlotLoaded();
        ((UserSlotData)UserData.instance).SetSlot(-1, false);

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
