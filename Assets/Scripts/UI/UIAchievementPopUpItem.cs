using UnityEngine;
using System.Collections;

public class UIAchievementPopUpItem : MonoBehaviour {
    public UISprite image;
    public UILabel text;

    private AnimatorData mAnimDat;

    public AnimatorData animDat { get { return mAnimDat; } }

    void Awake() {
        mAnimDat = GetComponent<AnimatorData>();
    }
}
