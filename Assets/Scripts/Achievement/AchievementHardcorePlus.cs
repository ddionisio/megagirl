using UnityEngine;
using System.Collections;

public class AchievementHardcorePlus : AchievementNotifier {
    public float delay = 2.0f;
    
    // Use this for initialization
    void Start() {
        if(SlotInfo.gameMode == SlotInfo.GameMode.Hardcore 
           && PlayerStats.deathCount == 0 
           && SlotInfo.GetHeartFlags(UserSlotData.currentSlot) == 0
           && SlotInfo.GetItemsFlags(UserSlotData.currentSlot) == 0) {
            Notify();
        }
    }
}
