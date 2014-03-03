using UnityEngine;
using System.Collections;

public class ModalSaveSlots : UIController {
    public UISlotInfo[] slots;

    private static int mSelectedSlot = -1;
    private bool mLateRefresh = false;

    public static int selectedSlot { get { return mSelectedSlot; } }

    protected override void OnActive(bool active) {
        if(active) {
            if(mLateRefresh) {
                Invoke("DoActive", 0.1f);
            }
            else {
                DoActive();
            }
        }
        else {
            HookUpEvents(false);
            CancelInvoke();
        }
    }

    void DoActive() {
        RefreshSlots();
        
        HookUpEvents(true);
        
        if(mSelectedSlot == -1)
            mSelectedSlot = 0;
        
        UICamera.selectedObject = slots[mSelectedSlot].exists ? slots[mSelectedSlot].infoGO : slots[mSelectedSlot].newGO;
        
        NGUILayoutBase.RefreshNow(transform);

        mLateRefresh = false;
    }
    
    protected override void OnOpen() {
    }
    
    protected override void OnClose() {
    }

    void OnInfoClick(GameObject go) {
        int slot = GetSlot(go);
        if(slot != -1) {
            mSelectedSlot = slot;
            UserSlotData.LoadSlot(slot, false);
            Main.instance.sceneManager.LoadScene(Scenes.levelSelect);
        }
    }

    void OnDeleteClick(GameObject go) {
        int slot = GetSlot(go);
        if(slot != -1) {
            mLateRefresh = true;

            mSelectedSlot = slot;

            UIModalConfirm.Open(GameLocalize.GetText("delete_confirm_title"), GameLocalize.GetText("delete_confirm_desc"),
                                delegate(bool yes) {
                if(yes) {
                    SlotInfo.DeleteData(slot);
                    UserSlotData.DeleteSlot(slot);
                }
                               });
        }
    }

    void OnNewClick(GameObject go) {
        int slot = GetSlot(go);
        if(slot != -1) {
            mSelectedSlot = slot;
            UIModalManager.instance.ModalOpen("newGame");
        }
    }

    private int GetSlot(GameObject go) {
        for(int i = 0; i < slots.Length; i++) {
            if(go == slots[i].newGO || go == slots[i].deleteGO || go == slots[i].infoGO) {
                return i;
            }
        }
        return -1;
    }

    private void HookUpEvents(bool yes) {
        if(yes) {
            for(int i = 0; i < slots.Length; i++) {
                if(slots[i].exists) {
                    slots[i].infoEvent.onClick = OnInfoClick;
                    slots[i].deleteEvent.onClick = OnDeleteClick;
                }
                else {
                    slots[i].newEvent.onClick = OnNewClick;
                }
            }
        }
        else {
            for(int i = 0; i < slots.Length; i++) {
                slots[i].infoEvent.onClick = null;
                slots[i].deleteEvent.onClick = null;
                slots[i].newEvent.onClick = null;
            }
        }
    }

    private void RefreshSlots() {
        UISlotInfo prev = null;

        for(int i = 0; i < slots.Length; i++) {
            slots[i].Init(i);

            if(prev) {
                if(slots[i].exists) {
                    if(prev.exists) {
                        prev.infoKeys.selectOnDown = slots[i].infoKeys;
                        prev.deleteKeys.selectOnDown = slots[i].deleteKeys;
                        slots[i].infoKeys.selectOnUp = prev.infoKeys;
                        slots[i].deleteKeys.selectOnUp = prev.deleteKeys;
                    }
                    else {
                        prev.newKeys.selectOnDown = slots[i].infoKeys;
                        slots[i].infoKeys.selectOnUp = prev.newKeys;
                        slots[i].deleteKeys.selectOnUp = prev.newKeys;
                    }
                }
                else {
                    if(prev.exists) {
                        prev.infoKeys.selectOnDown = slots[i].newKeys;
                        prev.deleteKeys.selectOnDown = slots[i].newKeys;
                        slots[i].newKeys.selectOnUp = prev.infoKeys;
                    }
                    else {
                        prev.newKeys.selectOnDown = slots[i].newKeys;
                        slots[i].newKeys.selectOnUp = prev.newKeys;
                    }
                }
            }

            prev = slots[i];
        }

        UISlotInfo first = slots[0];
        UISlotInfo last = slots[slots.Length - 1];
        if(first != last) {
            if(first.exists) {
                if(last.exists) {
                    last.infoKeys.selectOnDown = first.infoKeys;
                    last.deleteKeys.selectOnDown = first.deleteKeys;
                    first.infoKeys.selectOnUp = last.infoKeys;
                    first.deleteKeys.selectOnUp = last.deleteKeys;
                }
                else {
                    last.newKeys.selectOnDown = first.infoKeys;
                    first.infoKeys.selectOnUp = last.newKeys;
                    first.deleteKeys.selectOnUp = last.newKeys;
                }
            }
            else {
                if(last.exists) {
                    last.infoKeys.selectOnDown = first.newKeys;
                    last.deleteKeys.selectOnDown = first.newKeys;
                    first.newKeys.selectOnUp = last.infoKeys;
                }
                else {
                    last.newKeys.selectOnDown = first.newKeys;
                    first.newKeys.selectOnUp = last.newKeys;
                }
            }
        }
    }
}
