using UnityEngine;
using System.Collections;

public class ItemPickup : EntityBase {
    public const float destroyDelay = 4.0f;
    public const float destroyStartBlinkDelay = destroyDelay * 0.7f;

    public ItemType type;
    public int bit; //which bit in the flag, used by certain types
    public float value; //used by certain types
    public string sound;
    public string popTextRef;

    public LayerMask dropLayerMask; //which layers the drop will stop when hit
    public float dropSpeed;

    private bool mDropActive;
    private float mRadius;
    private bool mSpawned;

    private SpriteColorBlink[] mBlinkers;

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
                        player.stats.subTankEnergyCurrent += val;

                        //TODO: play special sound?
                    }
                    break;

                case ItemType.Energy:
                    Weapon wpn = null;
                    if(player.currentWeaponIndex == 0) {
                        wpn = player.lowestEnergyWeapon;
                    }
                    else
                        wpn = player.currentWeapon;

                    if(wpn && !wpn.isMaxEnergy) {
                        if(wpn.currentEnergy + val > Weapon.weaponEnergyDefaultMax) {
                            player.stats.subTankWeaponCurrent += (wpn.currentEnergy + val) - Weapon.weaponEnergyDefaultMax;
                        }

                        wpn.currentEnergy += val;
                    }
                    else {
                        player.stats.subTankWeaponCurrent += val;

                        //TODO: play special sound?
                    }
                    break;

                case ItemType.Life:
                    PlayerStats.curLife++;
                    HUD.instance.RefreshLifeCount();
                    break;

                case ItemType.HealthUpgrade:
                    PlayerStats.AddHPMod(bit);
                    break;

                case ItemType.EnergyTank:
                    if(bit == 0)
                        player.stats.AcquireSubTankEnergy1();
                    else
                        player.stats.AcquireSubTankEnergy2();
                    break;

                case ItemType.WeaponTank:
                    if(bit == 0)
                        player.stats.AcquireSubTankWeapon1();
                    else
                        player.stats.AcquireSubTankWeapon2();
                    break;

                case ItemType.Armor:
                    player.stats.AcquireArmor();
                    break;
            }

            if(!string.IsNullOrEmpty(sound)) {
                SoundPlayerGlobal.instance.Play(sound);
            }

            if(collider)
                collider.enabled = false;

            if(!string.IsNullOrEmpty(popTextRef))
                HUD.instance.PopUpMessage(GameLocalize.GetText(popTextRef));

            Release();
        }
    }

    void OnTriggerEnter(Collider col) {
        Player player = col.GetComponent<Player>();
        if(player)
            PickUp(player);
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
