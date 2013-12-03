using UnityEngine;
using System.Collections;

public class EnemyBossTank : Enemy {
    protected override void StateChanged() {
        base.StateChanged();

        switch((EntityState)state) {
            case EntityState.Normal:
                bodyCtrl.inputEnabled = true;
                //bodyCtrl.moveSide = 0.5f;
                break;
        }
    }
}
