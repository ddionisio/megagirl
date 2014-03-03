using UnityEngine;
using System.Collections;

public class UIPlayerInfo : MonoBehaviour {
    public UILabel armorEnableLabel;
    public UILabel heartsFoundLabel;
    public UILabel subEnergyTanksFoundLabel;
    public UILabel subWeaponTanksFoundLabel;

    private bool mStarted;

    void OnEnable() {
        armorEnableLabel.text = SlotInfo.isArmorAcquired ? "ENABLED" : "DISABLED";

        //get hp mod counts
        heartsFoundLabel.text = string.Format("{0}/{1}", SlotInfo.heartCount, SlotInfo.hpModMaxCount);

        //get sub energy counts
        int subECount = 0;
        if(SlotInfo.isSubTankEnergy1Acquired) subECount++;
        if(SlotInfo.isSubTankEnergy2Acquired) subECount++;

        subEnergyTanksFoundLabel.text = string.Format("{0}/{1}", subECount, 2);

        //get sub weapon counts
        int subWCount = 0;
        if(SlotInfo.isSubTankWeapon1Acquired) subWCount++;
        if(SlotInfo.isSubTankWeapon2Acquired) subWCount++;
        
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
