using UnityEngine;
using System.Collections;

public class EnemyBossNil : Enemy {
    public enum Phase {
        None,
        Idle,
        Move,
        CastGround,
        CastMissile,
        Dead
    }

    private AnimatorData mAnimDat;
    private Player mPlayer;
    private Phase mCurPhase = Phase.None;

    protected override void StateChanged() {
        switch((EntityState)prevState) {
            case EntityState.Normal:
                ToPhase(Phase.None);
                break;
        }
        
        base.StateChanged();
        
        switch((EntityState)state) {
            case EntityState.Normal:
                SetPhysicsActive(true, false);

                ToPhase(Phase.Move);
                break;
                
            case EntityState.Dead:
                ToPhase(Phase.Dead);
                break;
                
            case EntityState.Invalid:
                ToPhase(Phase.None);
                break;
        }
    }
    
    void ToPhase(Phase phase) {
        if(mCurPhase == phase)
            return;
        
        //Debug.Log("phase: " + phase);
        
        //prev
        switch(mCurPhase) {
            case Phase.Move:
                break;
                
            case Phase.Dead:
                break;
        }
        
        switch(phase) {
            case Phase.Move:
                break;
                
            case Phase.Dead:
                break;
        }
        
        mCurPhase = phase;
    }
    
    public override void SpawnFinish() {
        base.SpawnFinish();
    }

    protected override void SpawnStart() {
        base.SpawnStart();

        SetPhysicsActive(false, false);
    }
    
    protected override void Awake() {
        base.Awake();
        
        mPlayer = Player.instance;
        mAnimDat = GetComponent<AnimatorData>();
    }
}
