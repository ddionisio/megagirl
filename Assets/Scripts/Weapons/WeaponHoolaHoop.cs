using UnityEngine;
using System.Collections;

public class WeaponHoolaHoop : Weapon {
    public AnimatorData hoolaAnimDat;
    public float speed;
    public string clip = "twirl";

    public float energyPerSecond;

    public GameObject damageGO;

    private float mDefaultAirForce;

    public override void FireStart() {
        if(canFire && !mFireActive) {
            mFireActive = true;

            Player player = Player.instance;

            player.controllerSprite.PlayOverrideClip(clip);
            player.controllerSprite.useVelocitySpeed = true;

            player.controller.moveMaxSpeed = speed;
            player.controller.moveForce *= 2.0f;
            player.controller.moveAirForce = player.controller.moveForce;

            Stats.DamageMod dmgReduce = player.stats.GetDamageMod(player.stats.damageTypeReduction, Damage.Type.Contact);
            if(dmgReduce != null)
                dmgReduce.val = 1.0f;

            hoolaAnimDat.Play("active");

            damageGO.SetActive(true);
        }
    }

    public override void FireStop() {
        if(mFireActive) {
            mFireActive = false;

            Player player = Player.instance;

            player.controllerSprite.StopOverrideClip();
            player.controllerSprite.useVelocitySpeed = false;

            player.controller.moveMaxSpeed = player.controllerDefaultMaxSpeed;
            player.controller.moveForce = player.controllerDefaultForce;
            player.controller.moveAirForce = mDefaultAirForce;

            if(Mathf.Abs(player.controller.localVelocity.x) > player.controller.moveMaxSpeed) {
                player.controller.localVelocity = new Vector3(Mathf.Sign(player.controller.localVelocity.x) * player.controller.moveMaxSpeed, player.controller.localVelocity.y, 0.0f);
            }

            Stats.DamageMod dmgReduce = player.stats.GetDamageMod(player.stats.damageTypeReduction, Damage.Type.Contact);
            if(dmgReduce != null)
                dmgReduce.val = 0.0f;

            hoolaAnimDat.PlayDefault();

            damageGO.SetActive(false);
        }
    }

    public override void FireCancel() {
        FireStop();
    }

    protected override void Awake() {
        mDefaultAirForce = Player.instance.controller.moveAirForce;

        damageGO.SetActive(false);

        base.Awake();
    }

    // Update is called once per frame
    void Update() {
        if(mFireActive) {
            currentEnergy -= energyPerSecond * Time.deltaTime;
            if(currentEnergy <= 0.0f)
                FireStop();
        }
    }
}
