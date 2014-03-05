using UnityEngine;
using System.Collections;

public class AchievementServiceNGrounds : MonoBehaviour, Achievement.IService {
    private enum Status {
        Uninitialized,
        RetrieveMedals,
        ProcessData,
        None
    }

    private Newgrounds mNG;

    private Status mStatus = Status.Uninitialized;
    private Achievement.Data mData;

    void Awake() {
        Achievement.instance.RegisterService(this);

        mNG = GetComponent<Newgrounds>();
    }

	// Update is called once per frame
	void Update () {
        switch(mStatus) {
            case Status.Uninitialized:
                if(mNG.HasStarted()) {
                    StartCoroutine(mNG.getMedals());
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
                    }
                }
                break;

            case Status.ProcessData:
                if(!mNG.IsWorking())
                    mStatus = Status.None;
                break;
        }
	}

    /// <summary>
    /// Return true if we are ready to process new data.  Should return false if processing a data or is still initializing.
    /// </summary>
    public bool AchievementIsReady() {
        return mStatus == Status.None;
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
    public bool AchievementIsComplete(Achievement.Data data) {
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

            StartCoroutine(mNG.unlockMedal(data.title));
            mStatus = Status.ProcessData;
        }
    }
}
