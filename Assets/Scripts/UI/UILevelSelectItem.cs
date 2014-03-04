using UnityEngine;
using System.Collections;

public class UILevelSelectItem : MonoBehaviour {
    public string level;

    public string gitGirlPortraitRef = "gitGirl_";

    public tk2dSpriteAnimation animRef;

    public UISprite portrait;
    public GameObject inactive;

    public GameObject finalActiveGO;

    public GameObject infoSubActiveGO;

    private bool mIsFinalLevel = false;
    private UIEventListener mListener;

    public UIEventListener listener { get { return mListener; } }

    public bool isCompleted {
        get {
            return inactive.activeSelf;
        }
    }

    public bool isFinalUnlock {
        get {
            return mIsFinalLevel && finalActiveGO.activeSelf;
        }
    }

    public void Init() {
        mIsFinalLevel = false;

        if(inactive) {
            inactive.SetActive(string.IsNullOrEmpty(level) ? false : LevelController.IsLevelComplete(level));
        }
    }

    public void InitFinalLevel(UILevelSelectItem[] items, UILevelSelectItem exclude) {
        mIsFinalLevel = true;

        int completeCount = 0;
        for(int i = 0; i < items.Length; i++) {
            if(items[i] != this && items[i] != exclude && items[i].isCompleted)
                completeCount++;
        }

        finalActiveGO.SetActive(completeCount == items.Length - 2);
    }

    public bool Click(tk2dSpriteAnimator animSpr, AnimatorData toPlay, string take) {
        if(mIsFinalLevel) {
            //if unlocked, load level
            if(finalActiveGO.activeSelf) {
                Main.instance.sceneManager.LoadScene(Scenes.finalStages);
                return true;
            }
        }
        else {
            if(inactive.activeSelf) {
                Main.instance.sceneManager.LoadScene(level);
                return true;
            }
            else {
                //start intro
                UIModalManager.instance.ModalCloseAll();
                animSpr.Library = animRef;
                LevelSelectCharacterControl.instance.toScene = level;
                LevelSelectCharacterControl.instance.SetAnimWatch(toPlay);
                toPlay.Play(take);
                return true;
            }
        }

        return false;
    }

    public void Selected() {
    }

    void Awake() {
        mListener = GetComponent<UIEventListener>();

        if(finalActiveGO)
            finalActiveGO.SetActive(false);
    }
}
