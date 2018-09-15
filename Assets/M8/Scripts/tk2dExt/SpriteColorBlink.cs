using UnityEngine;
using System.Collections;

[AddComponentMenu("M8/tk2D/ColorBlink")]
public class SpriteColorBlink : MonoBehaviour {
    public tk2dBaseSprite sprite;
    public Color color;
    public float delay;

    private Color mOrigColor;
    private WaitForSeconds mWait;
    private bool mStarted;
    private bool mBlinkActive;

    void OnEnable() {
        if(mStarted && !mBlinkActive)
            StartCoroutine(DoBlink());
    }

    void OnDisable() {
        if(mStarted) {
            mBlinkActive = false;
            StopAllCoroutines();

            if(sprite != null)
                sprite.color = mOrigColor;
        }
    }

    void Awake() {
        if(sprite == null)
            sprite = GetComponent<tk2dBaseSprite>();

        mOrigColor = sprite.color;

        mWait = new WaitForSeconds(delay);
    }

    void Start() {
        mStarted = true;
        StartCoroutine(DoBlink());
    }

    IEnumerator DoBlink() {
        mBlinkActive = true;

        while(mBlinkActive) {
            sprite.color = color;

            yield return mWait;

            sprite.color = mOrigColor;

            yield return mWait;
        }
    }
}
