using UnityEngine;
using System.Collections;

public class WeaponLightning : Weapon {
    public AnimatorData fireActiveAnimDat;

    public float radius;

    public Transform strikeHolder;

    public GameObject reticle;

    public LayerMask masks;

    private GameObject[] mStrikes;
    private tk2dTiledSprite[][] mStrikeTileSprites;

    private Damage mDmg;
    private float mDefaultDmgAmt;
    private Collider[] mStruckCols;
    private int mStrikeActives;

    protected override Projectile CreateProjectile(int chargeInd, Transform seek) {
        if(mStrikeActives == 0) {
            PerformStrike(null, spawnPoint, chargeInd);
            if(mStrikeActives > 0) {
                if(fireActiveAnimDat) {
                    fireActiveAnimDat.Play("fire");
                }

                currentEnergy -= charges[chargeInd].energyCost;
            }
        }
        return null;
    }

    public override void FireStart() {
        base.FireStart();
    }

    protected override void OnDisable() {
        mStrikeActives = 0;

        for(int i = 0, max = mStrikes.Length; i < max; i++) {
            if(mStrikes[i])
                mStrikes[i].SetActive(false);
        }

        if(reticle)
            reticle.SetActive(false);

        base.OnDisable();
    }

    protected override void Awake() {
        base.Awake();

        mDmg = GetComponent<Damage>();
        mDefaultDmgAmt = mDmg.amount;

        mStrikes = new GameObject[strikeHolder.childCount];

        mStrikeTileSprites = new tk2dTiledSprite[mStrikes.Length][];
        for(int i = 0; i < mStrikes.Length; i++) {
            mStrikes[i] = strikeHolder.GetChild(i).gameObject;
            mStrikeTileSprites[i] = mStrikes[i].GetComponentsInChildren<tk2dTiledSprite>(true);
            mStrikes[i].SetActive(false);
        }

        mStruckCols = new Collider[mStrikes.Length];

        reticle.transform.parent = null;
        reticle.SetActive(false);
    }

    void PerformStrike(Collider aCol, Vector3 pos, int chargeInd) {
        if(mStrikeActives >= mStrikes.Length)
            return;

        mDmg.amount = mDefaultDmgAmt + ((float)chargeInd);

        Collider[] cols = Physics.OverlapSphere(pos, radius, masks);
        if(cols != null && cols.Length > 0) {
            //get nearest collider
            Vector3 p = Vector3.zero;
            Vector3 dir = Vector3.zero;
            Collider col = null;
            Stats colStat = null;
            float nearSqr = Mathf.Infinity;

            for(int cI = 0, cMax = cols.Length; cI < cMax; cI++) {
                if(System.Array.IndexOf(mStruckCols, cols[cI], 0, mStrikeActives) != -1)
                    continue;

                if(cols[cI] != aCol && cols[cI].gameObject.activeInHierarchy) {
                    Vector3 _p = cols[cI].bounds.center;
                    Vector3 _dir = _p - pos;
                    float _dist = _dir.sqrMagnitude;
                    if(_dist < nearSqr) {
                        Stats stat = cols[cI].GetComponent<Stats>();
                        if(stat && stat.CanDamage(mDmg)) {
                            p = _p;
                            dir = _dir;
                            col = cols[cI];
                            colStat = stat;
                            nearSqr = _dist;
                        }
                    }
                }
            }

            if(col == null)
                return;

            tk2dCamera cam = CameraController.instance.tk2dCam;

            float dist = Mathf.Sqrt(nearSqr);
            dir /= dist;

            if(mDmg.CallDamageTo(colStat, p, (p - pos).normalized)) {
                mStrikes[mStrikeActives].SetActive(true);
                mStrikes[mStrikeActives].transform.parent = null;
                mStrikes[mStrikeActives].transform.position = pos;
                mStrikes[mStrikeActives].transform.localScale = Vector3.one;
                mStrikes[mStrikeActives].transform.up = dir;

                for(int i = 0, max = mStrikeTileSprites[mStrikeActives].Length; i < max; i++) {
                    Vector2 dim = mStrikeTileSprites[mStrikeActives][i].dimensions;
                    dim.y = dist * cam.CameraSettings.orthographicPixelsPerMeter;
                    mStrikeTileSprites[mStrikeActives][i].dimensions = dim;
                }

                mStruckCols[mStrikeActives] = col;
                                
                mStrikeActives++;

                if(chargeInd > 0 && mStrikeActives + chargeInd <= mStrikes.Length)
                    PerformStrike(col, p, chargeInd - 1);
            }
        }
    }

    void Update() {
        if(mStrikeActives > 0) {
            mStrikeActives = 0;
            for(int i = 0, max = mStrikes.Length; i < max; i++) {
                if(mStrikes[i].activeInHierarchy)
                    mStrikeActives++;
                else {
                    mStrikes[i].transform.parent = strikeHolder;
                    mStruckCols[i] = null;
                }
            }

            reticle.SetActive(false);
        }
        else {
            //set reticle to nearest damageable target
            Collider reticleTarget = null;
            Collider[] cols = Physics.OverlapSphere(spawnPoint, radius, masks);
            if(cols != null && cols.Length > 0) {
                //get nearest collider
                float nearSqr = Mathf.Infinity;

                for(int cI = 0, cMax = cols.Length; cI < cMax; cI++) {
                    if(cols[cI].gameObject.activeInHierarchy) {
                        Vector3 _p = cols[cI].bounds.center;
                        Vector3 _dir = _p - spawnPoint;
                        float _dist = _dir.sqrMagnitude;
                        if(_dist < nearSqr) {
                            Stats stat = cols[cI].GetComponent<Stats>();
                            if(stat && stat.CanDamage(mDmg)) {
                                reticleTarget = cols[cI];
                                nearSqr = _dist;
                            }
                        }
                    }
                }
            }

            if(reticleTarget) {
                reticle.SetActive(true);
                Vector3 pos = reticleTarget.bounds.center; pos.z = 0;
                reticle.transform.position = pos;
            }
            else
                reticle.SetActive(false);
        }
    }

    void OnDrawGizmosSelected() {
        if(radius > 0.0f) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(spawnPoint, radius);
        }
    }
}
