using UnityEngine;
using System.Collections;

public class ModalOptions : UIController {
    public UIEventListener input;
    public UIEventListener graphics;
    public UIEventListener music;
    public UIEventListener sound;
    public UIEventListener exitToMainMenu;

    public UILabel musicLabel;
    public UILabel soundLabel;

    void RefreshLabels() {
        musicLabel.text = string.Format("MUSIC: {0}", Main.instance.userSettings.musicVolume > 0.0f ? "ON" : "OFF");
        soundLabel.text = string.Format("SOUND: {0}", Main.instance.userSettings.soundVolume > 0.0f ? "ON" : "OFF");
    }

    protected override void OnActive(bool active) {
        if(active) {
            if(input) {
                input.onClick = OnInputClick;
                UICamera.selectedObject = input.gameObject;
            }
            else {
                UICamera.selectedObject = music.gameObject;
            }

            if(graphics)
                graphics.onClick = OnGraphicsClick;

            music.onClick = OnMusicClick;
            sound.onClick = OnSoundClick;

            if(exitToMainMenu)
                exitToMainMenu.onClick = OnExitToMainMenuClick;
        }
        else {
            if(input)
                input.onClick = null;

            if(graphics)
                graphics.onClick = null;

            music.onClick = null;
            sound.onClick = null;

            if(exitToMainMenu)
                exitToMainMenu.onClick = null;
        }
    }

    protected override void OnOpen() {
        RefreshLabels();
        NGUILayoutBase.RefreshNow(transform);
    }

    protected override void OnClose() {
    }

    void OnInputClick(GameObject go) {
        UIModalManager.instance.ModalOpen("inputBind");
    }

    void OnGraphicsClick(GameObject go) {
        UIModalManager.instance.ModalOpen("graphics");
    }

    void OnSoundClick(GameObject go) {
        Main.instance.userSettings.soundVolume = Main.instance.userSettings.soundVolume > 0.0f ? 0.0f : 1.0f;

        Main.instance.userSettings.Save();

        RefreshLabels();
    }

    void OnMusicClick(GameObject go) {
        Main.instance.userSettings.musicVolume = Main.instance.userSettings.musicVolume > 0.0f ? 0.0f : 1.0f;

        Main.instance.userSettings.Save();

        RefreshLabels();
    }

    void OnExitToMainMenuClick(GameObject go) {
        UIModalConfirm.Open(GameLocalize.GetText("exit_to_main_title"), GameLocalize.GetText("exit_confirm_desc"),
                            delegate(bool yes) {
            if(yes)
                Main.instance.sceneManager.LoadScene(Scenes.main);
                           });

    }

    void Awake() {
#if OUYA
        input.gameObject.SetActive(false);
        input = null;

        graphics.gameObject.SetActive(false);
        graphics = null;
        
        UIButtonKeys musicBtnKeys = music.GetComponent<UIButtonKeys>();
        UIButtonKeys lastItmBtnKeys = exitToMainMenu ? exitToMainMenu.GetComponent<UIButtonKeys>() : sound.GetComponent<UIButtonKeys>();
        
        musicBtnKeys.selectOnUp = lastItmBtnKeys;
        lastItmBtnKeys.selectOnDown = musicBtnKeys;

        NGUILayoutBase.RefreshNow(transform);
#endif
    }
}
