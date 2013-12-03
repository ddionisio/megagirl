using UnityEngine;
using System.Collections;

public class ModalPause : UIController {
    public Transform weaponsHolder;

    public UIEnergyBar hpBar;

    public UISprite gitGirl;
    public UILabel lifeLabel;

    public GameObject energySubTankBar1;
    public UISprite energySubTankBar1Fill;

    public GameObject energySubTankBar2;
    public UISprite energySubTankBar2Fill;

    public GameObject weaponSubTankBar1;
    public UISprite weaponSubTankBar1Fill;

    public GameObject weaponSubTankBar2;
    public UISprite weaponSubTankBar2Fill;

    public UIEventListener energySubTank;
    public UIEventListener weaponSubTank;

    public UIEventListener exit;
    public UIEventListener options;

    private UIEnergyBar[] mWeapons;

    private int mInputLockCounter;
    private int mNumEnergyTank;
    private int mNumWeaponTank;

    private int mEnergySubTankBarFillW;
    private int mWeaponSubTankBarFillW;

    protected override void OnActive(bool active) {
        if(active) {
            InitHP();
            InitSubTanks();
            InitWeapons();

            //life
            lifeLabel.text = "x" + PlayerStats.curLife;

            exit.onClick = OnExitClick;
            options.onClick = OnOptionsClick;

            Main.instance.input.AddButtonCall(0, InputAction.MenuEscape, OnInputEscape);
        }
        else {
            for(int i = 0, max = mWeapons.Length; i < max; i++) {
                mWeapons[i].current = 0;
                mWeapons[i].RefreshBars();

                UIEventListener eventListener = mWeapons[i].GetComponent<UIEventListener>();
                eventListener.onClick = null;
                eventListener.onSelect = null;
            }

            energySubTank.onClick = null;
            weaponSubTank.onClick = null;

            exit.onClick = null;
            options.onClick = null;

            mInputLockCounter = 0;

            Main.instance.input.RemoveButtonCall(0, InputAction.MenuEscape, OnInputEscape);
        }
    }

    protected override void OnOpen() {

    }

    protected override void OnClose() {

    }

    void Awake() {
        mWeapons = weaponsHolder.GetComponentsInChildren<UIEnergyBar>(true);
        System.Array.Sort(mWeapons,
            delegate(UIEnergyBar bar1, UIEnergyBar bar2) {
                return bar1.name.CompareTo(bar2.name);
            });

        for(int i = 0, max = mWeapons.Length; i < max; i++) {
            UIEnergyBar wpnUI = mWeapons[i];
            wpnUI.current = 0;
            wpnUI.RefreshBars();
            wpnUI.animateEndCallback += OnEnergyAnimStop;
        }

        hpBar.animateEndCallback += OnEnergyAnimStop;

        mEnergySubTankBarFillW = energySubTankBar1Fill.width;
        mWeaponSubTankBarFillW = weaponSubTankBar1Fill.width;
    }

    void InitSubTanks() {
        UIButtonKeys energyBtnKeys = energySubTank.GetComponent<UIButtonKeys>();
        UIButtonKeys weaponBtnKeys = weaponSubTank.GetComponent<UIButtonKeys>();
        UIButtonKeys exitBtnKeys = exit.GetComponent<UIButtonKeys>();
        UIButtonKeys optionsBtnKeys = options.GetComponent<UIButtonKeys>();

        mNumEnergyTank = 0;
        if(PlayerStats.isSubTankEnergy1Acquired) mNumEnergyTank++;
        if(PlayerStats.isSubTankEnergy2Acquired) mNumEnergyTank++;

        if(mNumEnergyTank > 0) {
            energySubTank.onClick = OnEnergySubTankClick;
            energySubTankBar1.SetActive(mNumEnergyTank >= 1);
            energySubTankBar2.SetActive(mNumEnergyTank > 1);

            RefreshEnergyTank();
        }
        else {
            energySubTankBar1.SetActive(false);
            energySubTankBar2.SetActive(false);
        }

        mNumWeaponTank = 0;
        if(PlayerStats.isSubTankWeapon1Acquired) mNumWeaponTank++;
        if(PlayerStats.isSubTankWeapon2Acquired) mNumWeaponTank++;

        if(mNumWeaponTank > 0) {
            weaponSubTank.onClick = OnWeaponSubTankClick;
            weaponSubTankBar1.SetActive(mNumWeaponTank >= 1);
            weaponSubTankBar2.SetActive(mNumWeaponTank > 1);

            RefreshWeaponTank();
        }
        else {
            weaponSubTankBar1.SetActive(false);
            weaponSubTankBar2.SetActive(false);
        }

        energyBtnKeys.selectOnDown = mNumWeaponTank > 0 ? weaponBtnKeys : exitBtnKeys;

        weaponBtnKeys.selectOnUp = mNumEnergyTank > 0 ? energyBtnKeys : optionsBtnKeys;

        exitBtnKeys.selectOnUp =
            mNumWeaponTank > 0 ? weaponBtnKeys :
                mNumEnergyTank > 0 ? energyBtnKeys :
                    optionsBtnKeys;

        optionsBtnKeys.selectOnDown =
            mNumEnergyTank > 0 ? energyBtnKeys :
                mNumWeaponTank > 0 ? weaponBtnKeys :
                    exitBtnKeys;
    }

    void InitWeapons() {
        Player player = Player.instance;

        UIButtonKeys firstWeaponButtonKeys = null;
        UIButtonKeys lastWeaponButtonKeys = null;
        UIButtonKeys rightButtonKeys = null;

        if(PlayerStats.isSubTankEnergy1Acquired || PlayerStats.isSubTankEnergy2Acquired)
            rightButtonKeys = energySubTank.GetComponent<UIButtonKeys>();
        else if(PlayerStats.isSubTankWeapon1Acquired || PlayerStats.isSubTankWeapon2Acquired)
            rightButtonKeys = weaponSubTank.GetComponent<UIButtonKeys>();
        else
            rightButtonKeys = exit.GetComponent<UIButtonKeys>();

        for(int i = 0, max = mWeapons.Length; i < max; i++) {
            UIEnergyBar wpnUI = mWeapons[i];
            Weapon wpn = i < player.weapons.Length ? player.weapons[i] : null;

            UIEventListener eventListener = wpnUI.GetComponent<UIEventListener>();

            if(Weapon.IsAvailable(i) && wpn) {
                wpnUI.gameObject.SetActive(true);
                wpnUI.label.text = wpn.labelText;
                wpnUI.SetIconSprite(wpn.iconSpriteRef);

                wpnUI.max = Mathf.CeilToInt(Weapon.weaponEnergyDefaultMax);
                wpnUI.current = wpn.energyType == Weapon.EnergyType.Unlimited ? wpnUI.max : Mathf.CeilToInt(wpn.currentEnergy);

                eventListener.onClick = OnWeaponClick;
                eventListener.onSelect = OnWeaponSelect;

                UIButtonKeys buttonKeys = wpnUI.GetComponent<UIButtonKeys>();

                buttonKeys.selectOnUp = lastWeaponButtonKeys;
                buttonKeys.selectOnRight = rightButtonKeys;

                if(firstWeaponButtonKeys == null)
                    firstWeaponButtonKeys = buttonKeys;

                if(lastWeaponButtonKeys)
                    lastWeaponButtonKeys.selectOnDown = buttonKeys;

                lastWeaponButtonKeys = buttonKeys;
            }
            else {
                wpnUI.gameObject.SetActive(false);

                eventListener.onClick = null;

                UIButtonKeys buttonKeys = wpnUI.GetComponent<UIButtonKeys>();
                buttonKeys.selectOnUp = null;
                buttonKeys.selectOnDown = null;
            }
        }

        if(firstWeaponButtonKeys) {
            firstWeaponButtonKeys.selectOnUp = lastWeaponButtonKeys;
        }

        if(lastWeaponButtonKeys) {
            lastWeaponButtonKeys.selectOnDown = firstWeaponButtonKeys;
        }
    }

    void InitHP() {
        hpBar.max = Mathf.CeilToInt(Player.instance.stats.maxHP);
        hpBar.current = Mathf.CeilToInt(Player.instance.stats.curHP);
    }

    void DoTankFill(UISprite bar1, UISprite bar2, int barWidth, int amt) {
        if(amt > 0) {
            bar1.gameObject.SetActive(true);

            if(amt > barWidth) {
                bar1.width = barWidth;

                bar2.gameObject.SetActive(true);
                bar2.width = amt - barWidth;
            }
            else {
                bar1.width = amt;
                bar2.gameObject.SetActive(false);
            }
        }
        else {
            bar1.gameObject.SetActive(false);
            bar2.gameObject.SetActive(false);
        }
    }

    void RefreshEnergyTank() {
        Player player = Player.instance;
        DoTankFill(energySubTankBar1Fill, energySubTankBar2Fill, mEnergySubTankBarFillW, Mathf.RoundToInt(player.stats.subTankEnergyCurrent));
    }

    void RefreshWeaponTank() {
        Player player = Player.instance;
        DoTankFill(weaponSubTankBar1Fill, weaponSubTankBar2Fill, mWeaponSubTankBarFillW, Mathf.RoundToInt(player.stats.subTankWeaponCurrent));
    }

    void OnWeaponClick(GameObject go) {
        if(mInputLockCounter > 0)
            return;

        for(int i = 0, max = mWeapons.Length; i < max; i++) {
            if(mWeapons[i].gameObject == go) {
                //unpause?
                Player.instance.currentWeaponIndex = i;

                UIModalManager.instance.ModalCloseTop();
                break;
            }
        }
    }

    void OnWeaponSelect(GameObject go, bool state) {
        for(int i = 0, max = mWeapons.Length; i < max; i++) {
            if(mWeapons[i].gameObject == go) {
                //unpause?
                Weapon wpn = Player.instance.weapons[i];
                if(wpn && !string.IsNullOrEmpty(wpn.gitGirlSpriteRef)) {
                    gitGirl.spriteName = wpn.gitGirlSpriteRef;
                    gitGirl.MakePixelPerfect();
                }
                break;
            }
        }
    }

    void OnEnergySubTankClick(GameObject go) {
        if(mInputLockCounter > 0)
            return;

        PlayerStats stats = Player.instance.stats;

        float amtTank = stats.subTankEnergyCurrent;
        float amtNeeded = stats.maxHP - stats.curHP;

        if(amtTank > 0.0f && amtNeeded > 0.0f) {
            if(amtNeeded > amtTank) {
                stats.curHP += amtTank;
                stats.subTankEnergyCurrent = 0.0f;
            }
            else {
                stats.curHP = stats.maxHP;
                stats.subTankEnergyCurrent -= amtNeeded;
            }

            mInputLockCounter++;
            hpBar.currentSmooth = Mathf.CeilToInt(stats.curHP);

            RefreshEnergyTank();
        }
    }

    void OnWeaponSubTankClick(GameObject go) {
        if(mInputLockCounter > 0)
            return;

        Player player = Player.instance;
        Weapon lowestWpn = player.lowestEnergyWeapon;

        float amtTank = player.stats.subTankWeaponCurrent;

        if(amtTank > 0.0f && lowestWpn != null) {

            float amtNeeded = Weapon.weaponEnergyDefaultMax - lowestWpn.currentEnergy;

            if(amtNeeded > 0.0f) {
                //fill all weapons
                if(amtNeeded > amtTank) {
                    for(int i = 0, max = mWeapons.Length; i < max; i++) {
                        Weapon wpn = player.weapons[i];
                        if(wpn && !wpn.isMaxEnergy) {
                            wpn.currentEnergy += amtTank;

                            mInputLockCounter++;
                            mWeapons[i].currentSmooth = Mathf.CeilToInt(wpn.currentEnergy);
                        }
                    }

                    player.stats.subTankWeaponCurrent = 0.0f;
                }
                else {
                    for(int i = 0, max = mWeapons.Length; i < max; i++) {
                        Weapon wpn = player.weapons[i];
                        if(wpn && !wpn.isMaxEnergy) {
                            wpn.currentEnergy = Weapon.weaponEnergyDefaultMax;

                            mInputLockCounter++;
                            mWeapons[i].currentSmooth = Mathf.CeilToInt(wpn.currentEnergy);
                        }
                    }

                    player.stats.subTankWeaponCurrent -= amtNeeded;
                }

                RefreshWeaponTank();
            }
        }
    }

    void OnExitClick(GameObject go) {
        if(mInputLockCounter > 0)
            return;

        UIModalConfirm.Open(
            GameLocalize.GetText("exit_confirm_title"), GameLocalize.GetText("exit_confirm_desc"),
            delegate(bool yes) {
            if(yes)
                Main.instance.sceneManager.LoadScene(Scenes.levelSelect);
        });
    }

    void OnOptionsClick(GameObject go) {
        if(mInputLockCounter > 0)
            return;

        UIModalManager.instance.ModalOpen("options");
    }

    void OnInputEscape(InputManager.Info data) {
        if(mInputLockCounter > 0)
            return;

        if(data.state == InputManager.State.Pressed) {
            UIModalManager.instance.ModalCloseTop();
        }
    }

    void OnEnergyAnimStop(UIEnergyBar bar) {
        mInputLockCounter--;
    }
}
