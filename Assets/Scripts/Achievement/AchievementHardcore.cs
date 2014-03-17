using UnityEngine;
using System.Collections;

public class AchievementHardcore : AchievementNotifier {
    public float delay = 2.0f;
    
    // Use this for initialization
    void Start() {
        if(SlotInfo.gameMode == SlotInfo.GameMode.Hardcore) {
            Notify();
        }
    }
}
