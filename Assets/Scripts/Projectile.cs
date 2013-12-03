using UnityEngine;
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
        ReflectDirXOnly
    }

    public bool simple; //don't use rigidbody, make sure you have a sphere collider and set it to trigger
    public LayerMask simpleLayerMask;
    public string[] hitTags;
    public float startVelocity;
    public float startVelocityAddRand;
    public float force;
    [SerializeField]
    float speedLimit;
    public bool seekUseForce = false;
    public float seekForceDelay = 0.15f;
    public float seekStartDelay = 0.0f;
    public float seekVelocity;
    public float seekVelocityCap = 5.0f;
    public float seekAngleCap = 360.0f;
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
    public float explodeRadius;
    public ContactType contactType = ContactType.End;
    public ForceBounceType forceBounce = ForceBounceType.None;
    public int maxBounce = -1;
    public float bounceRotateAngle;
    public float bounceSurfaceOfs; //displace projectile slightly off surface based on normal
    public bool explodeOnDeath;
    public Transform applyDirToUp;
    public string deathSpawnGroup;
    public string deathSpawnType;

    /*public bool oscillate;
    public float oscillateForce;
    public float oscillateDelay;*/

    private bool mSpawning = false;
    protected Vector3 mActiveForce;
    protected Vector3 mDir = Vector3.zero;
    protected Transform mSeek = null;
    protected Vector3 mCurVelocity; //only use by simple

    private Damage mDamage;
    private float mDefaultSpeedLimit;
    protected SphereCollider mSphereColl;
    protected float mMoveScale = 1.0f;
    private Stats mStats;
    private int mCurBounce = 0;

    private Vector3 mSeekCurDir;
    private Vector3 mSeekCurDirVel;

    //private Vector2 mOscillateDir;
    //private bool mOscillateSwitch;

    public static Projectile Create(string group, string typeName, Vector3 startPos, Vector3 dir, Transform seek) {
        Projectile ret = Spawn<Projectile>(group, typeName, startPos);
        if(ret != null) {
            ret.mDir = dir;
            ret.mSeek = seek;
        }

        return ret;
    }

    public bool isAlive { get { return (State)state == State.Active || (State)state == State.Seek || (State)state == State.SeekForce; } }

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
            return rigidbody != null ? rigidbody.velocity : Vector3.zero;
        }

        set {
            if(simple)
                mCurVelocity = value;
            else if(rigidbody)
                rigidbody.velocity = value;
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

    public void SetSpeedLimit(float limit) {
        speedLimit = limit;
    }

    public void RevertSpeedLimit() {
        speedLimit = mDefaultSpeedLimit;
    }

    protected override void Awake() {
        base.Awake();

        mSphereColl = collider ? collider as SphereCollider : null;

        mDefaultSpeedLimit = speedLimit;

        if(rigidbody != null) {
            rigidbody.detectCollisions = false;
        }

        if(collider != null)
            collider.enabled = false;

        mDamage = GetComponent<Damage>();

        mStats = GetComponent<Stats>();
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

        if(releaseOnSleep) {
            activator.ForceActivate();
            Release();
        }
    }

    public override void SpawnFinish() {
        //Debug.Log("start dir: " + mStartDir);

        mSpawning = false;

        mCurBounce = 0;

        if(decayEnabled && decayDelay == 0) {
            OnDecayEnd();
        }
        else {
            //starting direction and force
            if(simple) {
                mCurVelocity = mDir * startVelocity;
            }
            else {
                if(rigidbody != null && mDir != Vector3.zero) {
                    //set velocity
                    if(startVelocityAddRand != 0.0f) {
                        rigidbody.velocity = mDir * (startVelocity + Random.value * startVelocityAddRand);
                    }
                    else {
                        rigidbody.velocity = mDir * startVelocity;
                    }

                    mActiveForce = mDir * force;
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
                applyDirToUp.up = mDir;
                InvokeRepeating("OnUpUpdate", 0.1f, 0.1f);
            }
        }
    }

    protected override void SpawnStart() {
        if(applyDirToUp && mDir != Vector3.zero) {
            applyDirToUp.up = mDir;
        }

        mSpawning = true;
    }

    public override void Release() {
        state = (int)State.Invalid;

        CancelInvoke();
        RevertSpeedLimit();
        
        PhysicsDisable();
        
        if(mStats) {
            mStats.Reset();
            mStats.isInvul = true;
        }

        mSpawning = false;

        base.Release();
    }

    protected override void StateChanged() {
        switch((State)state) {
            case State.Seek:
            case State.SeekForce:
            case State.Active:
                if(collider)
                    collider.enabled = true;

                if(rigidbody)
                    rigidbody.detectCollisions = true;

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

                    Invoke("Die", dieDelay);
                }
                else
                    Die();
                break;

            case State.Invalid:
                /*CancelInvoke();
                RevertSpeedLimit();

                PhysicsDisable();

                if(mStats) {
                    mStats.Reset();
                    mStats.isInvul = true;
                }*/
                break;
        }
    }

    void PhysicsDisable() {
        mCurVelocity = Vector3.zero;
        if(collider)
            collider.enabled = false;

        if(rigidbody) {
            rigidbody.detectCollisions = false;
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }
    }

    void Die() {
        if(!string.IsNullOrEmpty(deathSpawnGroup) && !string.IsNullOrEmpty(deathSpawnType)) {
            Vector2 p = transform.localToWorldMatrix.MultiplyPoint(explodeOfs);//explodeOnDeath ? transform.localToWorldMatrix.MultiplyPoint(explodeOfs) : transform.position;

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
            if(state == (int)State.Active || state == (int)State.Seek || state == (int)State.SeekForce)
                state = (int)State.Dying;
        }
    }

    protected virtual void ApplyContact(GameObject go, Vector3 pos, Vector3 normal) {
        switch(contactType) {
            case ContactType.End:
                state = (int)State.Dying;
                break;

            case ContactType.Stop:
                if(simple)
                    mCurVelocity = Vector3.zero;
                else if(rigidbody != null)
                    rigidbody.velocity = Vector3.zero;
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
                        if(rigidbody != null) {
                            if(bounceSurfaceOfs != 0.0f) {
                                Vector3 p = transform.position;
                                p += normal*bounceSurfaceOfs;
                                rigidbody.MovePosition(p);
                            }

                            Vector3 reflVel = Vector3.Reflect(rigidbody.velocity, normal);

                            rigidbody.velocity = reflVel;

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
                                    mActiveForce.Set(Mathf.Sign(reflVel.x), 0.0f, 0.0f);
                                    mActiveForce *= force;
                                    break;
                            }
                        }

                        //mActiveForce = Vector3.Reflect(mActiveForce, normal);
                    }

                    if(maxBounce > 0)
                        mCurBounce++;
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

                        if(rigidbody != null) {
                            rigidbody.velocity = Quaternion.AngleAxis(bounceRotateAngle, Vector3.forward) * rigidbody.velocity;

                            if(bounceSurfaceOfs != 0.0f) {
                                Vector3 p = transform.position;
                                p += normal*bounceSurfaceOfs;
                                rigidbody.MovePosition(p);
                            }
                        }
                    }

                    if(maxBounce > 0)
                        mCurBounce++;
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
            mDamage.CallDamageTo(go, pos, normal);
        }
    }

    void OnCollisionEnter(Collision collision) {
        foreach(ContactPoint cp in collision.contacts) {
            ApplyContact(cp.otherCollider.gameObject, cp.point, cp.normal);
            ApplyDamage(cp.otherCollider.gameObject, cp.point, cp.normal);
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
            if(rigidbody != null && rigidbody.velocity != Vector3.zero) {
                applyDirToUp.up = rigidbody.velocity;
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
                transform.position = hit.point + hit.normal * mSphereColl.radius;
                ApplyContact(hit.collider.gameObject, hit.point, hit.normal);
                ApplyDamage(hit.collider.gameObject, hit.point, hit.normal);
            }
            else {
                //try contain
                Collider[] cols = Physics.OverlapSphere(pos, mSphereColl.radius, simpleLayerMask);
                for(int i = 0, max = cols.Length; i < max; i++) {
                    Collider col = cols[i];
                    if(CheckTag(col.tag)) {
                        dir = (col.bounds.center - pos).normalized;
                        ApplyContact(col.gameObject, pos, dir);
                        ApplyDamage(col.gameObject, pos, dir);
                    }
                }
            }
        }

        //make sure we are still active
        if((State)state == State.Active || (State)state == State.Seek || (State)state == State.SeekForce)
            transform.position = transform.position + delta;
    }

    void DoSimple() {
        Vector3 curV = mCurVelocity * mMoveScale;

        if(speedLimit > 0) {
            float spdSqr = curV.sqrMagnitude;
            if(spdSqr > speedLimit * speedLimit) {
                curV = (curV / Mathf.Sqrt(spdSqr)) * speedLimit;
            }
        }

        Vector3 delta = curV * Time.fixedDeltaTime;
        DoSimpleMove(delta);
    }

    protected virtual void FixedUpdate() {
        switch((State)state) {
            case State.Active:
                if(simple) {
                    DoSimple();
                }
                else {
                    if(rigidbody != null) {
                        if(speedLimit > 0.0f) {
                            float sqrSpd = rigidbody.velocity.sqrMagnitude;
                            if(sqrSpd > speedLimit * speedLimit) {
                                rigidbody.velocity = (rigidbody.velocity / Mathf.Sqrt(sqrSpd)) * speedLimit;
                            }
                        }

                        if(mActiveForce != Vector3.zero)
                            rigidbody.AddForce(mActiveForce * mMoveScale);
                    }
                }
                break;

            case State.SeekForce:
                if(rigidbody && mSeek != null) {
                    Vector3 pos = transform.position;
                    Vector3 dest = mSeek.position;
                    Vector3 _dir = dest - pos;
                    float dist = _dir.magnitude;
                    
                    if(dist > 0.0f) {
                        _dir /= dist;
                        if(seekForceDelay > 0.0f)
                            mSeekCurDir = Vector3.SmoothDamp(mSeekCurDir, _dir, ref mSeekCurDirVel, seekForceDelay, Mathf.Infinity, Time.fixedDeltaTime);
                        else
                            mSeekCurDir = _dir;
                    }

                    if(speedLimit > 0.0f) {
                        float sqrSpd = rigidbody.velocity.sqrMagnitude;
                        if(sqrSpd > speedLimit * speedLimit) {
                            rigidbody.velocity = (rigidbody.velocity / Mathf.Sqrt(sqrSpd)) * speedLimit;
                        }
                    }

                    rigidbody.AddForce(mSeekCurDir * force * mMoveScale);
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
                            if(seekAngleCap < 360.0f) {
                                _dir = M8.MathUtil.DirCap(rigidbody.velocity.normalized, _dir, seekAngleCap);
                            }

                            mCurVelocity = M8.MathUtil.Steer(rigidbody.velocity, _dir * seekVelocity, seekVelocityCap, mMoveScale);
                        }
                    }

                    DoSimple();
                }
                else {
                    if(rigidbody != null && mSeek != null) {
                        //steer torwards seek
                        Vector3 pos = transform.position;
                        Vector3 dest = mSeek.position;
                        Vector3 _dir = dest - pos;
                        float dist = _dir.magnitude;

                        if(dist > 0.0f) {
                            _dir /= dist;

                            //restrict
                            if(seekAngleCap < 360.0f) {
                                _dir = M8.MathUtil.DirCap(rigidbody.velocity.normalized, _dir, seekAngleCap);
                            }

                            Vector3 force = M8.MathUtil.Steer(rigidbody.velocity, _dir * seekVelocity, seekVelocityCap, 1.0f);
                            rigidbody.AddForce(force, ForceMode.VelocityChange);
                        }
                    }
                }
                break;
        }
    }

    void OnDrawGizmos() {
        if(explodeRadius > 0.0f) {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.localToWorldMatrix.MultiplyPoint(explodeOfs), explodeRadius);
        }
    }

    private void DoExplode() {
        Vector3 pos = transform.localToWorldMatrix.MultiplyPoint(explodeOfs);
        //float explodeRadiusSqr = explodeRadius * explodeRadius;

        //TODO: spawn fx

        Collider[] cols = Physics.OverlapSphere(pos, explodeRadius, explodeMask.value);

        foreach(Collider col in cols) {
            if(col != null && col.rigidbody != null && CheckTag(col.gameObject.tag)) {
                //hurt?
                col.rigidbody.AddExplosionForce(explodeForce, pos, explodeRadius, 0.0f, ForceMode.Force);

                //float distSqr = (col.transform.position - pos).sqrMagnitude;

                mDamage.CallDamageTo(col.gameObject, pos, (col.bounds.center - pos).normalized);
            }
        }
    }
}
