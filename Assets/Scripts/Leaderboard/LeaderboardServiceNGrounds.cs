using UnityEngine;
using System.Collections;

public class LeaderboardServiceNGrounds : MonoBehaviour, Leaderboard.IService {

    private Newgrounds mNG;
    private bool mCurBoardProcess;
    private bool mScoreProcessing;
    private string mCurBoardName;
    private int mCurBoardScore;

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
        if(mCurBoardProcess) {
            if(mNG.HasStarted() && !mNG.IsWorking()) {
                if(!mScoreProcessing) {
                    StartCoroutine(mNG.postScore(mCurBoardScore, mCurBoardName));
                    mScoreProcessing = true;
                }
                else
                    mCurBoardProcess = false;
            }
        }
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
    public void LeaderboardProcessData(string boardName, int score) {
        mCurBoardProcess = true;
        mCurBoardName = boardName;
        mCurBoardScore = score;

        if(mNG.HasStarted() && !mNG.IsWorking()) {
            StartCoroutine(mNG.postScore(score, boardName));
            mScoreProcessing = true;
        }
    }
}
