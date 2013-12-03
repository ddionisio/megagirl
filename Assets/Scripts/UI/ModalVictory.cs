using UnityEngine;
using System.Collections;

public class ModalVictory : UIController {
    //NOTE: these are set by Enemy based on weaponIndexUnlock
    public static string sWeaponIconRef;
    public static string sWeaponTitleRef;

    public UIEventListener click;

    public UILabel title;
    public UILabel desc;
    public UISprite icon;
    //public float iconWidth = 24;
    //public float iconHeight = 24;

    protected override void OnActive(bool active) {
        if(active) {
            UICamera.selectedObject = click.gameObject;
            click.onClick = OnClick;
        }
        else {
            click.onClick = null;
        }
    }
    
    protected override void OnOpen() {
        if(!string.IsNullOrEmpty(sWeaponTitleRef)) {
            title.text = GameLocalize.GetText(sWeaponTitleRef);
            desc.text = GameLocalize.GetText(sWeaponTitleRef+"_desc");
        }
        if(!string.IsNullOrEmpty(sWeaponIconRef)) icon.spriteName = sWeaponIconRef;

        NGUILayoutBase.RefreshNow(transform);
    }
    
    protected override void OnClose() {
    }

    void OnClick(GameObject go) {
        Main.instance.sceneManager.LoadScene(Scenes.levelSelect);
    }
}
