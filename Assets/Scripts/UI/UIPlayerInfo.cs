using UnityEngine;
using System.Collections;

public class UIPlayerInfo : MonoBehaviour {
    public UILabel armorEnableLabel;
    public UILabel heartsFoundLabel;
    public UILabel subEnergyTanksFoundLabel;
    public UILabel subWeaponTanksFoundLabel;

    private bool mStarted;

    void OnEnable() {
        armorEnableLabel.text = PlayerStats.isArmorAcquired ? "ENABLED" : "DISABLED";

        //get hp mod counts
        int numHPMod = 0;
        int hpModFlags = SceneState.instance.GetGlobalValue(PlayerStats.hpModFlagsKey);
        for(int i = 0, check = 1; i < PlayerStats.hpModCount; i++, check <<= 1) {
            if((hpModFlags & check) != 0)
                numHPMod++;
        }

        heartsFoundLabel.text = string.Format("{0}/{1}", numHPMod, PlayerStats.hpModCount);

        //get sub energy counts
        int subECount = 0;
        if(PlayerStats.isSubTankEnergy1Acquired) subECount++;
        if(PlayerStats.isSubTankEnergy2Acquired) subECount++;

        subEnergyTanksFoundLabel.text = string.Format("{0}/{1}", subECount, 2);

        //get sub weapon counts
        int subWCount = 0;
        if(PlayerStats.isSubTankWeapon1Acquired) subWCount++;
        if(PlayerStats.isSubTankWeapon2Acquired) subWCount++;
        
        subWeaponTanksFoundLabel.text = string.Format("{0}/{1}", subWCount, 2);
    }

    void OnDisable() {
    }

	// Use this for initialization
	void Start () {
        mStarted = true;
        OnEnable();
	}
}
