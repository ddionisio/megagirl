using UnityEngine;
using System.Collections;

public class LevelSelectCharacterControl : MonoBehaviour {
    private static LevelSelectCharacterControl mInstance;

    [System.NonSerialized]
    public string toScene;

    public static LevelSelectCharacterControl instance { get { return mInstance; } }

    public void StartGame() {
        Main.instance.sceneManager.LoadScene(toScene);
    }

    public void SetAnimWatch(AnimatorData animDat) {
        animDat.takeCompleteCallback += OnAnimationEnd;
    }

    public void SetInput(bool aSet) {
        InputManager input = Main.instance ? Main.instance.input : null;
        if(input) {
            if(aSet) {
                input.AddButtonCall(0, InputAction.Fire, OnInput);
                input.AddButtonCall(0, InputAction.Jump, OnInput);
                input.AddButtonCall(0, InputAction.MenuAccept, OnInput);
                input.AddButtonCall(0, InputAction.MenuEscape, OnInput);
            }
            else {
                input.RemoveButtonCall(0, InputAction.Fire, OnInput);
                input.RemoveButtonCall(0, InputAction.Jump, OnInput);
                input.RemoveButtonCall(0, InputAction.MenuAccept, OnInput);
                input.RemoveButtonCall(0, InputAction.MenuEscape, OnInput);
            }
        }
    }

    void OnDestroy() {
        mInstance = null;

        SetInput(false);
    }

    void Awake() {
        mInstance = this;
    }

    void OnInput(InputManager.Info dat) {
        if(dat.state == InputManager.State.Pressed) {
            StartGame();
        }
    }

    void OnAnimationEnd(AnimatorData dat, AMTake take) {
        StartGame();
    }
}
