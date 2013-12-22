using UnityEngine;
using System.Collections;

public class ProjectileClone : Projectile {
    public float seekRadius = 10.0f;
    public LayerMask seekContactMask;
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
                mCtrl.inputEnabled = true;
                mCtrl.moveSideLock = true;

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

    protected override void FixedUpdate() {
        Collider[] seekCols = Physics.OverlapSphere(collider.bounds.center, seekRadius, seekContactMask);
        if(seekCols.Length == 0) {
            //try player's
            seekCols = Physics.OverlapSphere(Player.instance.collider.bounds.center, seekRadius, seekContactMask);
        }

        if(seekCols.Length > 0) {
            Bounds b = collider.bounds;

            //get nearest
            Collider nearestCol = null;
            float nearestDistSqr = Mathf.Infinity;
            Vector3 nearestDPos = Vector3.zero;
            for(int i = 0, max = seekCols.Length; i < max; i++) {
                nearestDPos = seekCols[i].bounds.center - b.center;
                float distSqr = nearestDPos.sqrMagnitude;
                if(distSqr < nearestDistSqr) {
                    nearestCol = seekCols[i];
                    nearestDistSqr = distSqr;
                }
            }

            Bounds nearestBounds = nearestCol.bounds;
            mCtrl.moveSide = Mathf.Sign(nearestDPos.x);
            if(b.min.y > nearestBounds.max.y) {
                mCtrl.Jump(false);
            }
            else if(b.max.y < nearestBounds.min.y) {
                if(!mCtrl.isJump) {
                    mCtrl.Jump(false);
                    mCtrl.Jump(true);
                }
            }
            else {
                if((mCtrl.canWallJump && !mCtrl.isJump) || (mCtrl.collisionFlags & CollisionFlags.Sides) != 0) {
                    mCtrl.Jump(false);
                    mCtrl.Jump(true);
                }
            }
        }
        else {
            mCtrl.Jump(false);
            mCtrl.moveSide = 0;
        }
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
