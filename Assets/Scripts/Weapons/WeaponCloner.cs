using UnityEngine;
using System.Collections;

public class WeaponCloner : Weapon {
    public Vector3 spawnOfs = new Vector3(0, 0.1f, 0);

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

    protected override void OnEnable() {
        base.OnEnable();

        if(mStarted && activeGO) {
            activeGO.SetActive(mCurProjCount > 0);
        }
    }

    protected override Projectile CreateProjectile(int chargeInd, Transform seek) {
        Projectile proj = base.CreateProjectile(chargeInd, seek);

        activeGO.SetActive(mCurProjCount > 0);

        return proj;
    }

    protected override void OnProjRelease(EntityBase ent) {
        base.OnProjRelease(ent);

        activeGO.SetActive(mCurProjCount > 0);
    }
}
