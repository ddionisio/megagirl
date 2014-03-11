using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Leaderboard : MonoBehaviour {
    public interface IService {
        /// <summary>
        /// Return true if we are still working on something
        /// </summary>
        bool LeaderboardIsWorking();

        /// <summary>
        /// Called when processing new data.
        /// </summary>
        void LeaderboardProcessData(string boardName, int score);
    }

    private struct DataProcess {
        public string boardName;
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

    public void PostScore(string aBoardName, int aScore) {
        mProcess.Enqueue(new DataProcess() { boardName = aBoardName, score = aScore });
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
            for(int i = 0; i < mServices.Count; i++) {
                IService service = mServices[i];
                if(!service.LeaderboardIsWorking()) {
                    numReady++;
                }
            }

            if(numReady == mServices.Count) {
                for(int i = 0; i < mServices.Count; i++) {
                    IService service = mServices[i];
                    service.LeaderboardProcessData(dat.boardName, dat.score);
                }

                mProcess.Dequeue();
            }
        }
	}
}
