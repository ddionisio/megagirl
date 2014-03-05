using UnityEngine;
using System.Collections;

public class AchievementAllUpgrades : AchievementNotifier {

    protected override void Awake() {
        base.Awake();

        ItemPickup itm = GetComponent<ItemPickup>();
        itm.pickupCallback += OnPickUp;
    }

    void OnPickUp(ItemPickup itm) {
        int heartFlags = SlotInfo.GetHeartFlags(UserSlotData.currentSlot);
        int itemFlags = SlotInfo.GetItemsFlags(UserSlotData.currentSlot);

        if(heartFlags == 255 && itemFlags == 31) {
            Notify();
        }
    }
}
