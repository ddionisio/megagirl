using UnityEngine;
using System.Collections;

public class WeaponCloner : Weapon {
    public Vector3 spawnOfs = new Vector3(0, 0.1f, 0);

    private Projectile mLastClone;

    public override Vector3 spawnPoint {
        get {
            Vector3 pt = Player.instance.transform.localToWorldMatrix.MultiplyPoint(spawnOfs);
            return pt;
        }
    }

    public override Vector3 dir {
        get {
            return Vector3.up;
        }
    }

    public override bool canFire {
        get {
            return base.canFire || mLastClone != null;
        }
    }

    protected override void OnEnable() {
        base.OnEnable();

        if(mStarted) {
            activeGO.SetActive(mLastClone != null);
        }
    }

    protected override void OnProjRelease(EntityBase ent) {
        if(mLastClone == ent) {
            mLastClone = null;
            activeGO.SetActive(false);
        }

        base.OnProjRelease(ent);
    }

    protected override Projectile CreateProjectile(int chargeInd, Transform seek) {
        if(chargeInd == 0) {
            if(mLastClone) {
                //detonate
                if(mLastClone.state == (int)Projectile.State.Active || mLastClone.state == (int)Projectile.State.Seek) {
                    if(mLastClone.stats)
                        mLastClone.stats.curHP = 0;
                    else
                        mLastClone.state = (int)Projectile.State.Dying;

                    mLastClone = null;
                    activeGO.SetActive(false);
                }
                return null;
            }
            else {
                mLastClone = base.CreateProjectile(chargeInd, seek);

                activeGO.SetActive(mLastClone != null);

                return mLastClone;
            }
        }

        return base.CreateProjectile(chargeInd, seek);
    }
}
