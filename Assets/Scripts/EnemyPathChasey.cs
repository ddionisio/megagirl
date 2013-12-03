using UnityEngine;
using System.Collections;

public class EnemyPathChasey : Enemy {
    public float chaseTimeScale = 1.5f;
    public float chaseCheckLength = 10.0f;
    public LayerMask chaseCheckMask;
    public GameObject attachGO;
    private TimeWarp mTimeWarp;

    protected override void StateChanged() {
        base.StateChanged();
        
        switch((EntityState)state) {
            case EntityState.Stun:
                if(animator)
                    animator.Pause();
                break;
                
            case EntityState.Normal:
                if(animator && !animator.isPlaying)
                    animator.Play("move");
                break;

            case EntityState.Dead:
                if(attachGO)
                    attachGO.SetActive(false);
                break;
                
            case EntityState.RespawnWait:
                break;
        }
    }

    protected override void ActivatorSleep() {
        base.ActivatorSleep();

        switch((EntityState)state) {
            case EntityState.Normal:
            case EntityState.Hurt:
            case EntityState.Stun:
                if(respawnOnSleep) {
                    if(attachGO)
                        attachGO.SetActive(false);
                }
                break;
        }
    }
    
    protected override void Restart() {
        base.Restart();

        if(attachGO)
            attachGO.SetActive(true);
    }
    
    public override void SpawnFinish() {
        base.SpawnFinish();
    }
    
    protected override void Awake() {
        base.Awake();

        mTimeWarp = GetComponent<TimeWarp>();
    }

    void FixedUpdate() {
        switch((EntityState)state) {
            case EntityState.Normal:
                float timeScale = mTimeWarp.scale;

                if(animator) {
                    float animTimeScale = 1.0f;
                    
                    RaycastHit hit;
                    Vector3 r = transform.right;
                    Vector3 pos = collider.bounds.center; pos.z = 0.0f;
                    if((Physics.Raycast(pos, r, out hit, chaseCheckLength, chaseCheckMask) && hit.collider.CompareTag("Player"))
                       || (Physics.Raycast(pos, -r, out hit, chaseCheckLength, chaseCheckMask) && hit.collider.CompareTag("Player"))) {
                        animTimeScale = chaseTimeScale;
                    }
                    animator.animScale = animTimeScale * timeScale;
                }
                break;
        }
    }
}
