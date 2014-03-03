using UnityEngine;
using System.Collections;

public class ModalNewGame : UIController {
    [System.Serializable]
    public class Data {
        public UIEventListener btn;
        public string infoRef;
        public SlotInfo.GameMode mode;
    }

    public Data[] data;
    public int startIndex = 0;

    public UILabel infoLabel;

    protected override void OnActive(bool active) {
        if(active) {
            for(int i = 0; i < data.Length; i++) {
                data[i].btn.onSelect = OnSelect;
                data[i].btn.onClick = OnClick;
            }

            UICamera.selectedObject = data[startIndex].btn.gameObject;
            infoLabel.text = GameLocalize.GetText(data[startIndex].infoRef);
            NGUILayoutBase.RefreshNow(transform);
        }
        else {
            for(int i = 0; i < data.Length; i++) {
                data[i].btn.onSelect = null;
                data[i].btn.onClick = null;
            }
        }
    }

    protected override void OnOpen() {
    }
    
    protected override void OnClose() {
    }

    int GetIndex(GameObject go) {
        for(int i = 0; i < data.Length; i++) {
            if(go == data[i].btn.gameObject)
                return i;
        }
        return -1;
    }

    void OnSelect(GameObject go, bool state) {
        if(state) {
            int ind = GetIndex(go);
            if(ind != -1) {
                infoLabel.text = GameLocalize.GetText(data[ind].infoRef);
                NGUILayoutBase.RefreshNow(transform);
            }
        }
    }

    void OnClick(GameObject go) {
        int ind = GetIndex(go);
        if(ind != -1) {
            SlotInfo.CreateSlot(ModalSaveSlots.selectedSlot, data[ind].mode);
            Main.instance.sceneManager.LoadScene(Scenes.levelSelect);
        }
    }
}
