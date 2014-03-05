using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UIAchievementPopUp : MonoBehaviour {
    private struct Data {
        public string imageRef;
        public string text;
    }

    public const float popUpInactiveCheckDelay = 0.2f;

    public Transform popUpHolder;

    public bool test;

    private int mPopUpCounter;
    private List<UIAchievementPopUpItem> mActives;
    private List<UIAchievementPopUpItem> mInactives;
    private Queue<Data> mPopUpQueue;

    private NGUILayoutFlow mLayout;

    private float mLastInactiveCheckTime;

    void OnDestroy() {
        if(Achievement.instance)
            Achievement.instance.popupCallback -= OnPopUp;
    }

    void Awake() {
        Achievement.instance.popupCallback += OnPopUp;

        mLayout = popUpHolder.GetComponent<NGUILayoutFlow>();

        int numChild = popUpHolder.childCount;

        mActives = new List<UIAchievementPopUpItem>(numChild);
        mInactives = new List<UIAchievementPopUpItem>(numChild);
        mPopUpQueue = new Queue<Data>(numChild);

        for(int i = 0; i < numChild; i++) {
            Transform c = popUpHolder.GetChild(i);
            mInactives.Add(c.GetComponent<UIAchievementPopUpItem>());
            c.gameObject.SetActive(false);
        }
    }
    	
	// Update is called once per frame
	void Update () {
        if(mActives.Count > 0 && Time.realtimeSinceStartup - mLastInactiveCheckTime > popUpInactiveCheckDelay) {
            for(int i = 0; i < mActives.Count; i++) {
                UIAchievementPopUpItem item = mActives[i];
                if(!item.animDat.isPlaying) {
                    item.gameObject.SetActive(false);
                    mInactives.Add(item);
                    mActives.RemoveAt(i);
                    i--;
                }
            }

            if(mActives.Count == 0)
                mPopUpCounter = 0;
        }

        if(mPopUpQueue.Count > 0 && mInactives.Count > 0) {
            Data d = mPopUpQueue.Dequeue();
            NewActive(d.imageRef, d.text);
        }

        if(test) {
            NewActive(null, mPopUpCounter.ToString());
            test = false;
        }
	}

    void NewActive(string imageRef, string title) {
        int ind = mInactives.Count-1;
        UIAchievementPopUpItem itm = mInactives[ind];
        
        mInactives.RemoveAt(ind);
        
        itm.name = mPopUpCounter.ToString();
        mPopUpCounter++;

        if(!string.IsNullOrEmpty(imageRef))
            itm.image.spriteName = imageRef;

        itm.text.text = title;
        
        mActives.Add(itm);
        
        itm.gameObject.SetActive(true);
        
        mLayout.Reposition();

        mLastInactiveCheckTime = Time.realtimeSinceStartup;
    }

    void OnPopUp(Achievement.Data data, int progress, bool complete) {
        if(mInactives.Count > 0) {
            NewActive(data.imageRef, data.title);
        }
        else {
            mPopUpQueue.Enqueue(new Data() { imageRef=data.imageRef, text=data.title });
        }
    }
}
