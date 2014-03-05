using UnityEngine;
using System.Collections;

/// <summary>
/// Put this in entity object, will unlock upon death.
/// </summary>
public class AchievementEnemyDeadPerfect : AchievementNotifier {
    private int mPlayerHitsTaken;
    private int mEnemyNonEnergyHitsTaken;
    private bool mInitialized=false;
    
    protected override void Awake() {
        base.Awake();
        
        EntityBase ent = GetComponent<EntityBase>();
        ent.setStateCallback += OnStateChange;
    }

    void OnStateChange(EntityBase ent) {
        switch((EntityState)ent.state) {
            case EntityState.Normal:
                if(!mInitialized) {
                    Player.instance.stats.applyDamageCallback += OnPlayerDamage;
                    ((Enemy)ent).stats.applyDamageCallback += OnEnemyDamage;
                    mInitialized = true;
                }
                break;

            case EntityState.Dead:
                if(mPlayerHitsTaken == 0 && mEnemyNonEnergyHitsTaken == 0) {
                    Notify();
                }
                break;
        }
    }

    void OnPlayerDamage(Damage damage) {
        mPlayerHitsTaken++;
    }

    void OnEnemyDamage(Damage damage) {
        if(damage.type != Damage.Type.Energy)
            mEnemyNonEnergyHitsTaken++;
    }
}
