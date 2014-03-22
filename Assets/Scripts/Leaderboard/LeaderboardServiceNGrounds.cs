using UnityEngine;
using System.Collections;

public class LeaderboardServiceNGrounds : MonoBehaviour, Leaderboard.IService {

    private Newgrounds mNG;
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
        
        mNG = GetComponent<Newgrounds>();
    }

	// Update is called once per frame
	void Update () {
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
	}

    public bool LeaderboardAllow() {
        return mNG.IsLoggedIn();
    }

    /// <summary>
    /// Return true if we are still working on something
    /// </summary>
    public bool LeaderboardIsWorking() {
        return !mNG.HasStarted() || mNG.IsWorking() || mCurBoardProcess;
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

        if(mNG.HasStarted()) {
            if(!mNG.IsWorking()) {
                StartCoroutine(mNG.postScore(mCurBoardScore, mCurBoardName));
                mScoreProcessing = true;
            }
        }
    }
}
