using UnityEngine;
using System.Collections;

public class ModalMessage : UIController {
    public UIEventListener click;

    protected override void OnActive(bool active) {
        if(active) {
            UICamera.selectedObject = click.gameObject;
            click.onClick = OnClick;
        }
        else {
            click.onClick = null;
        }
    }

    protected override void OnOpen() {
    }

    protected override void OnClose() {
    }

    void OnClick(GameObject go) {
        UIModalManager.instance.ModalCloseTop();
    }
}
