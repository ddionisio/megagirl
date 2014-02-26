using UnityEngine;
using System.Collections;

public class PlayerJumpOnDamage : MonoBehaviour {
    public float minVelY = 4.0f;
    public float gravityNormal = 18.0f; //the gravity the velocity is based on

    private SoundPlayer mSound;

	// Use this for initialization
	void Awake() {
	    DamageTrigger dt = GetComponent<DamageTrigger>();
        dt.damageCallback += OnDamage;

        mSound = GetComponent<SoundPlayer>();
	}

    void OnDamage(DamageTrigger trigger, GameObject victim) {
        Player player = Player.instance;

        /*player.controller.jumpCounterCurrent = 0;
        player.controller.Jump(false);
        player.controller.Jump(true);*/

        float criteriaY;
        if(gravityNormal > 0.0f) {
            criteriaY = minVelY*(Mathf.Abs(player.controller.gravityController.gravity)/gravityNormal);
        }
        else
            criteriaY = minVelY;

        Vector3 lv = player.controller.localVelocity;
        if(lv.y < minVelY) {
            lv.y = minVelY;
            player.controller.localVelocity = lv;
        }

        if(mSound)
            mSound.Play();
    }
}
