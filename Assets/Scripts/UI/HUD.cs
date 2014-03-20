using UnityEngine;
using System.Collections;

public class HUD : MonoBehaviour {
    public const string gitgirlNameRef = "gitgirl_name";
    public const string gitgirlPortraitRef = "dialog_portrait_gitgirl";

    public UIEnergyBar barHP;
    public UIEnergyBar barEnergy;
    public UIEnergyBar barBoss;
    public UILabel lifeCountLabel;
    public AnimatorData popUpMessage;
    public UILabel timeLabel;

    private UILabel mPopUpMessageLabel;

    private static HUD mInstance;

    public static HUD instance { get { return mInstance; } }

    public void PopUpMessage(string text) {
        mPopUpMessageLabel.text = text;
        popUpMessage.Play("go");
    }

    public void RefreshLifeCount() {
        lifeCountLabel.text = (PlayerStats.curLife - 1).ToString("D2");
    }

    void OnDestroy() {
        if(mInstance == this) {
            mInstance = null;
        }
    }

    void Awake() {
        if(mInstance == null) {
            mInstance = this;
        }

        UILabel[] popLabels = popUpMessage.GetComponentsInChildren<UILabel>(true);
        mPopUpMessageLabel = popLabels[0];

        if(LevelController.isTimeTrial) {
            timeLabel.gameObject.SetActive(true);
            lifeCountLabel.gameObject.SetActive(false);
        }
        else if(SlotInfo.gameMode != SlotInfo.GameMode.Hardcore) {
            lifeCountLabel.gameObject.SetActive(false);
        }
    }
}
