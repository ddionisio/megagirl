using UnityEngine;
using System.Collections;

public class WeaponStarGazer : Weapon {
    public Transform starLargeAttach; //point for large star
    public DamageTrigger starLargeChargeDamage; //the trigger used while chargning large star, this is to destroy it
    public int starLargeIndex = 2;
    public float angle;

    private Projectile mLastLargeStar;

    protected override void OnDisable() {
        /*if(mLastLargeStar) {
            if(!mLastLargeStar.isReleased)
                mLastLargeStar.Release();

            mLastLargeStar = null;
        }
        mLastLargeStar = null;*/

        base.OnDisable();
    }

    protected override void OnProjRelease(EntityBase ent) {
        if(mLastLargeStar == ent) {
            for(int i = 0; i < Player.instance.controller.collisionCount; i++) {
                if(Player.instance.controller.collisionData[i].collider == mLastLargeStar.collider) {
                    Player.instance.controller.ResetCollision();
                    break;
                }
            }

            mLastLargeStar = null;
        }

        base.OnProjRelease(ent);
    }

    protected override Projectile CreateProjectile(int chargeInd, Transform seek) {
        if(chargeInd == starLargeIndex) {
            if(mLastLargeStar && !mLastLargeStar.isReleased) {
                mLastLargeStar.Release();
                mLastLargeStar = null;
            }

            string type = charges[chargeInd].projType;
            if(!string.IsNullOrEmpty(type)) {
                Vector3 _pt = starLargeAttach.position, _dir = dir;

                _pt.z = 0.0f;

                if(angle != 0.0f) {
                    _dir = Quaternion.Euler(new Vector3(0, 0, Mathf.Sign(_dir.x)*angle))*_dir;
                }

                mLastLargeStar = Projectile.Create(projGroup, type, _pt, _dir, seek);
                if(mLastLargeStar) {
                    mCurProjCount++;
                    mLastLargeStar.releaseCallback += OnProjRelease;
                }
            }

            return mLastLargeStar;
        }
        else {
            return base.CreateProjectile(chargeInd, seek);
        }
    }

    protected override void Awake() {
        base.Awake();

        starLargeChargeDamage.damageCallback += OnStarLargeHit;
    }

    void OnStarLargeHit(DamageTrigger trigger, GameObject victim) {
        ResetCharge();
    }
}
