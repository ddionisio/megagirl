using UnityEngine;
using System.Collections;

public class WeaponWhip : Weapon {
    public override bool canFire {
        get {
            return base.canFire && anim.CurrentClip == mClips[(int)AnimState.normal];
        }
    }

    public Transform whipStart;
    public Transform whipEnd;

    public LayerMask hitMask;
    public float hitRadius = 0.25f;

    public LayerMask triggerMask;

    private Damage mDmg;
    private bool mActActive;

    protected override Projectile CreateProjectile(int chargeInd, Transform seek) {
        if(chargeInd == 0)
            currentEnergy -= charges[chargeInd].energyCost;

        return null;
    }

    protected override void OnDisable() {
        mActActive = false;

        base.OnDisable();
    }

    protected override void OnDestroy() {
        if(anim)
            anim.AnimationEventTriggered -= OnAnimEvent;

        base.OnDestroy();
    }

    protected override void Awake() {
        base.Awake();

        if(anim)
            anim.AnimationEventTriggered += OnAnimEvent;

        mDmg = GetComponent<Damage>();
    }

    void LateUpdate() {
        if(mActActive) {
            if(anim.CurrentClip != mClips[(int)AnimState.attack] || !anim.gameObject.activeInHierarchy)
                mActActive = false;
            else {
                //hit stuff
                Vector3 s = whipStart.position; s.z = 0.0f;
                Vector3 e = whipEnd.position; e.z = 0.0f;
                float dist = Mathf.Abs(e.x - s.x);
                Vector3 dir = new Vector3(Mathf.Sign(e.x - s.x), 0, 0);

                RaycastHit[] hits = Physics.SphereCastAll(s, hitRadius, dir, dist, hitMask);
                for(int i = 0, max = hits.Length; i < max; i++) {
                    mDmg.CallDamageTo(hits[i].collider.gameObject, hits[i].point, hits[i].normal);
                }

                hits = Physics.RaycastAll(s, dir, dist, triggerMask);
                for(int i = 0, max = hits.Length; i < max; i++) {
                    ItemPickup item = hits[i].collider.GetComponent<ItemPickup>();
                    if(item && !item.isReleased) {
                        item.PickUp(Player.instance);
                        //continue;
                    }

                    //other triggers?
                }
            }
        }
    }

    protected override void OnAnimationClipEnd(tk2dSpriteAnimator aAnim, tk2dSpriteAnimationClip aClip) {
        if(anim == aAnim && aClip == mClips[(int)AnimState.attack]) {
            mActActive = false;
            mFireActive = false;
        }

        base.OnAnimationClipEnd(aAnim, aClip);
    }

    void OnAnimEvent(tk2dSpriteAnimator aAnim, tk2dSpriteAnimationClip aClip, int frame) {
        if(anim == aAnim && aClip == mClips[(int)AnimState.attack]) {
            tk2dSpriteAnimationFrame frameDat = aClip.GetFrame(frame);
            if(frameDat.eventInfo == "actS") {
                mActActive = true;
            }
            else if(frameDat.eventInfo == "actE") {
                mActActive = false;
            }
        }
    }
}
