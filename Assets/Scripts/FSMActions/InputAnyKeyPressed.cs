using UnityEngine;
using HutongGames.PlayMaker;
using M8.PlayMaker;

[ActionCategory("Game")]
public class InputAnyKeyPressed : FsmStateAction {
    public FsmEvent toEvent;

    // Code that runs on entering the state.
    public override void OnEnter() {
        SetInput(true);
    }
    
    public override void OnExit() {
        SetInput(false);
    }

    void OnInput(InputManager.Info dat) {
        if(dat.state == InputManager.State.Pressed) {
            SetInput(false);

            if(!FsmEvent.IsNullOrEmpty(toEvent))
                Fsm.Event(toEvent);

            Finish();
        }
    }

    void SetInput(bool yes) {
        InputManager input = Main.instance ? Main.instance.input : null;
        if(input) {
            if(yes) {
                for(int i = 0; i < InputAction._count; i++) {
                    input.AddButtonCall(0, i, OnInput);
                }
            }
            else {
                for(int i = 0; i < InputAction._count; i++) {
                    input.RemoveButtonCall(0, i, OnInput);
                }
            }
        }
    }
}
