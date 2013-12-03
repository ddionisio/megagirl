using UnityEngine;
using System.Collections;

public class EntityControllerCurvePath : MonoBehaviour {
    public enum Face {
        Player,
        Left,
        Right
    }

    public enum Axis {
        X,
        Y
    }

    public tk2dBaseSprite sprite;
    public Face face = Face.Player;
    public Axis majorAxis = Axis.X;
    public float speed = 1.0f; //speed of major axis
    public float delay = 1.0f; //time it takes to get to 0.0 to 1.0
    public float curveScale = 1.0f; //the scale of the curve by meter
    public AnimationCurve curve;

    public float startDelay;

    private EntityBase mEnt;
    private bool mActive;
    private float mCurTime;
    private Vector3 mOrigin;

    private TimeWarp mTimeWarp;

    void Awake() {
        mEnt = GetComponent<EntityBase>();
        mEnt.spawnCallback += OnEntitySpawn;
        mEnt.setStateCallback += OnEntityState;

        mTimeWarp = GetComponent<TimeWarp>();
    }

    // Update is called once per frame
    void FixedUpdate() {
        if(mActive) {
            Vector3 pos = rigidbody.position;

            float dir = sprite.FlipX ? -1.0f : 1.0f;

            float dt = mTimeWarp ? Time.fixedDeltaTime * mTimeWarp.scale : Time.fixedDeltaTime;

            mCurTime += dir * dt;

            float t = mCurTime / delay;

            float delta = dir * speed * dt;
            float val = curve.Evaluate(t) * curveScale;

            Vector3 deltaPos = Vector3.zero;

            switch(majorAxis) {
                case Axis.X:
                    deltaPos.x = delta;
                    deltaPos.y = (mOrigin.y + val) - pos.y;
                    break;

                case Axis.Y:
                    deltaPos.x = (mOrigin.x + val) - pos.x;
                    deltaPos.y = delta;
                    break;
            }

            if(rigidbody) {
                rigidbody.MovePosition(rigidbody.position + deltaPos);
            }
        }
    }

    void OnEntitySpawn(EntityBase ent) {
        mOrigin = transform.position;
    }

    void OnEntityState(EntityBase ent) {
        switch((EntityState)ent.state) {
            case EntityState.Normal:
                if(startDelay > 0)
                    Invoke("DoActive", startDelay);
                else
                    DoActive();

                switch(face) {
                    case Face.Left:
                        sprite.FlipX = true;
                        break;

                    case Face.Right:
                        sprite.FlipX = false;
                        break;

                    case Face.Player:
                        Vector3 p = transform.position;
                        Vector3 playerP = Player.instance.transform.position;

                        switch(majorAxis) {
                            case Axis.X:
                                sprite.FlipX = Mathf.Sign(playerP.x - p.x) < 0.0f;
                                break;

                            case Axis.Y:
                                sprite.FlipX = Mathf.Sign(playerP.y - p.y) < 0.0f;
                                break;
                        }
                        break;
                }
                break;

            case EntityState.RespawnWait:
            case EntityState.Invalid:
            case EntityState.Dead:
                mCurTime = 0;
                mActive = false;
                CancelInvoke();
                break;
        }

    }

    void DoActive() {
        mActive = true;
    }

    void OnDrawGizmosSelected() {
        if(curve != null) {
            Gizmos.color = new Color(0, 0, 1, 0.3f);
            Vector3 p1 = transform.position;
            Vector3 p2 = transform.position;

            p1.y += curve.Evaluate(0.5f) * curveScale;
            p2.y += curve.Evaluate(-0.5f) * curveScale;

            Gizmos.DrawLine(p1, p2);
        }
        //float delta = dir * speed * Time.fixedDeltaTime;
        //float val = curve.Evaluate(t) * curveScale;
    }
}
