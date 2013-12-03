using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Use to make entity blink when damaged
/// </summary>
public class EntityDamageBlinkerSprite : MonoBehaviour {
    public float blinkDelay = 0.2f;
    public bool invulOnBlink = false;

    private SpriteColorBlink[] mBlinks;

    private EntityBase mEnt;
    private Stats mStats;

    private bool mStarted;
    private bool mNoBlinking; //NOTE: this is reset on disable

    public bool noBlinking { get { return mNoBlinking; } set { mNoBlinking = value; } }
    public SpriteColorBlink[] blinks { get { return mBlinks; } }

    void OnEnable() {
        if(mStarted) {
            if(mEnt && mEnt.isBlinking)
                OnEntityBlink(mEnt, true);
        }
    }

    void OnDisable() {
        mNoBlinking = false;

        if(mStarted) {
            if(mEnt)
                OnEntityBlink(mEnt, false);
        }
    }

    void Awake() {
        mBlinks = GetComponentsInChildren<SpriteColorBlink>(true);
        foreach(SpriteColorBlink blinker in mBlinks)
            blinker.enabled = false;

        mEnt = GetComponent<EntityBase>();
        mEnt.setBlinkCallback += OnEntityBlink;

        mStats = GetComponent<Stats>();
        if(mStats)
            mStats.changeHPCallback += OnStatsHPChange;
    }

    void Start() {
        mStarted = true;
    }

    void OnStatsHPChange(Stats stat, float delta) {
        if(mEnt.gameObject.activeInHierarchy) {
            if(stat.curHP > 0.0f && delta < 0.0f) {
                mEnt.Blink(blinkDelay);
            }
        }
    }

    void OnEntityBlink(EntityBase ent, bool b) {
        if(!mNoBlinking) {
            for(int i = 0, max = mBlinks.Length; i < max; i++) {
                mBlinks[i].enabled = b;
            }
        }

        if(invulOnBlink && mStats) {
            mStats.isInvul = b;
        }
    }
}
