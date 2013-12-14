using UnityEngine;
using System.Collections;

public class NPCWander : Enemy {
    public float minX;
    public float maxX;

    public float waitDelay = 1.0f;

    public bool hop = true;

    public GameObject[] miscVisuals;
    public GameObject trigger;

    private const string ActionRoutine = "DoStuff";

    private float mMinX;
    private float mMaxX;

    private float mCurDestX;

	protected override void StateChanged() {
        switch((EntityState)prevState) {
            case EntityState.Normal:
                StopCoroutine(ActionRoutine);

                if(trigger)
                    trigger.SetActive(false);

                if(miscVisuals != null && miscVisuals.Length > 0) {
                    foreach(GameObject go in miscVisuals)
                        go.SetActive(false);
                }
                break;
        }

        base.StateChanged();

        switch((EntityState)state) {
            case EntityState.Normal:
                if(minX != 0.0f || maxX != 0.0f)
                    StartCoroutine(ActionRoutine);

                if(trigger)
                    trigger.SetActive(true);
                break;
        }
    }

    protected override void ActivatorWakeUp() {
        base.ActivatorWakeUp();

        switch((EntityState)state) {
            case EntityState.Normal:
                if((minX != 0.0f || maxX != 0.0f))
                    StartCoroutine(ActionRoutine);
                break;
        }
    }

    protected override void ActivatorSleep() {
        base.ActivatorSleep();

        switch((EntityState)state) {
            case EntityState.Normal:
                StopCoroutine(ActionRoutine);
                break;
        }
    }

    protected override void Awake() {
        base.Awake();

        mMinX = transform.position.x + minX;
        mMaxX = transform.position.x + maxX;
    }

    void FixedUpdate() {
        switch((EntityState)state) {
            case EntityState.Normal:
                if(hop) {
                    if(bodyCtrl.isGrounded && !bodyCtrl.isJump) {
                        Jump(0);
                        Jump(1.0f);
                    }
                }
                break;
        }
    }

    IEnumerator DoStuff() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        WaitForSeconds waitPause = new WaitForSeconds(waitDelay);

        while((EntityState)state == EntityState.Normal) {
            mCurDestX = Mathf.Lerp(mMinX, mMaxX, ((float)Random.Range(0, 5))/4.0f);

            float dx = 0.0f;
            do {
                dx = mCurDestX - transform.position.x;
                bodyCtrl.moveSide = Mathf.Sign(dx);
                yield return wait;
            } while(Mathf.Abs(dx) > 0.1f);

            bodyCtrl.moveSide = 0.0f;
            yield return waitPause;
        }
    }

    protected override void OnDrawGizmosSelected() {
        base.OnDrawGizmosSelected();

        Color clr = Color.green *0.5f;

        Gizmos.color = clr;

        Vector3 pos = transform.position;
        Vector3 min = pos; min.x += minX;
        Vector3 max = pos; max.x += maxX;

        Gizmos.DrawLine(min, max);
    }
}
