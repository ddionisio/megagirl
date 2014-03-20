using UnityEngine;
using System.Collections;

public class ModalVictoryTrial : UIController {

    public UILabel timeLabel;
    public UILabel timeBestLabel;
    public GameObject newRecordGO;

    public UIEventListener click;

    protected override void OnActive(bool active) {
        if(active) {
            UICamera.selectedObject = click.gameObject;
            click.onClick = OnClick;
        }
        else {
            click.onClick = null;
        }
    }
    
    protected override void OnOpen() {
        //save data if best
        string level = LevelController.levelLoaded;
        float t = LevelController.timeTrialSaved;
        float tBest;

        bool isBest;

        if(TimeTrial.Exists(level)) {
            float lastT = TimeTrial.Load(level);
            if(t < lastT) {
                tBest = t;
                isBest = true;
                TimeTrial.Save(level, t);
            }
            else {
                tBest = lastT;
                isBest = false;
            }
        }
        else {
            tBest = t;
            isBest = true;
            TimeTrial.Save(level, t);
        }

        timeLabel.text = LevelController.LevelTimeFormat(t);
        timeBestLabel.text = LevelController.LevelTimeFormat(tBest);

        //post
        TimeTrial.Post(level, t);

        NGUILayoutBase.RefreshNow(transform);
    }
    
    protected override void OnClose() {
    }
    
    void OnClick(GameObject go) {
        Main.instance.sceneManager.LoadScene(Scenes.main);
    }
}
