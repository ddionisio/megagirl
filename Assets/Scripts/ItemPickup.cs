using UnityEngine;
using System.Collections;

public class ItemPickup : EntityBase {
    public const float destroyDelay = 4.0f;
    public const float destroyStartBlinkDelay = destroyDelay * 0.7f;

    public delegate void OnPickUp(ItemPickup item);

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

    public event OnPickUp pickupCallback;

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
                    SlotInfo.AddHPMod(bit);
                    Player.instance.stats.RefreshHPMod();
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

                case ItemType.Invul:
                    player.stats.invulGO.SetActive(true);
                    break;
            }

            if(savePickUp) {
                SceneState.instance.SetGlobalFlag(LevelController.levelPickupBitState, pickupBit, true, false);

                bool isHardcore = SlotInfo.gameMode == SlotInfo.GameMode.Hardcore;
                if(isHardcore) {
                    int dat = SceneState.instance.GetGlobalValue(LevelController.levelPickupBitState, 0);
                    SceneState.instance.SetValue(LevelController.levelPickupBitState, dat, true);
                }
            }

            if(!string.IsNullOrEmpty(sound)) {
                SoundPlayerGlobal.instance.Play(sound);
            }

            if(GetComponent<Collider>())
                GetComponent<Collider>().enabled = false;

            if(pickupCallback != null)
                pickupCallback(this);

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
        if(GetComponent<Collider>())
            GetComponent<Collider>().enabled = true;

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
        pickupCallback = null;

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

        //check if life pick, change to invul for non-hardcore.
        if(type == ItemType.Life && SlotInfo.gameMode != SlotInfo.GameMode.Hardcore) {
            type = ItemType.Invul;
            savePickUp = false;
            tk2dSpriteAnimator anim = GetComponentInChildren<tk2dSpriteAnimator>();
            if(anim)
                anim.DefaultClipId = anim.GetClipIdByName("alt");
        }

        //initialize variables
        if(GetComponent<Collider>()) {
            SphereCollider sphr = GetComponent<Collider>() as SphereCollider;
            if(sphr) {
                mRadius = sphr.radius;
            }
            else {
                mRadius = GetComponent<Collider>().bounds.extents.y;
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
                doDisable = SlotInfo.IsHPModAcquired(bit) || LevelController.isTimeTrial;
                break;

            case ItemType.EnergyTank:
                if(bit == 0)
                    doDisable = SlotInfo.isSubTankEnergy1Acquired || LevelController.isTimeTrial;
                else
                    doDisable = SlotInfo.isSubTankEnergy2Acquired || LevelController.isTimeTrial;
                break;

            case ItemType.WeaponTank:
                if(bit == 0)
                    doDisable = SlotInfo.isSubTankWeapon1Acquired || LevelController.isTimeTrial;
                else
                    doDisable = SlotInfo.isSubTankWeapon2Acquired || LevelController.isTimeTrial;
                break;

            case ItemType.Armor:
                doDisable = SlotInfo.isArmorAcquired || LevelController.isTimeTrial;
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
            if(Physics.SphereCast(GetComponent<Collider>().bounds.center, mRadius, Vector3.down, out hit, moveY, dropLayerMask)) {
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
