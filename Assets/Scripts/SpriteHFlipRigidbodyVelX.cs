using UnityEngine;
using System.Collections;

public class SpriteHFlipRigidbodyVelX : MonoBehaviour {
    public tk2dBaseSprite[] targets;
    public bool leftIsFlip;
    public float minX = 0.1f;

    private tk2dBaseSprite[] mSprites;

    public void SetFlip(bool flip) {
        for(int i = 0, max = mSprites.Length; i < max; i++) {
            mSprites[i].FlipX = flip ? leftIsFlip : !leftIsFlip;
        }
    }

    void Awake() {
        mSprites = targets != null && targets.Length > 0 ? targets : GetComponentsInChildren<tk2dBaseSprite>(true);
    }

	// Update is called once per frame
	void Update () {
        float vx = rigidbody.velocity.x;
        if(Mathf.Abs(vx) > minX) {
            float sign = Mathf.Sign(vx);
            for(int i = 0, max = mSprites.Length; i < max; i++) {
                SetFlip(sign < 1.0f);
            }
        }
	}
}
