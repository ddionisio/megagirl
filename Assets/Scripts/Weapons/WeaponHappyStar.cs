using UnityEngine;
using System.Collections;

public class WeaponHappyStar : Weapon {
    public float angle;

    public LayerMask playerSweepSolid;
    public float playerOfsY;

    public string twinkleProjType;
    public SoundPlayer twinkleSfx;

    public GameObject deactiveOnStopGO;

    public float groundImpulse = 10.0f;

    private ProjectileStarBounce mLastLargeStar;
    
    public override void FireStop() {
        base.FireStop();
        deactiveOnStopGO.SetActive(false);
    }
    
    public override void FireCancel() {
        base.FireCancel();
        deactiveOnStopGO.SetActive(false);
    }

    public override bool Jump(Player player) {
        if(mLastLargeStar && mLastLargeStar.isAlive && !mLastLargeStar.isReleased) {
            if(mLastLargeStar.attachBody) {
                mLastLargeStar.attachBody = null;
                player.controller.ResetCollision();
                player.controller.jumpCounterCurrent = 0;
                player.controller.Jump(true, true);
                return true;
            }
        }
        return false;
    }
    
    protected override void OnEnable() {
        base.OnEnable();
        
        if(mStarted) {
            deactiveOnStopGO.SetActive(false);
        }
    }
    
    protected override void OnDisable() {
        /*if(mLastLargeStar) {
            if(!mLastLargeStar.isReleased)
                mLastLargeStar.Release();

            mLastLargeStar = null;
        }
        mLastLargeStar = null;*/
        
        base.OnDisable();
    }
    
    protected override void OnProjRelease(EntityBase ent) {
        if(mLastLargeStar == ent) {
            mLastLargeStar = null;
        }
        
        base.OnProjRelease(ent);
    }
    
    protected override Projectile CreateProjectile(int chargeInd, Transform seek) {
        if(mLastLargeStar && mLastLargeStar.isAlive && !mLastLargeStar.isReleased) {
            //standard pew-pew star thing
            Projectile proj = Projectile.Create(projGroup, twinkleProjType, spawnPoint, dir, seek);
            if(proj) {
                twinkleSfx.Play();
                mCurProjCount++;
                proj.releaseCallback += OnProjRelease;
            }
            return proj;
        }
        else {
            string type = charges[chargeInd].projType;
            if(!string.IsNullOrEmpty(type)) {
                Player player = Player.instance;

                mLastLargeStar = Projectile.Create(projGroup, type, player.transform.position, dir, seek) as ProjectileStarBounce;
                if(mLastLargeStar) {
                    mCurProjCount++;
                    mLastLargeStar.releaseCallback += OnProjRelease;

                    PlaySfx(chargeInd);

                    //spend energy
                    currentEnergy -= charges[chargeInd].energyCost;

                    //check if we can move player above the attach
                    //then attach player
                    Vector3 attachPos = mLastLargeStar.attachWorldPos;
                    if(!player.controller.CheckPenetrate(attachPos, 0.1f, playerSweepSolid))
                        mLastLargeStar.attachBody = player.rigidbody;

                    if(player.controller.isGrounded) {
                        mLastLargeStar.rigidbody.AddForce(Vector3.up*groundImpulse, ForceMode.Impulse);
                    }
                }
            }
            
            return mLastLargeStar;
        }
    }

    void Update() {
        if(!mFireActive && deactiveOnStopGO.activeSelf)
            deactiveOnStopGO.SetActive(false);
    }
}
