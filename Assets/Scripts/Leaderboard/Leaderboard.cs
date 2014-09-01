using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Leaderboard : MonoBehaviour {
    public interface IService {
        /// <summary>
        /// Return true if we are allowed to process leaderboard stuff (if user is properly logged in, etc)
        /// </summary>
        bool LeaderboardAllow();

        /// <summary>
        /// Return true if we are still working on something
        /// </summary>
        bool LeaderboardIsWorking();

        /// <summary>
        /// Called when processing new data.
        /// </summary>
        void LeaderboardProcessData(string boardName, string text, int score);
    }

    private struct DataProcess {
        public string boardName;
        public string text;
        public int score;
    }

    private static Leaderboard mInstance;

    private List<IService> mServices = new List<IService>(4);
    private Queue<DataProcess> mProcess = new Queue<DataProcess>(10);

    public static Leaderboard instance { get { return mInstance; } }

    public void RegisterService(IService service) {
        mServices.Add(service);
    }

    public void UnregisterService(IService service) {
        mServices.Remove(service);
    }

    public void PostScore(string aBoardName, string aText, int aScore) {
        if(aScore > 0)
            mProcess.Enqueue(new DataProcess() { boardName = aBoardName, text = aText, score = aScore });
        else
            Debug.Log("score is "+aScore+" ??? ");
    }

    void OnDestroy() {
        if(mInstance == this) {
            mInstance = null;
        }
    }

    void Awake() {
        if(mInstance == null) {
            mInstance = this;
        }
    }
	
	// Update is called once per frame
	void Update() {
        if(mProcess.Count > 0) {
            DataProcess dat = mProcess.Peek();

            int numReady = 0;
            int numInvalid = 0;
            for(int i = 0; i < mServices.Count; i++) {
                IService service = mServices[i];
                if(!service.LeaderboardIsWorking()) {
                    if(service.LeaderboardAllow())
                        numReady++;
                    else
                        numInvalid++;
                }
            }

            if(numInvalid == mServices.Count) {
                mProcess.Dequeue();
            }
            else if(numReady == mServices.Count - numInvalid) {
                for(int i = 0; i < mServices.Count; i++) {
                    IService service = mServices[i];
                    service.LeaderboardProcessData(dat.boardName, dat.text, dat.score);
                }

                mProcess.Dequeue();
            }
        }
	}
}
