﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Achievement : MonoBehaviour {
    public enum Status {
        Processing,
        Complete
    }

    public interface IService {
        /// <summary>
        /// Return true if we are ready to process new data.  Should return false if processing a data or is still initializing.
        /// </summary>
        bool AchievementIsReady();

        /// <summary>
        /// Return the current data being processed, return null if no data to process.
        /// </summary>
        Data AchievementCurrentData();

        /// <summary>
        /// Get the current status of data being processed.
        /// </summary>
        Status AchievementCurrentStatus();

        /// <summary>
        /// Check to see if given achievement has already been completed.
        /// </summary>
        bool AchievementIsComplete(Data data);

        /// <summary>
        /// Called when processing new data.
        /// </summary>
        void AchievementProcessData(Data data, int progress, bool complete);
    }

    [System.Serializable]
    public class Data {
        public int id;
        public string title;
        public string description;
        public string imageRef;
        public int points;
        public int progress;
        public bool hidden;
    }

    private struct DataProcess {
        public Data data;
        public bool complete;
        public int progress;
    }

    public delegate void OnPopUp(Data data, int progress, bool complete);

    public TextAsset config;

    public event OnPopUp popupCallback; //called once all services has completed processing the data

    private static Achievement mInstance;

    private List<IService> mServices = new List<IService>(5);
    private Queue<DataProcess> mProcessUpdates;
    private Data[] mData;

    private Queue<DataProcess> mProcessPopUps;

    public static Achievement instance { get { return mInstance; } }

    public Data[] data { get { return mData; } }

    public Data GetDataById(int id) {
        for(int i = 0; i < mData.Length; i++) {
            if(mData[i].id == id)
                return mData[i];
        }
        return null;
    }

    public Data GetDataByTitle(string title) {
        for(int i = 0; i < mData.Length; i++) {
            if(mData[i].title == title)
                return mData[i];
        }
        return null;
    }

    /// <summary>
    /// A given achievement item has been updated.
    /// </summary>
    public void NotifyUpdate(Data aData, int aProgress, bool aComplete) {
        mProcessUpdates.Enqueue(new DataProcess() { data=aData, progress=aProgress, complete=aComplete});
    }

    public void RegisterService(IService service) {
        mServices.Add(service);
    }

    public void UnregisterService(IService service) {
        mServices.Remove(service);
    }

    void OnDestroy() {
        if(mInstance == this) {
            popupCallback = null;

            mInstance = null;
        }
    }

    void Awake() {
        if(mInstance == null) {
            fastJSON.JSON.Instance.Parameters.UseExtensions = false;
            mData = (fastJSON.JSON.Instance.ToObject<List<Data>>(config.text)).ToArray();

            mProcessUpdates = new Queue<Achievement.DataProcess>(mData.Length);
            mProcessPopUps = new Queue<DataProcess>(mData.Length);

            mInstance = this;
        }
    }

    void Update() {
        if(mProcessUpdates.Count > 0) {
            DataProcess dat = mProcessUpdates.Peek();

            int numProcessComplete = 0;

            for(int i = 0; i < mServices.Count; i++) {
                IService service = mServices[i];

                Data serviceData = service.AchievementCurrentData();
                if(serviceData == dat.data) {
                    if(service.AchievementCurrentStatus() == Status.Complete) {
                        numProcessComplete++;
                    }
                }
                else if(service.AchievementIsReady()) {
                    //if it's already completed, ignore completely and pop process
                    if(service.AchievementIsComplete(dat.data)) {
                        mProcessUpdates.Dequeue();
                        return;
                    }

                    service.AchievementProcessData(dat.data, dat.progress, dat.complete);
                }
            }

            //all done?
            if(numProcessComplete == mServices.Count) {
                mProcessUpdates.Dequeue();

                if(popupCallback != null)
                    popupCallback(dat.data, dat.progress, dat.complete);
                else
                    mProcessPopUps.Enqueue(dat);
            }
        }

        //notify pop-up
        if(mProcessPopUps.Count > 0 && popupCallback != null) {
            while(mProcessPopUps.Count > 0) {
                DataProcess dat = mProcessPopUps.Dequeue();
                popupCallback(dat.data, dat.progress, dat.complete);
            }
        }
    }
}