using UnityEngine;
using System.Collections;

public class ModalPreStart : UIController {
    public void Begin() {
        //determine if there's an existing game
        if(PlayerStats.isGameExists) {
            UIModalManager.instance.ModalOpen("startContinue");
        }
        else {
            UIModalManager.instance.ModalOpen("startNew");
        }
    }

    protected override void OnActive(bool active) {
        if(active) {
        }
        else {
        }
    }

    protected override void OnOpen() {

    }

    protected override void OnClose() {
    }
}
