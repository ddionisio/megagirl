using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//generalized input handling, useful for porting to non-unity conventions (e.g. Ouya)
[AddComponentMenu("M8/Core/InputManager")]
public class InputManager : MonoBehaviour {
    public const int MaxPlayers = 8;
    public const int ActionInvalid = -1;

    public delegate void OnButton(Info data);

    public enum State {
        None,
        Pressed,
        Released
    }

    public enum Control {
        Button,
        Axis
    }

    public enum ButtonAxis {
        None,
        Plus,
        Minus,
        Both
    }

    public struct Info {
        public State state;
        public float axis;
        public int index;
    }

    public class Key {
        public int player = 0;

        public string input = ""; //for use with unity's input
        public KeyCode code = KeyCode.None; //unity
        public InputKeyMap map = InputKeyMap.None; //for external (like ouya!)

        public ButtonAxis axis = ButtonAxis.None; //for buttons as axis
        public int index = 0; //which index this key refers to

        private bool mDirty = false;

        public bool isValid {
            get { return !string.IsNullOrEmpty(input) || code != KeyCode.None || map != InputKeyMap.None; }
        }

        public void SetAsInput(string input) {
            ResetKeys();

            this.input = input;
        }

        public void SetAsKey(KeyCode aCode) {
            ResetKeys();
            code = aCode;
        }

        public void SetDirty(bool dirty) {
            mDirty = dirty;
        }

        public string GetKeyString() {
            if(!string.IsNullOrEmpty(input))
                return input;

            if(code != KeyCode.None) {
                if(code == KeyCode.Escape)
                    return "ESC";
                else {
                    string s = code.ToString();

                    int i = s.IndexOf("Joystick");
                    if(i != -1) {
                        int bInd = s.LastIndexOf('B');
                        if(bInd != -1) {
                            return s.Substring(bInd);
                        }
                    }

                    return s;
                }
            }

            if(map != InputKeyMap.None && map != InputKeyMap.NumKeys) {
                return map.ToString();
            }

            return "";
        }

        void _ApplyInfo(uint dataPak) {
            ushort s1 = M8.Util.GetHiWord(dataPak);

            axis = (ButtonAxis)M8.Util.GetHiByte(s1);
            index = M8.Util.GetLoByte(s1);
        }

        public void _SetAsKey(uint dataPak) {
            ResetKeys();

            _ApplyInfo(dataPak);

            code = (KeyCode)M8.Util.GetLoWord(dataPak);
        }

        public void _SetAsMap(uint dataPak) {
            ResetKeys();

            _ApplyInfo(dataPak);

            map = (InputKeyMap)M8.Util.GetLoWord(dataPak);
        }

        public void ResetKeys() {
            input = "";
            code = KeyCode.None;
            map = InputKeyMap.None;
        }

        public float GetAxisValue() {
            float ret = 0.0f;

            switch(axis) {
                case ButtonAxis.Plus:
                    ret = 1.0f;
                    break;
                case ButtonAxis.Minus:
                    ret = -1.0f;
                    break;
            }

            return ret;
        }

        public bool IsDirty() {
            return mDirty;
        }
    }

    public class Bind {
        public int action = 0;
        public Control control = InputManager.Control.Button;
        public bool axisInvert;

        public float deadZone = 0.1f;
        public bool forceRaw;

        public List<Key> keys = null;
    }

    public TextAsset config;

    protected class PlayerData {
        public Info info;

        public bool down;

        public Key[] keys;

        public OnButton callback;

        public PlayerData(List<Key> aKeys) {
            down = false;
            ApplyKeyList(aKeys);
        }

        public void ApplyKeyList(List<Key> aKeys) {
            keys = aKeys.ToArray();
        }
    }

    protected class BindData {
        public Control control;
        public float deadZone;
        public bool forceRaw;
        public bool invert;

        public PlayerData[] players;

        public BindData(Bind bind) {
            control = bind.control;
            deadZone = bind.deadZone;
            forceRaw = bind.forceRaw;
            invert = bind.axisInvert;

            //construct player data, put in the keys
            ApplyKeys(bind);
        }

        public void ApplyKeys(Bind bind) {
            int numPlayers = 0;

            List<Key>[] playerKeys = new List<Key>[MaxPlayers];

            foreach(Key key in bind.keys) {
                if(key.player + 1 > numPlayers)
                    numPlayers = key.player + 1;

                if(playerKeys[key.player] == null)
                    playerKeys[key.player] = new List<Key>();

                playerKeys[key.player].Add(key);
            }

            if(players == null)
                players = new PlayerData[numPlayers];

            for(int i = 0; i < numPlayers; i++) {
                if(playerKeys[i] != null) {
                    if(players[i] == null)
                        players[i] = new PlayerData(playerKeys[i]);
                    else
                        players[i].ApplyKeyList(playerKeys[i]);
                }
            }
        }
    }

    protected BindData[] mBinds;

    private const int buttonCallMax = 32;
    protected PlayerData[] mButtonCalls = new PlayerData[buttonCallMax];
    protected int mButtonCallsCount = 0;

    private struct ButtonCallSetData {
        public PlayerData pd;
        public OnButton cb;
        public bool add;
    }

    private ButtonCallSetData[] mButtonCallSetQueue = new ButtonCallSetData[buttonCallMax]; //prevent breaking enumeration during update when adding/removing
    private int mButtonCallSetQueueCount;

    //interfaces (available after awake)

    /// <summary>
    /// Call this to reload binds from config and prefs.  This is to cancel editing key binds.
    /// If deletePrefs = true, then remove custom binds from prefs.
    /// </summary>
    public void RevertBinds(bool deletePrefs) {
        fastJSON.JSON.Instance.Parameters.UseExtensions = false;
        List<Bind> keys = fastJSON.JSON.Instance.ToObject<List<Bind>>(config.text);

        foreach(Bind key in keys) {
            if(key != null && key.keys != null) {
                if(mBinds[key.action] != null) {
                    BindData bindDat = mBinds[key.action];
                    bindDat.ApplyKeys(key);
                }
            }
        }

        if(deletePrefs) {
            for(int act = 0; act < mBinds.Length; act++) {
                BindData bindDat = mBinds[act];
                if(bindDat != null) {
                    for(int player = 0; player < bindDat.players.Length; player++) {
                        PlayerData pd = bindDat.players[player];
                        if(pd != null) {
                            for(int index = 0; index < pd.keys.Length; index++) {
                                string usdKey = _BaseKey(act, player, index);
                                _DeletePlayerPrefs(usdKey);
                            }
                        }
                    }
                }
            }
        }
        else {
            LoadBinds();
        }
    }

    private void LoadBinds() {
        for(int act = 0; act < mBinds.Length; act++) {
            BindData bindDat = mBinds[act];
            if(bindDat != null) {
                for(int player = 0; player < bindDat.players.Length; player++) {
                    PlayerData pd = bindDat.players[player];
                    if(pd != null) {
                        for(int index = 0; index < pd.keys.Length; index++) {
                            string usdKey = _BaseKey(act, player, index);

                            if(PlayerPrefs.HasKey(usdKey + "_i")) {
                                if(pd.keys[index] == null)
                                    pd.keys[index] = new Key();

                                pd.keys[index].SetAsInput(PlayerPrefs.GetString(usdKey + "_i"));
                            }
                            else if(PlayerPrefs.HasKey(usdKey + "_k")) {
                                if(pd.keys[index] == null)
                                    pd.keys[index] = new Key();

                                pd.keys[index]._SetAsKey((uint)PlayerPrefs.GetInt(usdKey + "_k"));
                            }
                            else if(PlayerPrefs.HasKey(usdKey + "_m")) {
                                if(pd.keys[index] == null)
                                    pd.keys[index] = new Key();

                                pd.keys[index]._SetAsMap((uint)PlayerPrefs.GetInt(usdKey + "_m"));
                            }
                            else if(PlayerPrefs.HasKey(usdKey + "_d"))
                                pd.keys[index].ResetKeys();
                        }
                    }
                }
            }
        }
    }

    string _BaseKey(int action, int player, int key) {
        return string.Format("bind_{0}_{1}_{2}", action, player, key);
    }

    void _DeletePlayerPrefs(string baseKey) {
        PlayerPrefs.DeleteKey(baseKey + "_i");
        PlayerPrefs.DeleteKey(baseKey + "_k");
        PlayerPrefs.DeleteKey(baseKey + "_m");
        PlayerPrefs.DeleteKey(baseKey + "_d");
    }

    /// <summary>
    /// Call this once you are done modifying key binds
    /// </summary>
    public void SaveBinds() {
        for(int act = 0; act < mBinds.Length; act++) {
            BindData bindDat = mBinds[act];
            if(bindDat != null) {
                for(int player = 0; player < bindDat.players.Length; player++) {
                    PlayerData pd = bindDat.players[player];
                    if(pd != null) {
                        for(int index = 0; index < pd.keys.Length; index++) {
                            string usdKey = _BaseKey(act, player, index);

                            Key key = pd.keys[index];
                            if(key.isValid) {
                                if(key.IsDirty()) {
                                    //for previous bind if type is changed
                                    _DeletePlayerPrefs(usdKey);

                                    if(!string.IsNullOrEmpty(key.input)) {
                                        PlayerPrefs.SetString(usdKey + "_k", key.input);
                                    }
                                    else {
                                        //pack data
                                        ushort code = 0;
                                        string postfix;

                                        if(key.code != KeyCode.None) {
                                            code = (ushort)key.code;
                                            postfix = "_k";
                                        }
                                        else if(key.map != InputKeyMap.None) {
                                            code = (ushort)key.map;
                                            postfix = "_m";
                                        }
                                        else
                                            postfix = null;

                                        if(postfix != null) {
                                            int val = (int)M8.Util.MakeLong(M8.Util.MakeWord((byte)key.axis, (byte)key.index), code);

                                            PlayerPrefs.SetInt(usdKey + postfix, val);
                                        }
                                    }

                                    key.SetDirty(false);
                                }
                            }
                            else {
                                _DeletePlayerPrefs(usdKey);
                                PlayerPrefs.SetString(usdKey + "_d", "-");
                            }
                        }
                    }
                }
            }
        }
    }

    public void UnBindKey(int player, int action, int index) {
        mBinds[action].players[player].keys[index].ResetKeys();
    }

    public bool CheckBindKey(int player, int action, int index) {
        return mBinds[action] != null && mBinds[action].players[player] != null && mBinds[action].players[player].keys[index].isValid;
    }

    public Key GetBindKey(int player, int action, int index) {
        return mBinds[action].players[player].keys[index];
    }

    public bool CheckBind(int action) {
        return mBinds[action] != null;
    }

    public Control GetControlType(int action) {
        return mBinds[action].control;
    }

    public float GetAxis(int player, int action) {
        BindData bindData = mBinds[action];
        PlayerData pd = bindData.players[player];
        Key[] keys = pd.keys;

        pd.info.axis = 0.0f;

        foreach(Key key in keys) {
            if(key != null) {
                float axis = ProcessAxis(key, bindData.deadZone, bindData.forceRaw);
                if(axis != 0.0f) {
                    pd.info.axis = bindData.invert ? -axis : axis;
                    break;
                }
            }
        }

        return pd.info.axis;
    }

    public State GetState(int player, int action) {
        return mBinds[action].players[player].info.state;
    }

    public bool IsDown(int player, int action) {
        if(action == ActionInvalid)
            return false;

        Key[] keys = mBinds[action].players[player].keys;

        foreach(Key key in keys) {
            if(key != null && ProcessButtonDown(key)) {
                return true;
            }
        }

        return false;
    }

    public int GetIndex(int player, int action) {
        return mBinds[action].players[player].info.index;
    }

    public void AddButtonCall(int player, int action, OnButton callback) {
        if(action < mBinds.Length && player < mBinds[action].players.Length) {
            PlayerData pd = mBinds[action].players[player];

            mButtonCallSetQueue[mButtonCallSetQueueCount] = new ButtonCallSetData() { pd = pd, cb = callback, add = true };
            mButtonCallSetQueueCount++;
        }
    }

    public void RemoveButtonCall(int player, int action, OnButton callback) {
        if(action < mBinds.Length && player < mBinds[action].players.Length) {
            PlayerData pd = mBinds[action].players[player];

            mButtonCallSetQueue[mButtonCallSetQueueCount] = new ButtonCallSetData() { pd = pd, cb = callback, add = false };
            mButtonCallSetQueueCount++;
        }
    }

    public void ClearButtonCall(int action) {
        foreach(PlayerData pd in mBinds[action].players) {
            pd.callback = null;

            mButtonCallSetQueue[mButtonCallSetQueueCount] = new ButtonCallSetData() { pd = pd, cb = null, add = false };
            mButtonCallSetQueueCount++;
        }
    }

    public void ClearAllButtonCalls() {
        foreach(BindData bd in mBinds) {
            if(bd != null && bd.players != null) {
                foreach(PlayerData pd in bd.players) {
                    pd.callback = null;
                }
            }
        }

        mButtonCallsCount = 0;

        mButtonCallSetQueueCount = 0;
    }

    //implements

    protected virtual float ProcessAxis(Key key, float deadZone, bool forceRaw) {
        if(key.input.Length > 0) {
            if(Time.timeScale == 0.0f || forceRaw) {
                float val = Input.GetAxisRaw(key.input);
                return Mathf.Abs(val) > deadZone ? val : 0.0f;
            }
            else {
                return Input.GetAxis(key.input);
            }
        }
        else if(key.code != KeyCode.None) {
            if(Input.GetKey(key.code)) {
                return key.GetAxisValue();
            }
        }

        return 0.0f;
    }

    protected virtual bool ProcessButtonDown(Key key) {
        return
            key.input.Length > 0 ? Input.GetButton(key.input) :
            key.code != KeyCode.None ? Input.GetKey(key.code) :
            false;
    }

    //internal

    protected virtual void OnDestroy() {
        ClearAllButtonCalls();
    }

    protected virtual void Awake() {
        if(config != null) {
            Dictionary<int, BindData> binds = new Dictionary<int, BindData>();

            fastJSON.JSON.Instance.Parameters.UseExtensions = false;
            List<Bind> keys = fastJSON.JSON.Instance.ToObject<List<Bind>>(config.text);

            int highestActionInd = 0;

            foreach(Bind key in keys) {
                if(key != null && key.keys != null) {
                    binds.Add(key.action, new BindData(key));

                    if(key.action > highestActionInd)
                        highestActionInd = key.action;
                }
            }

            mBinds = new BindData[highestActionInd + 1];
            foreach(KeyValuePair<int, BindData> pair in binds) {
                mBinds[pair.Key] = pair.Value;
            }

            //load user config binds
            LoadBinds();
        }
        else {
            mBinds = new BindData[0];
        }

        for(int i = 0; i < InputAction._count; i++) {
            GameLocalize.RegisterParam("input_"+i, OnTextParam);
        }
    }

    string OnTextParam(string key) {
        //NOTE: assumes player 0
        int delimitInd = key.LastIndexOf('_');
        int action = int.Parse(key.Substring(delimitInd+1));

        PlayerData pd = mBinds[action].players[0];

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        for(int i = 0; i < pd.keys.Length; i++) {
            string keyString = pd.keys[i].GetKeyString();
            if(!string.IsNullOrEmpty(keyString)) {
                sb.Append(keyString);

                if(pd.keys.Length > 1 && i < pd.keys.Length - 1) {
                    sb.Append(", ");
                }
            }
        }

        return sb.ToString();
    }

    protected virtual void Update() {
        for(int i = 0; i < mButtonCallsCount; i++) {
            PlayerData pd = mButtonCalls[i];

            pd.info.state = State.None;

            Key keyDown = null;

            for(int k = 0; k < pd.keys.Length; k++) {
                Key key = pd.keys[k];
                if(ProcessButtonDown(key)) {
                    keyDown = key;
                    break;
                }
            }

            if(keyDown != null) {
                if(!pd.down) {
                    pd.down = true;

                    pd.info.axis = keyDown.GetAxisValue();
                    pd.info.state = State.Pressed;
                    pd.info.index = keyDown.index;

                    pd.callback(pd.info);
                }
            }
            else {
                if(pd.down) {
                    pd.down = false;

                    pd.info.axis = 0.0f;
                    pd.info.state = State.Released;

                    pd.callback(pd.info);
                }
            }
        }

        //add or remove button calls
        for(int i = 0; i < mButtonCallSetQueueCount; i++) {
            ButtonCallSetData dat = mButtonCallSetQueue[i];

            if(dat.add) {
                if(dat.cb != null) {
                    if(dat.pd.callback != dat.cb) {
                        dat.pd.callback += dat.cb;
                    }

                    int ind = System.Array.IndexOf(mButtonCalls, dat.pd, 0, mButtonCallsCount);
                    if(ind == -1) {
                        mButtonCalls[mButtonCallsCount] = dat.pd;
                        mButtonCallsCount++;
                    }
                }
            }
            else {
                if(dat.cb != null)
                    dat.pd.callback -= dat.cb;
                else
                    dat.pd.callback = null;

                //no more callbacks, don't need to poll this anymore
                if(dat.pd.callback == null) {
                    if(mButtonCallsCount > 1) {
                        int ind = System.Array.IndexOf(mButtonCalls, dat.pd, 0, mButtonCallsCount);
                        mButtonCalls[ind] = mButtonCalls[mButtonCallsCount - 1];

                        mButtonCallsCount--;
                    }
                    else
                        mButtonCallsCount = 0;
                }
            }
        }

        mButtonCallSetQueueCount = 0;
    }
}
