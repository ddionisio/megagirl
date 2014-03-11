using UnityEngine;
using System.Collections;

public class EndComputeClearTime : MonoBehaviour {
    public UILabel label;

    public void RefreshClearTime() {
        label.text = string.Format(GameLocalize.GetText("cleartime"), SlotInfo.GetClearTimeString(UserSlotData.currentSlot));

        if(SlotInfo.HasClearTime(UserSlotData.currentSlot)) {
            if(SlotInfo.gameMode == SlotInfo.GameMode.Hardcore) {
                Leaderboard.instance.PostScore("Clear Time Iron Maiden", Mathf.RoundToInt(SlotInfo.GetClearTime(UserSlotData.currentSlot)*1000.0f));
            }
            else if(SlotInfo.gameMode == SlotInfo.GameMode.Normal) {
                Leaderboard.instance.PostScore("Clear Time", Mathf.RoundToInt(SlotInfo.GetClearTime(UserSlotData.currentSlot)*1000.0f));
            }
        }
    }
}
