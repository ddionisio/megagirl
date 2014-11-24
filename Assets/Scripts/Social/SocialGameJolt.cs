using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SocialGameJolt : MonoBehaviour, Achievement.IService, Leaderboard.IService {
    public enum State {
        Uninitialized,
        RequireLogin,
        GrabUser,
        VerifyUser,

        Busy,

        None,
        Fail
    }

    public int gameID;
    public string privateKey;

    public string userName;
    public string userToken;
    public bool userGrabFromWeb = true;

    public AchievementServiceOffline offlineAchieve;

    private static SocialGameJolt mInstance;

    private bool mIsGuest;
    private State mState = State.Uninitialized;
    private GJTrophy[] mTrophies;
    private GJTable[] mScores;

    private Achievement.Data mCurAchieveDat;

    public State state { get { return mState; } }
    public bool isGuest { get { return mIsGuest; } }

    void OnLevelWasLoaded(int ind) {
        if(Application.loadedLevelName == Scenes.levelSelect)
            StartCoroutine(DoOfflineSync());
    }

    void OnDestroy() {
        if(mInstance == this) {
            GJAPI.Users.VerifyCallback -= OnVerifyUser;
            GJAPI.Trophies.GetAllCallback -= OnTrophiesGrab;
            GJAPI.Trophies.AddCallback -= OnTrophyAdd;
            GJAPI.Scores.GetTablesCallback -= OnScoresGrab;
            GJAPI.Scores.AddCallback -= OnScoreAdd;

            if(Achievement.instance) {
                Achievement.instance.UnregisterService(this);
            }

            if(Leaderboard.instance) {
                Leaderboard.instance.UnregisterService(this);
            }

            mInstance = null;
        }
    }

    void Awake() {
        if(mInstance == null) {
            mInstance = this;
            DontDestroyOnLoad(gameObject);

            GJAPI.Init(gameID, privateKey);

            GJAPI.Users.VerifyCallback += OnVerifyUser;
            GJAPI.Trophies.GetAllCallback += OnTrophiesGrab;
            GJAPI.Trophies.AddCallback += OnTrophyAdd;
            GJAPI.Scores.GetTablesCallback += OnScoresGrab;
            GJAPI.Scores.AddCallback += OnScoreAdd;

            if(Achievement.instance) {
                Achievement.instance.RegisterService(this);
            }
            
            if(Leaderboard.instance) {
                Leaderboard.instance.RegisterService(this);
            }

            if(userGrabFromWeb) {
                mState = State.GrabUser;
                GJAPIHelper.Users.GetFromWeb(OnGrabUser);
            }
            else {
                //TODO: login window
                if(!string.IsNullOrEmpty(userName)) {
                    if(string.IsNullOrEmpty(userToken)) {
                        mState = State.None;
                        mIsGuest = true;
                    }
                    else {
                        mState = State.VerifyUser;
                        mIsGuest = false;
                        GJAPI.Users.Verify(userName, userToken);
                    }
                }
                else
                    mState = State.RequireLogin;
            }
        }
        else
            DestroyImmediate(gameObject);
    }

    void OnGrabUser(string user, string token) {
        mIsGuest = string.IsNullOrEmpty(token);

        userName = user;
        userToken = token;

        mState = State.VerifyUser;

        GJAPI.Users.Verify(userName, userToken);
    }

    void OnVerifyUser(bool success) {
        if(success) {
            if(Achievement.instance) {
                //grab trophies
                GJAPI.Trophies.GetAll();
                mState = State.Busy;
            }
            else {
                mState = State.None;
            }
        }
        else {
            Debug.Log("Unable to verify user: "+userName);
            mState = State.Fail;
        }
        //todo: fail log
    }

    void OnTrophiesGrab(GJTrophy[] trophies) {
        mTrophies = trophies;

        for(int i = 0; i < trophies.Length; i++) {
            Debug.Log("Trophy: "+trophies[i].Id+" title: "+trophies[i].Title);
        }

        if(Leaderboard.instance) {
            GJAPI.Scores.GetTables();
        }
        else
            mState = State.None;
    }

    void OnTrophyAdd(bool success) {
        if(success) {
            Debug.Log("Process trophy: "+mCurAchieveDat.title);
            mState = State.None;
        }
        else {
            UIModalMessage.Open("GameJolt Error", "Unable to process trophy: "+mCurAchieveDat.title, null);
            mState = State.Fail;
        }
    }

    void OnScoreAdd(bool success) {
        if(success) {
            Debug.Log("Score added");
            mState = State.None;
        }
        else {
            UIModalMessage.Open("GameJolt Error", "Unable to post score.", null);
            mState = State.Fail;
        }
    }

    void OnScoresGrab(GJTable[] scores) {
        mScores = scores;

        mState = State.None;
    }

    /// <summary>
    /// Return true if we are allowed to process achievement (if user is properly logged in, etc)
    /// </summary>
    public bool AchievementAllow() {
        return !mIsGuest && mState != State.Fail;
    }
    
    /// <summary>
    /// Return true if we are ready to process new data.  Should return false if processing a data or is still initializing.
    /// </summary>
    public bool AchievementIsReady() {
        return mState == State.None;
    }
    
    /// <summary>
    /// Return the current data being processed, return null if no data to process.
    /// </summary>
    public Achievement.Data AchievementCurrentData() {
        return mCurAchieveDat;
    }
    
    /// <summary>
    /// Get the current status of data being processed.
    /// </summary>
    public Achievement.Status AchievementCurrentStatus() {
        switch(mState) {
            case State.None:
                return Achievement.Status.Complete;

            case State.Fail:
                return Achievement.Status.Error;
        }

        return Achievement.Status.Complete;
    }
    
    /// <summary>
    /// Check to see if given achievement has already been completed.
    /// </summary>
    public bool AchievementIsUnlocked(Achievement.Data data) {
        GJTrophy t = GetTrophy(data.id);
        if(t != null) {
            Debug.Log("Is achieved: "+data.title+": "+t.Achieved);
            return t.Achieved;
        }
        return false;
    }
    
    /// <summary>
    /// Called when processing new data.
    /// </summary>
    public void AchievementProcessData(Achievement.Data data, int progress, bool complete) {
        GJTrophy t = GetTrophy(data.id);
        if(t != null) {
            mCurAchieveDat = data;
            t.Achieved = complete;
            GJAPI.Trophies.Add(t.Id);
            mState = State.Busy;
        }
        else {
            Debug.Log("Trophy id: "+data.id+" not found");
            mCurAchieveDat = null;
        }
    }

    /// <summary>
    /// Return true if we are allowed to process leaderboard stuff (if user is properly logged in, etc)
    /// </summary>
    public bool LeaderboardAllow() {
        return mState != State.Fail;
    }
    
    /// <summary>
    /// Return true if we are still working on something
    /// </summary>
    public bool LeaderboardIsWorking() {
        return mState != State.None && mState != State.Fail;
    }
    
    /// <summary>
    /// Called when processing new data.
    /// </summary>
    public void LeaderboardProcessData(string boardName, string text, int score) {
        GJTable table = null;
        for(int i = 0; i < mScores.Length; i++) {
            if(mScores[i].Name == boardName) {
                table = mScores[i];
                break;
            }
        }

        if(table != null) {
            mState = State.Busy;

            if(mIsGuest)
                GJAPI.Scores.AddForGuest(text, (uint)score, "Guest", table.Id);
            else
                GJAPI.Scores.Add(text, (uint)score, table.Id);
        }
    }
    
    GJTrophy GetTrophy(int id) {
        for(int i = 0; i < mTrophies.Length; i++) {
            if(mTrophies[i].Id == id)
                return mTrophies[i];
        }

        return null;
    }

    IEnumerator DoOfflineSync() {
        //sync from offline achievement
        if(offlineAchieve) {
            WaitForFixedUpdate wait = new WaitForFixedUpdate();
            while(!AchievementIsReady())
                yield return wait;

            Achievement achieve = Achievement.instance;
            Achievement.Data[] achieveDat = achieve.data;

            List<Achievement.Data> syncUnlockAchieve = new List<Achievement.Data>(achieveDat.Length);

            for(int i = 0; i < achieveDat.Length; i++) {
                if(!AchievementIsUnlocked(achieveDat[i])) {
                    //check if it's unlocked in offline
                    if(offlineAchieve.AchievementIsUnlocked(achieveDat[i])) {
                        Debug.Log("Newgrounds Syncing from offline: "+achieveDat[i].title);
                        syncUnlockAchieve.Add(achieveDat[i]);
                    }
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
