using UnityEngine;
using System.Collections;

public class UIPlayerInfo : MonoBehaviour {
    [System.Serializable]
    public class GameModeData {
        public string textRef;
        public Color color;
    }

    public GameModeData[] gameModes;

    public UILabel mode;
    public UISprite armor;
    public UILabel heartsFoundLabel;
    public UILabel subEnergyTanksFoundLabel;
    public UILabel subWeaponTanksFoundLabel;
    public UILabel livesLabel;

    private bool mStarted;

    void OnEnable() {
        int modeInd = (int)SlotInfo.gameMode;
        mode.text = GameLocalize.GetText(gameModes[modeInd].textRef);
        mode.color = gameModes[modeInd].color;
        
        armor.color = SlotInfo.isArmorAcquired ? Color.white : Color.black;

        //get hp mod counts
        heartsFoundLabel.text = string.Format("{0}/{1}", SlotInfo.heartCount, SlotInfo.hpModMaxCount);

        livesLabel.text = SlotInfo.gameMode == SlotInfo.GameMode.Hardcore ? string.Format("x{0:D2}", PlayerStats.curLife >= 0 ? PlayerStats.curLife - 1 : 0) : "--";

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
