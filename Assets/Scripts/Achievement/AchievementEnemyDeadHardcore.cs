using UnityEngine;
using System.Collections;

/// <summary>
/// Put this in entity object, will unlock upon death.
/// </summary>
public class AchievementEnemyDeadHardcore : AchievementNotifier {
    
    protected override void Awake() {
        base.Awake();
        
        EntityBase ent = GetComponent<EntityBase>();
        ent.setStateCallback += OnStateChange;
    }
    
    void OnStateChange(EntityBase ent) {
        if(SlotInfo.gameMode == SlotInfo.GameMode.Hardcore && ent.state == (int)EntityState.Dead) {
            Notify();
        }
    }
}
