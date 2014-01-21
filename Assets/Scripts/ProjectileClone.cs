using UnityEngine;
using System.Collections;

public class ProjectileClone : Projectile {
    public float seekRadius = 10.0f;
    public LayerMask seekContactMask;

    public float playerVelocityDist = -2.0f;
    public float dirChangeDelay = 0.5f;

    private SpriteColorBlink[] mBlinks;

    private EntityDamageBlinkerSprite mBlinker;
    private float mLastDirChangeTime;

    protected override void StateChanged() {
        base.StateChanged();

        switch((State)state) {
            case State.Active:
                Invoke("DoBlink", decayDelay * 0.85f);
                mLastDirChangeTime = 0.0f;
                break;

            case State.Invalid:
                break;
        }
    }

    public override void SpawnFinish() {

        base.SpawnFinish();
    }

    protected override void Awake() {
        base.Awake();

        mBlinker = GetComponent<EntityDamageBlinkerSprite>();
    }

    protected override void FixedUpdate() {
        if(Time.fixedTime - mLastDirChangeTime > dirChangeDelay) {
            Vector3 destPos;

            Collider[] seekCols = Physics.OverlapSphere(collider.bounds.center, seekRadius, seekContactMask);

            if(seekCols.Length > 0) {
                Bounds b = collider.bounds;

                //get nearest
                Collider nearestCol = null;
                float nearestDistSqr = Mathf.Infinity;
                Vector3 nearestDPos = Vector3.zero;
                for(int i = 0, max = seekCols.Length; i < max; i++) {
                    nearestDPos = seekCols[i].bounds.center - b.center;
                    float distSqr = nearestDPos.sqrMagnitude;
                    if(distSqr < nearestDistSqr) {
                        nearestCol = seekCols[i];
                        nearestDistSqr = distSqr;
                    }
                }

                Bounds nearestBounds = nearestCol.bounds;

                destPos = nearestBounds.center;
            }
            else {
                Player player = Player.instance;
                Vector3 playerVel = player.rigidbody.velocity;
                destPos = player.collider.bounds.center + playerVel.normalized*playerVelocityDist;
            }

            mActiveForce = (destPos - collider.bounds.center).normalized*force;

            mLastDirChangeTime = Time.fixedTime;
        }

        rigidbody.AddForce(mActiveForce * mMoveScale);
    }

    void DoBlink() {
        mBlinker.noBlinking = true;
        foreach(SpriteColorBlink blinker in mBlinker.blinks) {
            blinker.enabled = true;
        }
    }
}
