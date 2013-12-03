using UnityEngine;
using System.Collections;

public class ProjectileClone : Projectile {
    public LayerMask deathContactMask;

    private SpriteColorBlink[] mBlinks;

    private EntityDamageBlinkerSprite mBlinker;
    private PlatformerController mCtrl;

    protected override void StateChanged() {
        base.StateChanged();

        switch((State)state) {
            case State.Active:
                mCtrl.ResetCollision();
                mCtrl.gravityController.enabled = true;

                Invoke("DoBlink", decayDelay * 0.85f);
                break;

            case State.Invalid:
                mCtrl.ResetCollision();
                mCtrl.gravityController.enabled = false;
                break;
        }
    }

    public override void SpawnFinish() {

        base.SpawnFinish();
    }

    protected override void Awake() {
        base.Awake();

        mCtrl = GetComponent<PlatformerController>();
        mCtrl.gravityController.enabled = false;

        mBlinker = GetComponent<EntityDamageBlinkerSprite>();
    }

    void DoBlink() {
        mBlinker.noBlinking = true;
        foreach(SpriteColorBlink blinker in mBlinker.blinks) {
            blinker.enabled = true;
        }
    }

    protected override void ApplyContact(GameObject go, Vector3 pos, Vector3 normal) {
        if(((1 << go.layer) & deathContactMask) != 0)
            state = (int)State.Dying;
        else
            base.ApplyContact(go, pos, normal);
    }
}
