using UnityEngine;
using System.Collections;

//custom key mapping
public enum InputKeyMap : int {
	None = -1,

#if OUYA
    BUTTON_O_PS3 = 0,
    BUTTON_U_PS3 = 2,
    BUTTON_Y_PS3 = 3,
    BUTTON_A_PS3 = 1,

    BUTTON_LB_PS3 = 4,
    BUTTON_RB_PS3 = 5,

    BUTTON_START_PS3 = 10,

#endif

    NumKeys
}