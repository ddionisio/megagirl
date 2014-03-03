using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Game")]
public class LevelTimeSave : FsmStateAction {
    public enum Type {
        Save,
        Progress,
    }

    public Type type = Type.Save;
    
    // Code that runs on entering the state.
    public override void OnEnter() {
        switch(type) {
            case Type.Progress:
                LevelController.instance.TimeProgressSave();
                break;

            case Type.Save:
                LevelController.instance.TimeSave();
                break;
        }

        Finish();
    }
    
    
}
