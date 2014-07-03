using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AchievementServiceNGrounds : MonoBehaviour, Achievement.IService {
    public AchievementServiceOffline offlineAchieve;

    private enum Status {
        Uninitialized,
        RetrieveMedals,
        ProcessData,
        RetryWait,
        Wait,
        Error,
        None
    }

#if !OUYA
    private Newgrounds mNG;
#endif

    private const float waitDelay = 1.0f;
    private const int retryCount = 5;

    private Status mStatus = Status.Uninitialized;
    private Achievement.Data mData;
    private int mCurRetry = 0;
    private float mLastTime;

    void Awake() {
        Achievement.instance.RegisterService(this);

#if !OUYA
        mNG = GetComponent<Newgrounds>();
#endif
    }

	// Update is called once per frame
	void Update () {
#if OUYA
        return;
#else
        switch(mStatus) {
            case Status.Uninitialized:
                if(mNG.HasStarted()) {
                    StartCoroutine(mNG.loadSettings());
                    mStatus = Status.RetrieveMedals;
                }
                break;

            case Status.RetrieveMedals:
                if(!mNG.IsWorking()) {
                    if(mData != null) {
                        StartCoroutine(mNG.unlockMedal(mData.title));
                        mStatus = Status.ProcessData;
                    }
                    else {
                        mStatus = Status.None;
                        mLastTime = Time.time;

                        //sync from offline achievement
                        if(offlineAchieve) {
                            Achievement achieve = Achievement.instance;
                            Achievement.Data[] achieveDat = achieve.data;

                            List<Achievement.Data> syncUnlockAchieve = new List<Achievement.Data>(achieveDat.Length);

                            for(int i = 0; i < achieveDat.Length; i++) {
                                if(!AchievementIsUnlocked(achieveDat[i])) {
                                    //check if it's unlocked in offline
                                    if(offlineAchieve.AchievementIsUnlocked(achieveDat[i]))
                                        syncUnlockAchieve.Add(achieveDat[i]);
                                }
                            }

                            //queue up unlocks
                            for(int i = 0; i < syncUnlockAchieve.Count; i++) {
                                achieve.NotifyUpdate(syncUnlockAchieve[i], 0, true);
                            }
                            //
                        }
                    }
                }
                break;

            case Status.ProcessData:
                if(!mNG.IsWorking()) {
                    if(!mNG.success) {
                        if(mCurRetry == retryCount) {
                            //TODO: tell the user the bad news
                            UIModalMessage.Open("Server Error: "+mNG.errorCode, mNG.errorMessage, null);
                            mStatus = Status.Error;
                        }
                        else {
                            mCurRetry++;
                            mLastTime = Time.time;
                            mStatus = Status.RetryWait;
                        }
                    }
                    else {
                        mStatus = Status.None;
                        mLastTime = Time.time;
                    }
                }
                break;

            case Status.Wait:
                if(mNG.HasStarted() && !mNG.IsWorking()) {
                    StartCoroutine(mNG.unlockMedal(mData.title));
                    mStatus = Status.ProcessData;
                }
                break;

            case Status.RetryWait:
                if(Time.time - mLastTime > waitDelay) {
                    if(mNG.IsWorking() || !mNG.HasStarted())
                        mStatus = Status.Wait;
                    else {
                        StartCoroutine(mNG.unlockMedal(mData.title));
                        mStatus = Status.ProcessData;
                    }
                }
                break;
        }
#endif
	}

    public bool AchievementAllow() {
#if OUYA
        return false;
#else
        return mNG.IsLoggedIn();
#endif
    }

    /// <summary>
    /// Return true if we are ready to process new data.  Should return false if processing a data or is still initializing.
    /// </summary>
    public bool AchievementIsReady() {
        return (mStatus == Status.None || mStatus == Status.Error) && Time.time - mLastTime > waitDelay;
    }
    
    /// <summary>
    /// Return the current data being processed, return null if no data to process.
    /// </summary>
    public Achievement.Data AchievementCurrentData() {
        return mData;
    }
    
    /// <summary>
    /// Get the current status of data being processed.
    /// </summary>
    public Achievement.Status AchievementCurrentStatus() {
        if(mStatus != Status.None)
            return Achievement.Status.Processing;

        return Achievement.Status.Complete;
    }
    
    /// <summary>
    /// Check to see if given achievement has already been completed.
    /// </summary>
    public bool AchievementIsUnlocked(Achievement.Data data) {
#if OUYA
        return false;
#else
        if(mStatus == Status.Uninitialized || mStatus == Status.RetrieveMedals)
            return false;

        //Debug.Log("Newgrounds Check Medal Complete: "+data.title);

        int ind = mNG.findMedal(data.title);

        //Debug.Log("Newgrounds Medal Index: "+ind+" complete? "+mNG.Medals[ind].unlocked);

        return mNG.Medals[ind].unlocked;
#endif
    }
    
    /// <summary>
    /// Called when processing new data.
    /// </summary>
    public void AchievementProcessData(Achievement.Data data, int progress, bool complete) {
        mData = data;
        if(mStatus == Status.None) {
#if !OUYA
            //Debug.Log("Newgrounds Unlocking Medal: "+data.title);
            if(mNG.IsWorking() || !mNG.HasStarted())
                mStatus = Status.Wait;
            else {
                StartCoroutine(mNG.unlockMedal(data.title));
                mStatus = Status.ProcessData;
            }
#endif
            mCurRetry = 0;
        }
    }
}
