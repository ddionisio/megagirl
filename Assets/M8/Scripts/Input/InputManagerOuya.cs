using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("M8/Core/InputManagerOuya")]
public class InputManagerOuya : InputManager {
#if OUYA
    OuyaSDK.OuyaPlayer GetPlayer(Key key) {
        return (OuyaSDK.OuyaPlayer)(key.player+1);
    }

    protected override bool ProcessButtonDown(Key key) {
        if(key.map == InputKeyMap.None)
            return base.ProcessButtonDown(key);

        return OuyaExampleCommon.GetButton((int)key.map, GetPlayer(key));
    }

    protected override float ProcessAxis(Key key, float deadZone, bool forceRaw) {
        if(key.map == InputKeyMap.None)
            return base.ProcessAxis(key, deadZone, forceRaw);

        OuyaSDK.OuyaPlayer player = GetPlayer(key);

        switch(key.axis) {
            case ButtonAxis.Minus:
                return OuyaExampleCommon.GetButton((OuyaSDK.KeyEnum)key.map, player) ? -1.0f : 0.0f;
                break;
            case ButtonAxis.Plus:
                return OuyaExampleCommon.GetButton((OuyaSDK.KeyEnum)key.map, player) ? 1.0f : 0.0f;
                break;
            case ButtonAxis.Both:
                float val = OuyaExampleCommon.GetAxis((OuyaSDK.KeyEnum)key.map, player);
                return Mathf.Abs(val) > deadZone ? val : 0.0f;
        }

        return 0.0f;
    }

    //internal

    protected override void OnDestroy() {
        base.OnDestroy();
    }

    protected override void Awake() {
        base.Awake();
    }
#endif
}
