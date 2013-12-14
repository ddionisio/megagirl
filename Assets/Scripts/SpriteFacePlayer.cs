using UnityEngine;
using System.Collections;

public class SpriteFacePlayer : MonoBehaviour {
    public tk2dBaseSprite[] targets;
    public bool leftIsFlip;
    
    private tk2dBaseSprite[] mSprites;

    private GameObject[] mPlayers;
    
    public void SetFlip(bool flip) {
        for(int i = 0, max = mSprites.Length; i < max; i++) {
            mSprites[i].FlipX = flip ? leftIsFlip : !leftIsFlip;
        }
    }
    
    void Awake() {
        mSprites = targets != null && targets.Length > 0 ? targets : GetComponentsInChildren<tk2dBaseSprite>(true);

        mPlayers = GameObject.FindGameObjectsWithTag("Player");
    }
        
    // Update is called once per frame
    void Update () {
        float nearDistX = Mathf.Infinity;
        Transform nearT = null;
        
        float x = transform.position.x;
        for(int i = 0, max = mPlayers.Length; i < max; i++) {
            if(mPlayers[i] && mPlayers[i].activeSelf) {
                Transform t = mPlayers[i].transform;
                float distX = Mathf.Abs(t.position.x - x);
                if(distX < nearDistX) {
                    nearT = t;
                    nearDistX = distX;
                }
            }
        }
        
        float dirX = nearT ? Mathf.Sign(nearT.position.x - transform.position.x) : 0.0f;
        bool flip = dirX < 0.0f;

        for(int i = 0, max = mSprites.Length; i < max; i++) {
            SetFlip(flip);
        }
    }
}
