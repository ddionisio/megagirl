using UnityEngine;
using System.Collections;

public class AchievementServiceNGrounds : MonoBehaviour, Achievement.IService {
    private enum Status {
        Uninitialized,
        RetrieveMedals,
        ProcessData,
        RetryWait,
        Wait,
        Error,
        None
    }

    private Newgrounds mNG;

    private const float waitDelay = 1.0f;
    private const int retryCount = 5;

    private Status mStatus = Status.Uninitialized;
    private Achievement.Data mData;
    private int mCurRetry = 0;
    private float mLastTime;

    void Awake() {
        Achievement.instance.RegisterService(this);

        mNG = GetComponent<Newgrounds>();
    }

	// Update is called once per frame
	void Update () {
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
	}

    public bool AchievementAllow() {
        return mNG.IsLoggedIn();
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
        if(mStatus == Status.Uninitialized || mStatus == Status.RetrieveMedals)
            return false;

        //Debug.Log("Newgrounds Check Medal Complete: "+data.title);

        int ind = mNG.findMedal(data.title);

        //Debug.Log("Newgrounds Medal Index: "+ind+" complete? "+mNG.Medals[ind].unlocked);

        return mNG.Medals[ind].unlocked;
    }
    
    /// <summary>
    /// Called when processing new data.
    /// </summary>
    public void AchievementProcessData(Achievement.Data data, int progress, bool complete) {
        mData = data;
        if(mStatus == Status.None) {
            //Debug.Log("Newgrounds Unlocking Medal: "+data.title);
            if(mNG.IsWorking() || !mNG.HasStarted())
                mStatus = Status.Wait;
            else {
                StartCoroutine(mNG.unlockMedal(data.title));
                mStatus = Status.ProcessData;
            }

            mCurRetry = 0;
        }
    }
}
