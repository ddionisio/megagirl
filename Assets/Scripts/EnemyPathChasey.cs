using UnityEngine;
using System.Collections;

public class EnemyPathChasey : Enemy {
    public float chaseTimeScale = 1.5f;
    public float chaseCheckLength = 10.0f;
    public LayerMask chaseCheckMask;
    public GameObject attachGO;
    public tk2dSpriteAnimator anim;
    public string sadClip = "sad";
    public string attachProjType;
    public Transform attachProjPt;
    public GameObject attachProjChain;
    private TimeWarp mTimeWarp;
    private Projectile mProj;

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

                if(prevState != (int)EntityState.Stun) {
                    if(anim)
                        anim.Play(anim.DefaultClip);

                    if(!mProj && !string.IsNullOrEmpty(attachProjType)) {
                        mProj = Projectile.Create(projGroup, attachProjType, attachProjPt.position, Vector3.zero, null);
                        RigidBodyMoveToTarget attacher = mProj.GetComponent<RigidBodyMoveToTarget>();
                        if(attacher)
                            attacher.target = attachProjPt;

                        mProj.releaseCallback += OnProjRelease;
                        
                        if(attachProjChain)
                            attachProjChain.SetActive(true);
                    }
                }
                break;

            case EntityState.Dead:
                if(attachGO)
                    attachGO.SetActive(false);

                if(mProj) {
                    mProj.releaseCallback -= OnProjRelease;

                    if(mProj.isAlive) {
                        RigidBodyMoveToTarget attacher = mProj.GetComponent<RigidBodyMoveToTarget>();
                        if(attacher)
                            attacher.target = null;

                        if(mProj.stats)
                            mProj.stats.curHP = 0;
                        else
                            mProj.state = (int)Projectile.State.Dying;
                    }

                    mProj = null;

                    if(attachProjChain)
                        attachProjChain.SetActive(false);
                }
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

        if(attachProjChain)
            attachProjChain.SetActive(false);
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

    void OnProjRelease(EntityBase ent) {
        mProj.releaseCallback -= OnProjRelease;
        mProj = null;
        if((EntityState)state != EntityState.Dead) {
            if(anim)
                anim.Play(sadClip);
        }
        
        if(attachProjChain)
            attachProjChain.SetActive(false);
    }
}
