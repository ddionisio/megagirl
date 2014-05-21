using UnityEngine;
using System.Collections;

public class AchievementRetroActive : MonoBehaviour {

    public void Apply() {
        StopAllCoroutines();
        StartCoroutine(DoIt());
    }

    IEnumerator DoIt() {
        Achievement achieve = Achievement.instance;

        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        //wait for services to be completed
        while(!achieve.isReady)
            yield return wait;

        //wait for user slot
        UserSlotData usd = UserData.instance as UserSlotData;
        if(usd) {
            while(usd.slot == -1)
                yield return wait;
        }

        //now for the checks

        if(LevelController.IsLevelComplete("level_katgirl"))
            achieve.NotifyUpdate(achieve.GetDataById(7041), 0, true);

        if(LevelController.IsLevelComplete("level_lightninggirl"))
            achieve.NotifyUpdate(achieve.GetDataById(7038), 0, true);

        if(LevelController.IsLevelComplete("level_tankgirl"))
            achieve.NotifyUpdate(achieve.GetDataById(7039), 0, true);

        if(LevelController.IsLevelComplete("level_valleygirl"))
            achieve.NotifyUpdate(achieve.GetDataById(7033), 0, true);

        if(LevelController.IsLevelComplete("level_clonegirl"))
            achieve.NotifyUpdate(achieve.GetDataById(7036), 0, true);

        if(LevelController.IsLevelComplete("level_hipster"))
            achieve.NotifyUpdate(achieve.GetDataById(7034), 0, true);

        if(LevelController.IsLevelComplete("level_final_0"))
            achieve.NotifyUpdate(achieve.GetDataById(7037), 0, true);

        if(LevelController.IsLevelComplete("level_final_1"))
            achieve.NotifyUpdate(achieve.GetDataById(7035), 0, true);

        if(LevelController.IsLevelComplete("level_final_2"))
            achieve.NotifyUpdate(achieve.GetDataById(7032), 0, true);

        if(LevelController.IsLevelComplete("level_final_boss_boss")) {
            achieve.NotifyUpdate(achieve.GetDataById(7040), 0, true);

            if(SlotInfo.GetGameMode(usd.slot) == SlotInfo.GameMode.Hardcore) {
                achieve.NotifyUpdate(achieve.GetDataById(7049), 0, true);

                if(PlayerStats.deathCount < 1) {
                    achieve.NotifyUpdate(achieve.GetDataById(7054), 0, true);
                }
            }
        }

        int heartFlags = SlotInfo.GetHeartFlags(usd.slot);
        int itemFlags = SlotInfo.GetItemsFlags(usd.slot);

        if(heartFlags == 255 && itemFlags == 31) {
            achieve.NotifyUpdate(achieve.GetDataById(7052), 0, true);
        }
    }
}
