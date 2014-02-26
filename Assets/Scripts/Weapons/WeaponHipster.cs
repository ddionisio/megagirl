using UnityEngine;
using System.Collections;

public class WeaponHipster : Weapon {
    public GameObject fireGO;

    public float fallSpeedCap;

    public float energyPerSecond;

    public override void FireStart() {
        if(canFire && !mFireActive) {
            mFireActive = true;

            fireGO.SetActive(true);

            chargeSfx.Play();
        }
    }

    public override void FireStop() {
        if(mFireActive) {
            mFireActive = false;

            fireGO.SetActive(false);

            chargeSfx.Stop();
        }
    }

    public override void FireCancel() {
        FireStop();
    }

    protected override void Awake() {
        base.Awake();

        fireGO.SetActive(false);
    }

    void FixedUpdate() {
        if(mFireActive) {
            PlatformerController ctrl = Player.instance.controller;
            Vector3 lv = ctrl.localVelocity;
            if(lv.y < 0.0f && lv.y < -fallSpeedCap) {
                lv.y = -fallSpeedCap;
                ctrl.localVelocity = lv;
            }

            currentEnergy -= energyPerSecond * Time.fixedDeltaTime;
            if(currentEnergy <= 0.0f)
                FireStop();
        }
    }
}
