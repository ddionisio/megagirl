using UnityEngine;
using System.Collections;

/// <summary>
/// Achievement notifier.  Inherite depending on when to notify the achievement system.
/// Call Notify if conditions have been met.
/// </summary>
public abstract class AchievementNotifier : MonoBehaviour {
    public int id;
    public int progress;
    public bool complete;

    private Achievement.Data mData;

    public Achievement.Data data { get { return mData; } }

    //interface

    protected void Notify() {
        Achievement a = Achievement.instance;

        if(complete)
            a.NotifyUpdate(mData, 0, true);
        else
            a.NotifyUpdate(mData, progress, false);
    }

    //internal

    protected virtual void Awake() {
        mData = Achievement.instance.GetDataById(id);
    }
}
