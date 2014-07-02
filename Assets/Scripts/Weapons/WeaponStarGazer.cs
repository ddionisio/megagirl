using UnityEngine;
using System.Collections;

public class WeaponStarGazer : Weapon {
    public DamageTrigger starLargeChargeDamage; //the trigger used while chargning large star, this is to destroy it

    public GameObject deactiveOnStopGO;


    public override void FireStop() {
        base.FireStop();
        deactiveOnStopGO.SetActive(false);
    }
    
    public override void FireCancel() {
        base.FireCancel();
        deactiveOnStopGO.SetActive(false);
    }

    protected override void OnEnable() {
        base.OnEnable();

        if(mStarted) {
            deactiveOnStopGO.SetActive(false);
        }
    }

    protected override void OnDisable() {
        /*if(mLastLargeStar) {
            if(!mLastLargeStar.isReleased)
                mLastLargeStar.Release();

            mLastLargeStar = null;
        }
        mLastLargeStar = null;*/

        base.OnDisable();
    }

    protected override void Awake() {
        base.Awake();

        starLargeChargeDamage.damageCallback += OnStarLargeHit;
    }

    void OnStarLargeHit(DamageTrigger trigger, GameObject victim) {
        ResetCharge();
    }

    void Update() {
        if(!mFireActive && deactiveOnStopGO.activeSelf)
            deactiveOnStopGO.SetActive(false);
    }

    protected override void OnAnimationClipEnd(tk2dSpriteAnimator aAnim, tk2dSpriteAnimationClip aClip) {
        base.OnAnimationClipEnd(aAnim, aClip);
        deactiveOnStopGO.SetActive(false);
    }
}
