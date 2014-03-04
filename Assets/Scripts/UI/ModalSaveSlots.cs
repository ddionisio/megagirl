using UnityEngine;
using System.Collections;

public class ModalSaveSlots : UIController {
    public UISlotInfo[] slots;

    public UIEventListener back;

    private UIButtonKeys mBackKeys;
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
            back.onClick = null;
            CancelInvoke();
        }
    }

    void DoActive() {
        RefreshSlots();
        
        HookUpEvents(true);

        back.onClick = OnBack;
        
        if(mSelectedSlot == -1)
            mSelectedSlot = 0;
        
        UICamera.selectedObject = slots[mSelectedSlot].exists ? slots[mSelectedSlot].infoGO : slots[mSelectedSlot].newGO;
        
        NGUILayoutBase.RefreshNow(transform);

        mLateRefresh = false;
    }
    
    protected override void OnOpen() {
        UserSlotData.LoadSlot(-1, false);
    }
    
    protected override void OnClose() {
    }

    void Awake() {
        mBackKeys = back.GetComponent<UIButtonKeys>();
    }

    void OnInfoClick(GameObject go) {
        int slot = GetSlot(go);
        if(slot != -1) {
            mSelectedSlot = slot;

            if(SlotInfo.IsDead(slot)) {
                UIModalConfirm.Open(GameLocalize.GetText("dead_confirm_title"), GameLocalize.GetText("dead_confirm_desc"),
                                    delegate(bool yes) {
                    if(yes) {
                        SlotInfo.DeleteData(slot);
                        UserSlotData.DeleteSlot(slot);

                        SlotInfo.CreateSlot(ModalSaveSlots.selectedSlot, SlotInfo.GameMode.Hardcore);
                        SceneState.instance.ResetGlobalValues();
                        SceneState.instance.ResetValues();
                        Main.instance.sceneManager.LoadScene(Scenes.levelSelect);

                    }
                });
            }
            else {
                UserSlotData.LoadSlot(slot, true);
                SlotInfo.LoadCurrentSlotData();
                SceneState.instance.ResetGlobalValues();
                SceneState.instance.ResetValues();
                Main.instance.sceneManager.LoadScene(Scenes.levelSelect);
            }
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

    void OnBack(GameObject go) {
        UIModalManager.instance.ModalCloseTop();
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
                first.infoKeys.selectOnUp = mBackKeys;
                first.deleteKeys.selectOnUp = mBackKeys;
                mBackKeys.selectOnDown = first.infoKeys;
            }
            else {
                first.newKeys.selectOnUp = mBackKeys;
                mBackKeys.selectOnDown = first.newKeys;
            }

            if(last.exists) {
                last.infoKeys.selectOnDown = mBackKeys;
                last.deleteKeys.selectOnDown = mBackKeys;
                mBackKeys.selectOnUp = last.infoKeys;
            }
            else {
                last.newKeys.selectOnDown = mBackKeys;
                mBackKeys.selectOnUp = last.newKeys;
            }
        }
    }
}
