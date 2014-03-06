using UnityEngine;
using System.Collections;

/// <summary>
/// Put this in entity object, will unlock upon death.
/// </summary>
public class AchievementEnemyMultiDeadPerfect : AchievementNotifier {
    public Enemy[] enemies;

    private int mPlayerHitsTaken;
    private int mEnemyNonEnergyHitsTaken;
    private int mEnemyDeadCount;
    private bool mInitialized=false;
    
    protected override void Awake() {
        base.Awake();

        for(int i = 0; i < enemies.Length; i++) {
            enemies[i].setStateCallback += OnStateChange;
        }
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
                mEnemyDeadCount++;
                if(mEnemyDeadCount >= enemies.Length && mPlayerHitsTaken == 0 && mEnemyNonEnergyHitsTaken == 0) {
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
