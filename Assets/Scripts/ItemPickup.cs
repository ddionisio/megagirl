using UnityEngine;
using System.Collections;

public class ItemPickup : EntityBase {
    public const float destroyDelay = 4.0f;
    public const float destroyStartBlinkDelay = destroyDelay * 0.7f;

    public ItemType type;
    public int bit; //which bit in the flag, used by certain types
    public bool savePickUp; //if true, sets flag in pickupBit to avoid spawning item when starting level over after death
    public int pickupBit; //make sure not to set >= 30
    public float value; //used by certain types
    public string sound;
    public string popTextRef;

    //if not empty, use this instead
    public string[] dialogTextRefs;

    public LayerMask dropLayerMask; //which layers the drop will stop when hit
    public float dropSpeed;

    public string sfxId;

    private bool mDropActive;
    private float mRadius;
    private bool mSpawned;

    private SpriteColorBlink[] mBlinkers;

    private bool mPicked;
    private int mCurDialogTextInd;

    /// <summary>
    /// Force pickup
    /// </summary>
    public void PickUp(Player player) {
        if(player && player.state != (int)EntityState.Dead && player.state != (int)EntityState.Invalid && gameObject.activeInHierarchy) {
            float val = value;

            switch(type) {
                case ItemType.Health:
                    if(player.stats.curHP < player.stats.maxHP) {
                        if(player.stats.curHP + val > player.stats.maxHP) {
                            player.stats.subTankEnergyCurrent += (player.stats.curHP + val) - player.stats.maxHP;
                        }

                        player.stats.curHP += val;
                    }
                    else {
                        float curTankAmt = player.stats.subTankEnergyCurrent;
                        player.stats.subTankEnergyCurrent += val;

                        if(curTankAmt < player.stats.subTankEnergyCurrent)
                            SoundPlayerGlobal.instance.Play(sfxId);
                    }
                    break;

                case ItemType.Energy:
                    Weapon wpn = null;
                    if(player.currentWeaponIndex == 0 || player.currentWeapon.isMaxEnergy) {
                        wpn = player.lowestEnergyWeapon;
                    }
                    else
                        wpn = player.currentWeapon;

                    if(wpn && !wpn.isMaxEnergy) {
                        if(wpn.currentEnergy + val > Weapon.weaponEnergyDefaultMax) {
                            player.stats.subTankWeaponCurrent += (wpn.currentEnergy + val) - Weapon.weaponEnergyDefaultMax;
                        }

                        wpn.currentEnergy += val;

                        if(wpn != player.currentWeapon)
                            SoundPlayerGlobal.instance.Play(sfxId);
                    }
                    else {
                        float curTankAmt = player.stats.subTankWeaponCurrent;
                        player.stats.subTankWeaponCurrent += val;

                        if(curTankAmt < player.stats.subTankWeaponCurrent)
                            SoundPlayerGlobal.instance.Play(sfxId);
                    }
                    break;

                case ItemType.Life:
                    PlayerStats.curLife++;
                    HUD.instance.RefreshLifeCount();
                    SoundPlayerGlobal.instance.Play(sfxId);
                    break;

                case ItemType.HealthUpgrade:
                    PlayerStats.AddHPMod(bit);
                    SoundPlayerGlobal.instance.Play(sfxId);
                    break;

                case ItemType.EnergyTank:
                    if(bit == 0)
                        player.stats.AcquireSubTankEnergy1();
                    else
                        player.stats.AcquireSubTankEnergy2();

                    SoundPlayerGlobal.instance.Play(sfxId);
                    break;

                case ItemType.WeaponTank:
                    if(bit == 0)
                        player.stats.AcquireSubTankWeapon1();
                    else
                        player.stats.AcquireSubTankWeapon2();

                    SoundPlayerGlobal.instance.Play(sfxId);
                    break;

                case ItemType.Armor:
                    player.stats.AcquireArmor();
                    player.RefreshArmor();

                    SoundPlayerGlobal.instance.Play(sfxId);
                    break;
            }

            if(savePickUp) {
                SceneState.instance.SetGlobalFlag(LevelController.levelPickupBitState, pickupBit, true, false);
            }

            if(!string.IsNullOrEmpty(sound)) {
                SoundPlayerGlobal.instance.Play(sound);
            }

            if(collider)
                collider.enabled = false;

            if(dialogTextRefs != null && dialogTextRefs.Length > 0) {
                mCurDialogTextInd = 0;
                UIModalCharacterDialog dlg = UIModalCharacterDialog.Open(true, UIModalCharacterDialog.defaultModalRef, 
                                                                         dialogTextRefs[mCurDialogTextInd], HUD.gitgirlNameRef, HUD.gitgirlPortraitRef, null);
                dlg.actionCallback += OnDialogAction;

            }
            else {
                if(!string.IsNullOrEmpty(popTextRef))
                    HUD.instance.PopUpMessage(GameLocalize.GetText(popTextRef));

                Release();
            }
        }
    }

    void OnDialogAction(UIModalCharacterDialog dlg, int choiceInd) {
        mCurDialogTextInd++;
        if(mCurDialogTextInd == dialogTextRefs.Length) {
            Release();
            dlg.actionCallback -= OnDialogAction;
            UIModalManager.instance.ModalCloseTop(); //assume dialog is top
        }
        else {
            dlg.Apply(true, dialogTextRefs[mCurDialogTextInd], HUD.gitgirlNameRef, HUD.gitgirlPortraitRef, null);
        }
    }

    void OnTriggerEnter(Collider col) {
        if(!mPicked) {
            Player player = col.GetComponent<Player>();
            if(player)
                PickUp(player);

            mPicked = true;
        }
    }

    protected override void ActivatorSleep() {
        base.ActivatorSleep();

        if(mSpawned) {
            Release();
        }
    }

    protected override void OnDespawned() {
        //reset stuff here
        if(collider)
            collider.enabled = true;

        mDropActive = false;

        foreach(SpriteColorBlink blinker in mBlinkers) {
            blinker.enabled = false;
        }

        CancelInvoke();

        base.OnDespawned();
    }

    protected override void OnSpawned() {
        activator.deactivateOnStart = false;
        mSpawned = true;
        mPicked = false;

        base.OnSpawned();
    }

    protected override void OnDestroy() {
        //dealloc here

        base.OnDestroy();
    }

    public override void SpawnFinish() {
        //start ai, player control, etc
        StartCoroutine(DoDrop());
    }

    protected override void SpawnStart() {
        //initialize some things
    }

    protected override void Awake() {
        base.Awake();

        //initialize variables
        if(collider) {
            SphereCollider sphr = collider as SphereCollider;
            if(sphr) {
                mRadius = sphr.radius;
            }
            else {
                mRadius = collider.bounds.extents.y;
            }
        }

        mBlinkers = GetComponentsInChildren<SpriteColorBlink>(true);
        foreach(SpriteColorBlink blinker in mBlinkers) {
            blinker.enabled = false;
        }

        autoSpawnFinish = true;
    }

    // Use this for initialization
    protected override void Start() {
        base.Start();

        //initialize variables from other sources (for communicating with managers, etc.)

        //check if we are already collected, depending on type
        bool doDisable = false;

        switch(type) {
            case ItemType.HealthUpgrade:
                doDisable = PlayerStats.IsHPModAcquired(bit);
                break;

            case ItemType.EnergyTank:
                if(bit == 0)
                    doDisable = PlayerStats.isSubTankEnergy1Acquired;
                else
                    doDisable = PlayerStats.isSubTankEnergy2Acquired;
                break;

            case ItemType.WeaponTank:
                if(bit == 0)
                    doDisable = PlayerStats.isSubTankWeapon1Acquired;
                else
                    doDisable = PlayerStats.isSubTankWeapon2Acquired;
                break;

            case ItemType.Armor:
                doDisable = PlayerStats.isArmorAcquired;
                break;

            default:
                if(savePickUp) {
                    doDisable = SceneState.instance.CheckGlobalFlag(LevelController.levelPickupBitState, pickupBit);
                }
                break;
        }

        if(doDisable) {
            if(activator)
                activator.ForceActivate();
            gameObject.SetActive(false);
        }
    }

    IEnumerator DoDrop() {
        mDropActive = true;

        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        while(mDropActive) {
            yield return wait;

            float moveY = dropSpeed * Time.fixedDeltaTime;
            Vector3 pos = transform.position;

            RaycastHit hit;
            if(Physics.SphereCast(collider.bounds.center, mRadius, Vector3.down, out hit, moveY, dropLayerMask)) {
                pos = hit.point + hit.normal * mRadius;
                mDropActive = false;

                Invoke("Release", destroyDelay);
                Invoke("DoBlinkers", destroyStartBlinkDelay);
            }
            else {
                pos.y -= moveY;
            }

            transform.position = pos;
        }
    }

    void DoBlinkers() {
        foreach(SpriteColorBlink blinker in mBlinkers) {
            blinker.enabled = true;
        }
    }
}
