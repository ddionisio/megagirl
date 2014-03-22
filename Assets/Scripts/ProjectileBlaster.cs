using UnityEngine;
using System.Collections;

public class ProjectileBlaster : Projectile {
    public Transform renderT;

    protected override void ApplyContact(GameObject go, Vector3 pos, Vector3 normal) {
        if(isAlive) {
            Stats s = go.GetComponent<Stats>();
            if(s) {
                if(!s.CanDamage(damage) || s.curHP > damage.amount) {
                    state = (int)State.Dying;
                }
            }
            else {
                state = (int)State.Dying;
            }
        }
    }

    protected override void StateChanged() {
        if(state == (int)State.Active) {
            renderT.up = new Vector3(Mathf.Sign(mCurVelocity.x), 0, 0);
        }

        base.StateChanged();
    }
}
