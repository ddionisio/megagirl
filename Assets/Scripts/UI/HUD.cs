using UnityEngine;
using System.Collections;

public class HUD : MonoBehaviour {
    public UIEnergyBar barHP;
    public UIEnergyBar barEnergy;
    public UIEnergyBar barBoss;
    public UILabel lifeCountLabel;
    public AnimatorData popUpMessage;

    private UILabel mPopUpMessageLabel;

    private static HUD mInstance;

    public static HUD instance { get { return mInstance; } }

    public void PopUpMessage(string text) {
        mPopUpMessageLabel.text = text;
        popUpMessage.Play("go");
    }

    public void RefreshLifeCount() {
        lifeCountLabel.text = PlayerStats.curLife.ToString();
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
    }

    // Use this for initialization
    void Start() {

    }
}
