using UnityEngine;
using System.Collections;

public class ModalFinalStage : UIController {
    [System.Serializable]
    public class Item {
        public NGUISpriteAnimation dotAnim;
        public NGUIFillAnim pathAnim;
        public string level;
    }

    public Item[] stages;

    //anims
    public GameObject flashGO;
    public float flashWaitDelay = 2.0f;
    public float flashDelay = 0.3f;
    public int flashCount = 2;

    public float startLevelDelay = 2.0f;

    private bool mInputLock;
    private int mCurStage;

    protected override void OnActive(bool active) {
        InputManager input = Main.instance.input;

        if(active) {
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
    
    protected override void OnOpen() {
        //determine which stage
        mCurStage = 0;

        for(int i = 0; i < stages.Length - 1; i++) {
            Item stage = stages[i];
            if(LevelController.isLevelComplete(stage.level)) {
                stage.pathAnim.Fill();
                mCurStage++;
            }
            else
                break;
        }

        StartCoroutine(DoPlay());
    }
    
    protected override void OnClose() {
        StopAllCoroutines();
    }

    void OnInput(InputManager.Info dat) {
        if(!mInputLock) {
            if(dat.state == InputManager.State.Pressed) {
                UIModalManager.instance.ModalCloseAll();

                //go to stage
                Main.instance.sceneManager.LoadScene(stages[mCurStage].level);
            }
        }
    }

    IEnumerator DoPlay() {
        //wait until we are done flashing
        mInputLock = true;

        stages[mCurStage].dotAnim.Play();

        yield return new WaitForSeconds(flashWaitDelay);

        WaitForSeconds flashWait = new WaitForSeconds(flashDelay);

        for(int i = 0; i < flashCount; i++) {
            flashGO.SetActive(true);
            yield return flashWait;
            flashGO.SetActive(false);
            yield return flashWait;
        }

        mInputLock = false;

        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        stages[mCurStage].pathAnim.Play();

        while(stages[mCurStage].pathAnim.isPlaying) {
            yield return wait;
        }

        yield return new WaitForSeconds(startLevelDelay);

        mInputLock = true;

        //go to stage
        Main.instance.sceneManager.LoadScene(stages[mCurStage].level);
    }
}
