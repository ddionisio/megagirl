using UnityEngine;
using System.Collections;

public class WeaponHappyStar : Weapon {
    public Transform starLargeAttach; //point for large star
    public float angle;

    public LayerMask playerSweepSolid;
    public float playerOfsY;

    public string twinkleProjType;
    public SoundPlayer twinkleSfx;

    public GameObject deactiveOnStopGO;

    private Projectile mLastLargeStar;
    
    public override void FireStop() {
        base.FireStop();
        deactiveOnStopGO.SetActive(false);
    }
    
    public override void FireCancel() {
        base.FireCancel();
        deactiveOnStopGO.SetActive(false);
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
            for(int i = 0; i < Player.instance.controller.collisionCount; i++) {
                if(Player.instance.controller.collisionData[i].collider == mLastLargeStar.collider) {
                    Player.instance.controller.ResetCollision();
                    break;
                }
            }
            
            mLastLargeStar = null;
        }
        
        base.OnProjRelease(ent);
    }
    
    protected override Projectile CreateProjectile(int chargeInd, Transform seek) {
        if(mLastLargeStar) {
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
                bool isGround = player.controller.isGrounded;

                Vector3 _pt = isGround ? starLargeAttach.position : player.collider.bounds.center, _dir = dir;
                
                _pt.z = 0.0f;
                
                if(angle != 0.0f) {
                    _dir = Quaternion.Euler(new Vector3(0, 0, Mathf.Sign(_dir.x)*angle))*_dir;
                }
                
                mLastLargeStar = Projectile.Create(projGroup, type, _pt, _dir, seek);
                if(mLastLargeStar) {
                    mCurProjCount++;
                    mLastLargeStar.releaseCallback += OnProjRelease;
                    
                    PlaySfx(chargeInd);
                    
                    //spend energy
                    currentEnergy -= charges[chargeInd].energyCost;

                    if(!isGround) {
                        //move player to top if possible
                        Vector3 dest = _pt; _pt.y += playerOfsY;
                        Vector3 pdir = dest - player.transform.position;
                        float d = pdir.magnitude;
                        if(d > 0) {
                            pdir /= d;
                            RaycastHit hit;
                            if(!player.rigidbody.SweepTest(pdir, out hit, d) || ((1<<hit.collider.gameObject.layer) & playerSweepSolid) == 0) {
                                player.rigidbody.MovePosition(dest);
                            }
                        }
                    }
                    //playerOfsY playerSweepSolid
                    //if(player.rigidbody.SweepTest(
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
