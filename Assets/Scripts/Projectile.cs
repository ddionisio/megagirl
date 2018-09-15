﻿using UnityEngine;
using System.Collections;

public class Projectile : EntityBase {
    public enum State {
        Invalid = -1,
        Active,
        Seek,
        SeekForce,
        Dying
    }

    public enum ContactType {
        None,
        End,
        Stop,
        Bounce,
        BounceRotate
    }

    public enum ForceBounceType {
        None,
        ContactNormal,
        ReflectDir,
        ReflectDirXOnly,
        ContactNormalXOnly,
    }

    public struct HitInfo {
        public Collider col;
        public Vector3 normal;
        public Vector3 point;
    }

    public const string soundHurt = "enemyHit";

    public bool simple; //don't use rigidbody, make sure you have a sphere collider and set it to trigger
    public LayerMask simpleLayerMask;
    public string[] hitTags;
    public float startVelocity;
    public float startVelocityAddRand;
    public float force;
    [SerializeField]
    float speedLimit;
    public bool seekUseForce = false;
    public float seekAngleLimit = 360.0f; //angle limit from startDir
    public float seekForceDelay = 0.15f;
    public float seekStartDelay = 0.0f;
    public float seekVelocity;
    public float seekVelocityCap = 5.0f;
    public float seekTurnAngleCap = 360.0f;
    public bool decayEnabled = true;
    public float decayDelay;
    public bool releaseOnDie;
    public float dieDelay;
    public bool dieBlink;
    public bool dieDisablePhysics = true;
    public bool releaseOnSleep;
    public LayerMask explodeMask;
    public float explodeForce;
    public Vector3 explodeOfs;
    public Transform explodeOfsTarget;
    public float explodeRadius;
    public ContactType contactType = ContactType.End;
    public ForceBounceType forceBounce = ForceBounceType.None;
    public int maxBounce = -1;
    public float bounceRotateAngle;
    public float bounceSurfaceOfs; //displace projectile slightly off surface based on normal
    public bool bounceRigidbodyApplyVelocity = true;
    public bool explodeOnDeath;
    public Transform applyDirToUp;
    public LayerMask EndByContactMask; //if not 0, use death contact mask to determine if we die based on layer
    public string deathSpawnGroup;
    public string deathSpawnType;
    public bool autoDisableCollision = true; //when not active, disable collision
    public int damageExpireCount = -1; //the number of damage dealt for it to die, -1 for infinite

    public SoundPlayer contactSfx;
    public SoundPlayer dyingSfx;

    /*public bool oscillate;
    public float oscillateForce;
    public float oscillateDelay;*/

    private bool mSpawning = false;
    protected Vector3 mActiveForce;
    protected Vector3 mInitDir = Vector3.zero;
    protected Transform mSeek = null;
    protected Vector3 mCurVelocity; //only use by simple

    private Damage mDamage;
    private float mDefaultSpeedLimit;
    protected SphereCollider mSphereColl;
    protected float mMoveScale = 1.0f;
    private Stats mStats;
    private int mCurBounce = 0;
    private int mCurDamageCount = 0;
    private HitInfo mLastHit;

    private Vector3 mSeekCurDir;
    private Vector3 mSeekCurDirVel;

    private Rigidbody mBody;

    //private Vector2 mOscillateDir;
    //private bool mOscillateSwitch;

    public static Projectile Create(string group, string typeName, Vector3 startPos, Vector3 dir, Transform seek) {
        Projectile ret = Spawn<Projectile>(group, typeName, startPos);
        if(ret != null) {
            ret.mInitDir = dir;
            ret.mSeek = seek;
        }

        return ret;
    }

    public bool isAlive { get { return (State)state == State.Active || (State)state == State.Seek || (State)state == State.SeekForce; } }

    public Damage damage { get { return mDamage; } }

    public Transform seek {
        get { return mSeek; }
        set {
            mSeek = value;

            if(mSeek) {
                if((State)state == State.Active) {
                    state = (int)(seekUseForce ? State.SeekForce : State.Seek);
                }
            }
            else {
                if((State)state == State.Seek || (State)state == State.SeekForce) {
                    state = (int)State.Active;
                }
            }
        }
    }

    public bool spawning { get { return mSpawning; } }

    public Vector3 velocity {
        get {
            if(simple)
                return mCurVelocity;
            return mBody != null ? mBody.velocity : Vector3.zero;
        }

        set {
            if(simple)
                mCurVelocity = value;
            else if(mBody)
                mBody.velocity = value;
        }
    }

    public Vector3 activeForce {
        get { return mActiveForce; }
        set { mActiveForce = value; }
    }

    public float moveScale {
        get { return mMoveScale; }
        set { mMoveScale = value; }
    }

    public Stats stats { get { return mStats; } }

    public HitInfo lastHit { get { return mLastHit; } }

    public void SetSpeedLimit(float limit) {
        speedLimit = limit;
    }

    public void RevertSpeedLimit() {
        speedLimit = mDefaultSpeedLimit;
    }

    protected override void Awake() {
        base.Awake();

        mBody = GetComponent<Rigidbody>();

        mSphereColl = GetComponent<Collider>() ? GetComponent<Collider>() as SphereCollider : null;

        mDefaultSpeedLimit = speedLimit;

        if(mBody != null && autoDisableCollision) {
            mBody.detectCollisions = false;
        }

        if(GetComponent<Collider>() != null && autoDisableCollision)
            GetComponent<Collider>().enabled = false;

        mDamage = GetComponent<Damage>();

        Stats[] ss = GetComponentsInChildren<Stats>(true);
        mStats = ss.Length > 0 ? ss[0] : null;
        if(mStats) {
            mStats.changeHPCallback += OnHPChange;
            mStats.isInvul = true;
        }

        if(!FSM)
            autoSpawnFinish = true;
    }

    // Use this for initialization
    protected override void Start() {
        base.Start();
    }

    protected override void ActivatorSleep() {
        base.ActivatorSleep();

        if(!mSpawning && isAlive && releaseOnSleep) {
            activator.ForceActivate();
            Release();
        }
    }

    public override void SpawnFinish() {
        //Debug.Log("start dir: " + mStartDir);

        mSpawning = false;

        mCurBounce = 0;
        mCurDamageCount = 0;

        if(decayEnabled && decayDelay == 0) {
            OnDecayEnd();
        }
        else {
            //starting direction and force
            if(simple) {
                mCurVelocity = mInitDir * startVelocity;
            }
            else {
                if(mBody != null && mInitDir != Vector3.zero) {
                    //set velocity
                    if(!mBody.isKinematic) {
                        if(startVelocityAddRand != 0.0f) {
                            mBody.velocity = mInitDir * (startVelocity + Random.value * startVelocityAddRand);
                        }
                        else {
                            mBody.velocity = mInitDir * startVelocity;
                        }
                    }

                    mActiveForce = mInitDir * force;
                }
            }

            if(decayEnabled)
                Invoke("OnDecayEnd", decayDelay);

            if(seekStartDelay > 0.0f) {
                state = (int)State.Active;

                Invoke("OnSeekStart", seekStartDelay);
            }
            else {
                OnSeekStart();
            }
                        
            if(applyDirToUp) {
                applyDirToUp.up = mInitDir;
                InvokeRepeating("OnUpUpdate", 0.1f, 0.1f);
            }
        }
    }

    protected override void SpawnStart() {
        if(applyDirToUp && mInitDir != Vector3.zero) {
            applyDirToUp.up = mInitDir;
        }

        mSpawning = true;
    }

    public override void Release() {
        state = (int)State.Invalid;
        base.Release();
    }

    protected override void StateChanged() {
        switch((State)state) {
            case State.Seek:
            case State.SeekForce:
            case State.Active:
                if(GetComponent<Collider>())
                    GetComponent<Collider>().enabled = true;

                if(mBody)
                    mBody.detectCollisions = true;

                if(mStats)
                    mStats.isInvul = false;
                break;

            case State.Dying:
                CancelInvoke();

                if(dieDisablePhysics)
                    PhysicsDisable();

                if(dieDelay > 0) {
                    if(dieBlink)
                        Blink(dieDelay);

                    CancelInvoke("Die");
                    Invoke("Die", dieDelay);
                }
                else
                    Die();

                if(dyingSfx && !dyingSfx.isPlaying)
                    dyingSfx.Play();
                break;

            case State.Invalid:
                CancelInvoke();
                RevertSpeedLimit();
                
                PhysicsDisable();
                
                if(mStats) {
                    mStats.Reset();
                    mStats.isInvul = true;
                }
                
                mSpawning = false;
                break;
        }
    }

    void PhysicsDisable() {
        mCurVelocity = Vector3.zero;
        if(GetComponent<Collider>() && autoDisableCollision)
            GetComponent<Collider>().enabled = false;

        if(mBody) {
            if(autoDisableCollision)
                mBody.detectCollisions = false;

            if(!mBody.isKinematic) {
                mBody.velocity = Vector3.zero;
                mBody.angularVelocity = Vector3.zero;
            }
        }
    }

    void Die() {
        if(!string.IsNullOrEmpty(deathSpawnGroup) && !string.IsNullOrEmpty(deathSpawnType)) {
            Vector2 p = (explodeOfsTarget ? explodeOfsTarget : transform).localToWorldMatrix.MultiplyPoint(explodeOfs);//explodeOnDeath ? transform.localToWorldMatrix.MultiplyPoint(explodeOfs) : transform.position;

            PoolController.Spawn(deathSpawnGroup, deathSpawnType, deathSpawnType, null, p, Quaternion.identity);
        }

        if(explodeOnDeath && explodeRadius > 0.0f) {
            DoExplode();
        }

        if(releaseOnDie) {
            Release();
        }
    }

    bool CheckTag(string tag) {
        if(hitTags.Length == 0)
            return true;

        for(int i = 0, max = hitTags.Length; i < max; i++) {
            if(hitTags[i] == tag)
                return true;
        }

        return false;
    }

    protected virtual void OnHPChange(Stats stat, float delta) {
        if(stat.curHP == 0) {
            if(isAlive)
                state = (int)State.Dying;
        }
        else if(delta < 0.0f) {
            SoundPlayerGlobal.instance.Play(soundHurt);
        }
    }

    protected virtual void ApplyContact(GameObject go, Vector3 pos, Vector3 normal) {
        switch(contactType) {
            case ContactType.None:
                if(contactSfx && contactSfx.gameObject.activeInHierarchy)
                    contactSfx.Play();
                break;

            case ContactType.End:
                if(isAlive)
                    state = (int)State.Dying;
                break;

            case ContactType.Stop:
                if(simple)
                    mCurVelocity = Vector3.zero;
                else if(mBody != null)
                    mBody.velocity = Vector3.zero;
                break;
                            
            case ContactType.Bounce:
                if(maxBounce > 0 && mCurBounce == maxBounce)
                    state = (int)State.Dying;
                else {
                    if(simple) {
                        mCurVelocity = Vector3.Reflect(mCurVelocity, normal);

                        if(bounceSurfaceOfs != 0.0f) {
                            Vector3 p = transform.position;
                            p += normal*bounceSurfaceOfs;
                            transform.position = p;
                        }
                    }
                    else {
                        if(mBody != null) {
                            if(bounceSurfaceOfs != 0.0f) {
                                Vector3 p = transform.position;
                                p += normal*bounceSurfaceOfs;
                                mBody.MovePosition(p);
                            }

                            Vector3 reflVel = Vector3.Reflect(mBody.velocity, normal);

                            if(bounceRigidbodyApplyVelocity)
                                mBody.velocity = reflVel;

                            //TODO: this is only for 2D
                            switch(forceBounce) {
                                case ForceBounceType.ContactNormal:
                                    mActiveForce.Set(normal.x, normal.y, 0.0f);
                                    mActiveForce.Normalize();
                                    mActiveForce *= force;
                                    break;

                                case ForceBounceType.ReflectDir:
                                    mActiveForce.Set(reflVel.x, reflVel.y, 0.0f);
                                    mActiveForce.Normalize();
                                    mActiveForce *= force;
                                    break;

                                case ForceBounceType.ReflectDirXOnly:
                                    if(Mathf.Abs(reflVel.x) > float.Epsilon) {
                                        mActiveForce.Set(Mathf.Sign(reflVel.x), 0.0f, 0.0f);
                                        mActiveForce *= force;
                                    }
                                    break;

                                case ForceBounceType.ContactNormalXOnly:
                                    if(Mathf.Abs(normal.x) > float.Epsilon) {
                                        mActiveForce.Set(normal.x, 0.0f, 0.0f);
                                        mActiveForce.Normalize();
                                        mActiveForce *= force;
                                    }
                                    break;
                            }

                            if(bounceSurfaceOfs != 0.0f) {
                                Vector3 p = transform.position;
                                p += normal*bounceSurfaceOfs;
                                mBody.MovePosition(p);
                            }
                        }

                        //mActiveForce = Vector3.Reflect(mActiveForce, normal);
                    }

                    if(maxBounce > 0)
                        mCurBounce++;

                    if(contactSfx && contactSfx.gameObject.activeInHierarchy)
                        contactSfx.Play();
                }
                break;

            case ContactType.BounceRotate:
                if(maxBounce > 0 && mCurBounce == maxBounce)
                    state = (int)State.Dying;
                else {
                    if(simple) {
                        mCurVelocity = Quaternion.AngleAxis(bounceRotateAngle, Vector3.forward) * mCurVelocity;

                        if(bounceSurfaceOfs != 0.0f) {
                            Vector3 p = transform.position;
                            p += normal*bounceSurfaceOfs;
                            transform.position = p;
                        }
                    }
                    else {
                        mActiveForce = Quaternion.AngleAxis(bounceRotateAngle, Vector3.forward) * mCurVelocity;

                        if(mBody != null) {
                            mBody.velocity = Quaternion.AngleAxis(bounceRotateAngle, Vector3.forward) * mBody.velocity;

                            if(bounceSurfaceOfs != 0.0f) {
                                Vector3 p = transform.position;
                                p += normal*bounceSurfaceOfs;
                                mBody.MovePosition(p);
                            }
                        }
                    }

                    if(maxBounce > 0)
                        mCurBounce++;

                    if(contactSfx && contactSfx.gameObject.activeInHierarchy)
                        contactSfx.Play();
                }
                break;
        }

        //do damage
        //if(!explodeOnDeath && CheckTag(go.tag)) {
        //mDamage.CallDamageTo(go, pos, normal);
        //}
    }

    void ApplyDamage(GameObject go, Vector3 pos, Vector3 normal) {
        if(mDamage && !explodeOnDeath && CheckTag(go.tag)) {
            if(mDamage.CallDamageTo(go, pos, normal)) {
                if(damageExpireCount != -1) {
                    mCurDamageCount++;
                    if(mCurDamageCount == damageExpireCount)
                        state = (int)State.Dying;
                }
            }
        }
    }

    void OnCollisionEnter(Collision collision) {
        foreach(ContactPoint cp in collision.contacts) {
            mLastHit.col = cp.otherCollider;
            mLastHit.normal = cp.normal;
            mLastHit.point = cp.point;

            ApplyContact(cp.otherCollider.gameObject, cp.point, cp.normal);
            ApplyDamage(cp.otherCollider.gameObject, cp.point, cp.normal);

            if(contactType != ContactType.End && EndByContactMask.value != 0 && isAlive && ((1<<cp.otherCollider.gameObject.layer) & EndByContactMask) != 0) {
                state = (int)State.Dying;
            }
        }
    }

    void OnCollisionStay(Collision collision) {
        if(state != (int)State.Invalid && mDamage) {
            //do damage
            foreach(ContactPoint cp in collision.contacts) {
                ApplyDamage(cp.otherCollider.gameObject, cp.point, cp.normal);
            }
        }
    }

    /*void OnTrigger(Collider collider) {
        ApplyContact(collider.gameObject, -mover.dir);
    }*/

    void OnDecayEnd() {
        state = (int)State.Dying;
    }

    void OnSeekStart() {
        if((State)state != State.Dying) {
            if(mSeek) {
                if(seekUseForce) {
                    mSeekCurDir = mActiveForce.normalized;
                    mSeekCurDirVel = Vector3.zero;
                }

                state = (int)(seekUseForce ? State.SeekForce : State.Seek);
            }
            else
                state = (int)State.Active;
        }
    }

    void OnUpUpdate() {
        if(simple) {
            applyDirToUp.up = mCurVelocity;
        }
        else {
            if(mBody != null && mBody.velocity != Vector3.zero) {
                applyDirToUp.up = mBody.velocity;
            }
        }
    }

    protected void SimpleCheckContain() {
        Vector3 pos = mSphereColl.bounds.center;
        Collider[] cols = Physics.OverlapSphere(pos, mSphereColl.radius, simpleLayerMask);
        for(int i = 0, max = cols.Length; i < max; i++) {
            Collider col = cols[i];
            if(CheckTag(col.tag)) {
                Vector3 dir = (col.bounds.center - pos).normalized;
                ApplyContact(col.gameObject, pos, dir);
                ApplyDamage(col.gameObject, pos, dir);
            }
        }
    }

    protected void DoSimpleMove(Vector3 delta) {
        float d = delta.magnitude;

        if(d > 0.0f) {
            Vector3 pos = mSphereColl.bounds.center;
            Vector3 dir = new Vector3(delta.x / d, delta.y / d, delta.z / d);

            //check if hit something
            RaycastHit hit;
            if(Physics.SphereCast(pos, mSphereColl.radius, dir, out hit, d, simpleLayerMask)) {
                mLastHit.col = hit.collider;
                mLastHit.normal = hit.normal;
                mLastHit.point = hit.point;

                transform.position = hit.point + hit.normal * mSphereColl.radius;
                ApplyContact(hit.collider.gameObject, hit.point, hit.normal);
                ApplyDamage(hit.collider.gameObject, hit.point, hit.normal);
            }
            else {
                //try contain
                SimpleCheckContain();
            }
        }

        //make sure we are still active
        if(isAlive)
            transform.position = transform.position + delta;
    }

    void DoSimple() {
        Vector3 curV = mCurVelocity * mMoveScale;

        Vector3 delta = curV * Time.fixedDeltaTime;
        DoSimpleMove(delta);
    }

    void OnSuddenDeath() {
        Release();
    }

    protected virtual void FixedUpdate() {
        switch((State)state) {
            case State.Active:
                if(simple) {
                    DoSimple();
                }
                else {
                    if(mBody != null) {
                        if(speedLimit > 0.0f) {
                            float sqrSpd = mBody.velocity.sqrMagnitude;
                            if(sqrSpd > speedLimit * speedLimit) {
                                mBody.velocity = (mBody.velocity / Mathf.Sqrt(sqrSpd)) * speedLimit;
                            }
                        }

                        if(mActiveForce != Vector3.zero)
                            mBody.AddForce(mActiveForce * mMoveScale);
                    }
                }
                break;

            case State.SeekForce:
                if(mBody && mSeek != null) {
                    Vector3 pos = transform.position;
                    Vector3 dest = mSeek.position;
                    Vector3 _dir = dest - pos; _dir.z = 0.0f;
                    float dist = _dir.magnitude;
                    
                    if(dist > 0.0f) {
                        _dir /= dist;

                        if(seekAngleLimit < 360.0f) {
                            _dir = M8.MathUtil.DirCap(mInitDir, _dir, seekAngleLimit);
                        }

                        if(seekForceDelay > 0.0f)
                            mSeekCurDir = Vector3.SmoothDamp(mSeekCurDir, _dir, ref mSeekCurDirVel, seekForceDelay, Mathf.Infinity, Time.fixedDeltaTime);
                        else
                            mSeekCurDir = _dir;
                    }

                    if(speedLimit > 0.0f) {
                        float sqrSpd = mBody.velocity.sqrMagnitude;
                        if(sqrSpd > speedLimit * speedLimit) {
                            mBody.velocity = (mBody.velocity / Mathf.Sqrt(sqrSpd)) * speedLimit;
                        }
                    }

                    mBody.AddForce(mSeekCurDir * force * mMoveScale);
                }
                break;

            case State.Seek:
                if(simple) {
                    if(mSeek != null) {
                        //steer torwards seek
                        Vector3 pos = transform.position;
                        Vector3 dest = mSeek.position;
                        Vector3 _dir = dest - pos;
                        float dist = _dir.magnitude;

                        if(dist > 0.0f) {
                            _dir /= dist;

                            //restrict
                            if(seekTurnAngleCap < 360.0f) {
                                _dir = M8.MathUtil.DirCap(mBody.velocity.normalized, _dir, seekTurnAngleCap);
                            }

                            mCurVelocity = M8.MathUtil.Steer(mBody.velocity, _dir * seekVelocity, seekVelocityCap, mMoveScale);
                        }
                    }

                    DoSimple();
                }
                else {
                    if(mBody != null && mSeek != null) {
                        //steer torwards seek
                        Vector3 pos = transform.position;
                        Vector3 dest = mSeek.position;
                        Vector3 _dir = dest - pos;
                        float dist = _dir.magnitude;

                        if(dist > 0.0f) {
                            _dir /= dist;

                            //restrict
                            if(seekTurnAngleCap < 360.0f) {
                                _dir = M8.MathUtil.DirCap(mBody.velocity.normalized, _dir, seekTurnAngleCap);
                            }

                            Vector3 force = M8.MathUtil.Steer(mBody.velocity, _dir * seekVelocity, seekVelocityCap, 1.0f);
                            mBody.AddForce(force, ForceMode.VelocityChange);
                        }
                    }
                }
                break;
        }
    }

    void OnDrawGizmos() {
        if(explodeRadius > 0.0f) {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere((explodeOfsTarget ? explodeOfsTarget : transform).localToWorldMatrix.MultiplyPoint(explodeOfs), explodeRadius);
        }
    }

    private void DoExplode() {
        Vector3 pos = (explodeOfsTarget ? explodeOfsTarget : transform).localToWorldMatrix.MultiplyPoint(explodeOfs);
        //float explodeRadiusSqr = explodeRadius * explodeRadius;

        //TODO: spawn fx

        Collider[] cols = Physics.OverlapSphere(pos, explodeRadius, explodeMask.value);

        foreach(Collider col in cols) {
            if(col != null && col.GetComponent<Rigidbody>() != null && CheckTag(col.gameObject.tag)) {
                //hurt?
                col.GetComponent<Rigidbody>().AddExplosionForce(explodeForce, pos, explodeRadius, 0.0f, ForceMode.Force);

                //float distSqr = (col.transform.position - pos).sqrMagnitude;

                mDamage.CallDamageTo(col.gameObject, pos, (col.bounds.center - pos).normalized);
            }
        }
    }
}
