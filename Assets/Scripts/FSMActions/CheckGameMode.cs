using UnityEngine;
using System.Collections;

using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Game")]
public class CheckGameMode : FsmStateAction {
    public SlotInfo.GameMode mode;

    public FsmEvent equalEvent;
    public FsmEvent notEqualEvent;
    
    // Code that runs on entering the state.
    public override void OnEnter() {
        if(SlotInfo.gameMode == mode) {
            if(!FsmEvent.IsNullOrEmpty(equalEvent))
                Fsm.Event(equalEvent);
        }
        else {
            if(!FsmEvent.IsNullOrEmpty(notEqualEvent))
                Fsm.Event(notEqualEvent);
        }

        Finish();
    }
}
