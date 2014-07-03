using UnityEngine;
using System.Collections;

public class ModalOptions : UIController {
    public UIEventListener input;
    public UIEventListener graphics;
    public UIEventListener music;
    public UIEventListener sound;
    public UIEventListener back;
    public UIEventListener exitToMainMenu;

    public UILabel musicLabel;
    public UILabel soundLabel;

    public SoundPlayer soundChangeSfx;

    private UISlider mMusicSlider;
    private UISlider mSoundSlider;

    void RefreshInfo() {
        UserSettings s = Main.instance.userSettings;
        mMusicSlider.value = s.musicVolume;
        mSoundSlider.value = s.soundVolume;
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

            //music.onClick = OnMusicClick;
            //sound.onClick = OnSoundClick;

            back.onClick = OnBack;

            if(exitToMainMenu)
                exitToMainMenu.onClick = OnExitToMainMenuClick;

            RefreshInfo();

            EventDelegate.Set(mMusicSlider.onChange, OnMusicValueChange);
            EventDelegate.Set(mSoundSlider.onChange, OnSoundValueChange);
        }
        else {
            if(input)
                input.onClick = null;

            if(graphics)
                graphics.onClick = null;

            //music.onClick = null;
            //sound.onClick = null;

            back.onClick = null;

            if(exitToMainMenu)
                exitToMainMenu.onClick = null;

            EventDelegate.Remove(mMusicSlider.onChange, OnMusicValueChange);
            EventDelegate.Remove(mSoundSlider.onChange, OnSoundValueChange);
        }
    }

    protected override void OnOpen() {
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
        UserSettings s = Main.instance.userSettings;
        s.soundVolume = s.soundVolume > 0.0f ? 0.0f : 1.0f;
        s.Save();

        mSoundSlider.value = s.soundVolume;

        if(s.soundVolume > 0.0f)
            soundChangeSfx.Play();
    }

    void OnMusicClick(GameObject go) {
        UserSettings s = Main.instance.userSettings;
        s.musicVolume = s.musicVolume > 0.0f ? 0.0f : 1.0f;
        s.Save();

        mMusicSlider.value = s.musicVolume;
    }

    void OnMusicValueChange() {
        UserSettings s = Main.instance.userSettings;
        s.musicVolume = mMusicSlider.value;
        s.Save();
    }

    void OnSoundValueChange() {
        UserSettings s = Main.instance.userSettings;
        s.soundVolume = mSoundSlider.value;
        s.Save();

        if(s.soundVolume > 0.0f)
            soundChangeSfx.Play();
    }

    void OnExitToMainMenuClick(GameObject go) {
        UIModalConfirm.Open(GameLocalize.GetText("exit_to_main_title"), GameLocalize.GetText("exit_confirm_desc"),
                            delegate(bool yes) {
            if(yes)
                Main.instance.sceneManager.LoadScene(Scenes.main);
                           });

    }

    void OnBack(GameObject go) {
        UIModalManager.instance.ModalCloseTop();
    }

    void Awake() {
#if OUYA
        input.gameObject.SetActive(false);
        input = null;

        graphics.gameObject.SetActive(false);
        graphics = null;
        
        UIButtonKeys musicBtnKeys = music.GetComponent<UIButtonKeys>();
        UIButtonKeys backBtnKeys = back.GetComponent<UIButtonKeys>();

        musicBtnKeys.selectOnUp = backBtnKeys;
        backBtnKeys.selectOnDown = musicBtnKeys;

        NGUILayoutBase.RefreshNow(transform);
#endif

        mMusicSlider = music.GetComponent<UISlider>();
        mSoundSlider = sound.GetComponent<UISlider>();
    }
}
