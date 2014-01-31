using UnityEngine;
using System.Collections;

public class Stats : MonoBehaviour {
    public delegate void ChangeCallback(Stats stat, float delta);

    [System.Serializable]
    public class DamageMod {
        public Damage.Type type;
        public float val;
    }

    public float maxHP;

    public float damageAmp = 0.0f; //general amplification
    public float damageReduction = 0.0f;

    public DamageMod[] damageTypeAmp;
    public DamageMod[] damageTypeReduction;

    public bool stunImmune;

    public string deathTag = "Death";

    public int itemDropIndex = -1;
    public Vector3 itemDropOfs;

    public event ChangeCallback changeHPCallback;

    protected Damage mLastDamage;
    protected Vector3 mLastDamagePos;
    protected Vector3 mLastDamageNorm;

    protected float mCurHP;
    private bool mIsInvul;

    public float curHP {
        get { return mCurHP; }

        set {
            float v = Mathf.Clamp(value, 0, maxHP);
            if(mCurHP != v) {
                float prev = mCurHP;
                mCurHP = v;

                if(changeHPCallback != null)
                    changeHPCallback(this, mCurHP - prev);
            }
        }
    }

    public bool isInvul { get { return mIsInvul; } set { mIsInvul = value; } }

    public Damage lastDamageSource { get { return mLastDamage; } }

    /// <summary>
    /// This is the latest damage hit position when hp was reduced, set during ApplyDamage
    /// </summary>
    public Vector3 lastDamagePosition { get { return mLastDamagePos; } }

    /// <summary>
    /// This is the latest damage hit normal when hp was reduced, set during ApplyDamage
    /// </summary>
    public Vector3 lastDamageNormal { get { return mLastDamageNorm; } }

    public void AddDamageReduce(float amt) {
        for(int i = 0; i < damageTypeReduction.Length; i++) {
            damageTypeReduction[i].val += amt;
        }
    }

    public DamageMod GetDamageMod(DamageMod[] dat, Damage.Type type) {
        if(dat != null) {
            for(int i = 0, max = dat.Length; i < max; i++) {
                if(dat[i].type == type) {
                    return dat[i];
                }
            }
        }
        return null;
    }

    public bool CanDamage(Damage damage) {
        if(!mIsInvul) {
            float amt = damage.amount;

            if(damageAmp > 0.0f) {
                amt += amt * damageAmp;
            }
            
            if(damageReduction > 0.0f) {
                amt -= amt * damageReduction;
            }
            
            DamageMod damageAmpByType = GetDamageMod(damageTypeAmp, damage.type);
            if(damageAmpByType != null) {
                amt += damage.amount * damageAmpByType.val;
            }
            else {
                DamageMod damageReduceByType = GetDamageMod(damageTypeReduction, damage.type);
                if(damageReduceByType != null)
                    amt -= amt * damageReduceByType.val;
            }

            return amt > 0.0f;
        }
        return false;
    }

    protected float CalculateDamageAmount(Damage damage) {
        float amt = damage.amount;
        
        if(damageAmp > 0.0f) {
            amt += amt * damageAmp;
        }
        
        if(damageReduction > 0.0f) {
            amt -= amt * damageReduction;
        }
        
        DamageMod damageAmpByType = GetDamageMod(damageTypeAmp, damage.type);
        if(damageAmpByType != null) {
            amt += damage.amount * damageAmpByType.val;
        }
        else {
            DamageMod damageReduceByType = GetDamageMod(damageTypeReduction, damage.type);
            if(damageReduceByType != null)
                amt -= amt * damageReduceByType.val;
        }

        return amt;
    }

    public virtual bool ApplyDamage(Damage damage, Vector3 hitPos, Vector3 hitNorm) {
        mLastDamage = damage;
        mLastDamagePos = hitPos;
        mLastDamageNorm = hitNorm;

        if(!mIsInvul && mCurHP > 0.0f) {
            float amt = CalculateDamageAmount(damage);

            if(amt > 0.0f) {
                if(curHP - amt <= 0.0f && itemDropIndex >= 0) {
                    //Debug.Log("drop?");
                    ItemDropManager.instance.DoDrop(itemDropIndex, transform.localToWorldMatrix.MultiplyPoint(itemDropOfs));
                }

                curHP -= amt;

                return true;
            }
        }

        return false;
    }

    public virtual void Reset() {
        curHP = maxHP;
        mIsInvul = false;
        mLastDamage = null;
    }

    protected virtual void OnDestroy() {
        changeHPCallback = null;
    }

    protected virtual void Awake() {
        mCurHP = maxHP;
    }

    void OnCollisionEnter(Collision col) {
        if(col.gameObject.CompareTag(deathTag)) {
            curHP = 0;
        }
    }

    void OnTriggerEnter(Collider col) {
        if(col.gameObject.CompareTag(deathTag)) {
            curHP = 0;
        }
    }

    void OnDrawGizmosSelected() {
        if(itemDropIndex >= 0) {
            Color clr = Color.red;
            clr.a = 0.5f;
            Gizmos.color = clr;
            Gizmos.DrawSphere(transform.localToWorldMatrix.MultiplyPoint(itemDropOfs), 0.15f);
        }
    }
}
