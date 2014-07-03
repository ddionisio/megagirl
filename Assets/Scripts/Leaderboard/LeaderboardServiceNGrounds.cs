using UnityEngine;
using System.Collections;

public class LeaderboardServiceNGrounds : MonoBehaviour, Leaderboard.IService {
#if !OUYA
    private Newgrounds mNG;
#endif
    private bool mCurBoardProcess;
    private bool mScoreProcessing;
    private string mCurBoardName;
    private int mCurBoardScore;

    private const float retryWaitDelay = 1.0f;
    private const int retryCount = 5;

    private bool mRetry;
    private int mCurRetry = 0;
    private float mLastTime;

    void OnDestroy() {
        if(Leaderboard.instance) {
            Leaderboard.instance.UnregisterService(this);
        }
    }

    void Awake() {
        Leaderboard.instance.RegisterService(this);
#if !OUYA        
        mNG = GetComponent<Newgrounds>();
#endif
    }

	// Update is called once per frame
	void Update () {
#if !OUYA        
        if(mRetry) {
            if(Time.time - mLastTime > retryWaitDelay) {
                DoScoreProcess();
                mRetry = false;
            }
        }
        else if(mCurBoardProcess) {
            if(mNG.HasStarted()) {
                if(!mNG.IsWorking()) {
                    if(!mScoreProcessing) { //start process if we haven't
                        StartCoroutine(mNG.postScore(mCurBoardScore, mCurBoardName));
                        mScoreProcessing = true;
                    }
                    else { //completed
                        if(!mNG.success) {
                            if(mCurRetry == retryCount) {
                                //TODO: tell the user the bad news
                                UIModalMessage.Open("Server Error: "+mNG.errorCode, mNG.errorMessage, null);
                                mCurBoardProcess = false;
                            }
                            else {
                                mCurRetry++;
                                mLastTime = Time.time;
                                mRetry = true;
                                mScoreProcessing = false;
                            }
                        }
                        else {
                            mCurBoardProcess = false;
                        }
                    }
                }
            }
        }
#endif
	}

    public bool LeaderboardAllow() {
#if OUYA
        return false;
#else
        return mNG.IsLoggedIn();
#endif
    }

    /// <summary>
    /// Return true if we are still working on something
    /// </summary>
    public bool LeaderboardIsWorking() {
#if OUYA
        return false;
#else
        return !mNG.HasStarted() || mNG.IsWorking() || mCurBoardProcess;
#endif
    }
    
    /// <summary>
    /// Called when processing new data.
    /// </summary>
    public void LeaderboardProcessData(string boardName, string text, int score) {
        mCurBoardProcess = true;
        mCurBoardName = boardName;
        mCurBoardScore = score;

        DoScoreProcess();
    }

    void DoScoreProcess() {
        mScoreProcessing = false;
#if !OUYA
        if(mNG.HasStarted()) {
            if(!mNG.IsWorking()) {
                StartCoroutine(mNG.postScore(mCurBoardScore, mCurBoardName));
                mScoreProcessing = true;
            }
        }
#endif
    }
}
