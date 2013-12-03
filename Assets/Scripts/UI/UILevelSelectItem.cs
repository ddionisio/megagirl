using UnityEngine;
using System.Collections;

public class UILevelSelectItem : MonoBehaviour {
    public string level;

    public string gitGirlPortraitRef = "gitGirl_";

    public tk2dSpriteAnimation animRef;

    public UISprite portrait;
    public GameObject inactive;

    private bool mIsFinalLevel = false;
    private UIEventListener mListener;

    public UIEventListener listener { get { return mListener; } }

    public bool isFinalUnlock {
        get {
            return mIsFinalLevel && !inactive.activeSelf;
        }
    }

    public void Init() {
        mIsFinalLevel = false;

        inactive.SetActive(string.IsNullOrEmpty(level) ? false : LevelController.isLevelComplete(level));
    }

    public void InitFinalLevel(UILevelSelectItem[] items) {
        mIsFinalLevel = true;

        //check if all levels are completed
        //if not, set to mystery mode
        inactive.SetActive(true);
    }

    public void Click(tk2dSpriteAnimator animSpr, AnimatorData toPlay, string take) {
        if(mIsFinalLevel) {
            //if unlocked, load level
        }
        else {
            if(inactive.activeSelf) {
                Main.instance.sceneManager.LoadScene(level);
            }
            else {
                //start intro
                UIModalManager.instance.ModalCloseAll();
                animSpr.Library = animRef;
                LevelSelectCharacterControl.instance.toScene = level;
                LevelSelectCharacterControl.instance.SetAnimWatch(toPlay);
                toPlay.Play(take);
            }
        }
    }

    void Awake() {
        mListener = GetComponent<UIEventListener>();
    }
}
