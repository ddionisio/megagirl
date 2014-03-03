using UnityEngine;
using System.Collections;

public class UISlotInfo : MonoBehaviour {
    public const string portraitNormal = "levelSelectPortraits_gitGirl";
    public const string portraitHardcore = "levelSelectPortraits_gitGirlMad";

    public GameObject infoGO;
    public GameObject deleteGO;
    public GameObject newGO;

    public UISprite portrait;

    public GameObject[] weapons; //make sure it's in the same order as player's

    public UILabel heartsLabel;
    public UILabel eTankLabel;
    public UILabel wTankLabel;
    public UILabel clearTimeLabel;

    public UISprite armor;

    private UIEventListener mInfoEvent;
    private UIButtonKeys mInfoKeys;

    private UIEventListener mDeleteEvent;
    private UIButtonKeys mDeleteKeys;

    private UIEventListener mNewEvent;
    private UIButtonKeys mNewKeys;

    private bool mExists;

    public bool exists { get { return mExists; } }

    public UIEventListener infoEvent { get { return mInfoEvent; } }
    public UIButtonKeys infoKeys { get { return mInfoKeys; } }
    
    public UIEventListener deleteEvent { get { return mDeleteEvent; } }
    public UIButtonKeys deleteKeys { get { return mDeleteKeys; } }
    
    public UIEventListener newEvent { get { return mNewEvent; } }
    public UIButtonKeys newKeys { get { return mNewKeys; } }

	public void Init(int slot) {
        mExists = UserSlotData.IsSlotExist(slot);
        if(mExists) {
            infoGO.SetActive(true);
            deleteGO.SetActive(true);
            newGO.SetActive(false);

            switch(SlotInfo.GetGameMode(slot)) {
                case SlotInfo.GameMode.Hardcore:
                    portrait.spriteName = portraitHardcore;
                    break;

                default:
                    portrait.spriteName = portraitNormal;
                    break;
            }

            for(int i = 0; i < weapons.Length; i++) {
                weapons[i].SetActive(SlotInfo.WeaponIsUnlock(slot, i+1));
            }

            heartsLabel.text = "x" + SlotInfo.GetHeartCount(slot);

            int tankCount = 0;
            if(SlotInfo.IsSubTankEnergy1Acquired(slot)) tankCount++;
            if(SlotInfo.IsSubTankEnergy2Acquired(slot)) tankCount++;
            eTankLabel.text = "x" + tankCount;

            tankCount = 0;
            if(SlotInfo.IsSubTankWeapon1Acquired(slot)) tankCount++;
            if(SlotInfo.IsSubTankWeapon2Acquired(slot)) tankCount++;
            wTankLabel.text = "x" + tankCount;

            armor.color = SlotInfo.IsArmorAcquired(slot) ? Color.white : Color.black;

            clearTimeLabel.text = "CLEAR TIME: "+SlotInfo.GetClearTimeString(slot);
        }
        else {
            infoGO.SetActive(false);
            deleteGO.SetActive(false);
            newGO.SetActive(true);
        }
    }

    void Awake() {
        mInfoEvent = infoGO.GetComponent<UIEventListener>();
        mInfoKeys = infoGO.GetComponent<UIButtonKeys>();
        
        mDeleteEvent = deleteGO.GetComponent<UIEventListener>();
        mDeleteKeys = deleteGO.GetComponent<UIButtonKeys>();
        
        mNewEvent = newGO.GetComponent<UIEventListener>();
        mNewKeys = newGO.GetComponent<UIButtonKeys>();
    }
}
