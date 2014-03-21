using UnityEngine;
using System.Collections;

public class ModalTrial : UIController {
    [System.Serializable]
    public class Item {
        public UIEventListener l;
        public UILabel label;
        public UILabel time;

        private bool mLocked;
        public bool locked { get { return mLocked; } set { mLocked = value; } }
    }

    public Item[] items;
    public Color activeColor;

    public UIEventListener back;

    public GameObject waitGO;

    protected override void OnActive(bool active) {
        if(active) {
            waitGO.SetActive(true);

            UICamera.selectedObject = waitGO;

            StartCoroutine("DoWait");
        }
        else {
            waitGO.SetActive(false);

            for(int i = 0; i < items.Length; i++) {
                items[i].l.onClick = null;
            }

            back.onClick = null;

            StopCoroutine("DoWait");
        }
    }
    
    protected override void OnOpen() {
    }
    
    protected override void OnClose() {
    }

    void OnItemClick(GameObject go) {
        int ind = -1;
        for(int i = 0; i < items.Length; i++) {
            if(items[i].l.gameObject == go) {
                if(!items[i].locked)
                    ind = i;

                break;
            }
        }

        if(ind != -1) {
            SlotInfo.LoadTimeTrialData();
            SceneState.instance.SetGlobalValue(LevelController.timeTrialKey, 1, false);
            Main.instance.sceneManager.LoadScene(TimeTrial.instance.data[ind].level);
        }
    }

    void OnReturn(GameObject go) {
        UIModalManager.instance.ModalCloseTop();
    }

    IEnumerator DoWait() {
        yield return StartCoroutine(Achievement.instance.WaitServiceComplete());

        waitGO.SetActive(false);

        UICamera.selectedObject = items[0].l.gameObject;

        for(int i = 0; i < items.Length; i++) {
            items[i].l.onClick = OnItemClick;
            
            //check locked
            bool isLocked;
            if(TimeTrial.instance.data[i].requireUnlock) {
                if(!Achievement.instance.AchievementIsUnlocked(TimeTrial.instance.data[i].achieveId)) {
                    isLocked = true;

                    //go through slot data
                    ModalSaveSlots modalSlots = UIModalManager.instance.ModalGetController<ModalSaveSlots>("slots");
                    for(int s = 0; s < modalSlots.slots.Length; s++) {
                        SceneState.instance.ResetGlobalValues();
                        UserSlotData.LoadSlot(s, false);
                        if(LevelController.IsLevelComplete(TimeTrial.instance.data[i].level)) {
                            isLocked = false;
                            break;
                        }

                        UserSlotData.LoadSlot(-1, false);
                    }
                }
                else
                    isLocked = false;
            }
            else
                isLocked = false;
            
            if(isLocked) {
                items[i].label.text = "?????";
                items[i].label.color = Color.gray;
                items[i].time.text = "BEST - ---:--.--";

                items[i].locked = true;
            }
            else {
                items[i].label.text = TimeTrial.instance.data[i].name;
                items[i].label.color = activeColor;
                
                if(TimeTrial.Exists(TimeTrial.instance.data[i].level)) {
                    items[i].time.text = "BEST - " + LevelController.LevelTimeFormat(TimeTrial.Load(TimeTrial.instance.data[i].level));
                }
                else
                    items[i].time.text = "BEST - ---:--.--";

                items[i].locked = false;
            }
        }

        back.onClick = OnReturn;
    }
}
