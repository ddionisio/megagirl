using UnityEngine;
using System.Collections;

public class PlatformerSpriteController : MonoBehaviour {
    public delegate void Callback(PlatformerSpriteController ctrl);
    public delegate void CallbackClip(PlatformerSpriteController ctrl, tk2dSpriteAnimationClip clip);
    public delegate void CallbackClipFrame(PlatformerSpriteController ctrl, tk2dSpriteAnimationClip clip, int frame);

    public enum State {
        None,
        Slide,
        Climb
    }

    public bool leftFlip = true;
    public bool defaultLeft = false;

    public tk2dSpriteAnimator anim;
    public PlatformerController controller;

    public tk2dSpriteAnimation[] animLibs; //use to swap

    public string idleClip = "idle";
    public string moveClip = "move";

    public string[] upClips = { "up" }; //based on jump counter
    public string[] downClips = { "down" }; //based on jump counter

    public string wallStickClip = "wall";
    public string wallJumpClip = "wallJump";

    public string slideClip = "slide";
    //public string climbClip = "climb";

    public bool multiSprites;

    public float minSpeed = 0.5f;//used if useVelocitySpeed=true 
    public float framePerMeter = 0.1f; //used if useVelocitySpeed=true 

    public ParticleSystem wallStickParticle;

    public event Callback flipCallback;
    public event CallbackClip clipFinishCallback;
    public event CallbackClipFrame clipFrameEventCallback;

    //TODO: queue system

    private class ClipData {
        public tk2dSpriteAnimationClip idle;
        public tk2dSpriteAnimationClip move;
        public tk2dSpriteAnimationClip[] ups;
        public tk2dSpriteAnimationClip[] downs;
        public tk2dSpriteAnimationClip wallStick;
        public tk2dSpriteAnimationClip wallJump;

        public tk2dSpriteAnimationClip slide;
        //public tk2dSpriteAnimationClip climb;

        public void RefreshClipRefs() {
        }

        public ClipData(PlatformerSpriteController ctrl, tk2dSpriteAnimation lib) {
            idle = lib.GetClipByName(ctrl.idleClip);

            move = lib.GetClipByName(ctrl.moveClip);

            ups = new tk2dSpriteAnimationClip[ctrl.upClips.Length];
            for(int i = 0, len = ctrl.upClips.Length; i < len; i++)
                ups[i] = lib.GetClipByName(ctrl.upClips[i]);

            downs = new tk2dSpriteAnimationClip[ctrl.downClips.Length];
            for(int i = 0, len = ctrl.downClips.Length; i < len; i++)
                downs[i] = lib.GetClipByName(ctrl.downClips[i]);

            wallStick = lib.GetClipByName(ctrl.wallStickClip);
            wallJump = lib.GetClipByName(ctrl.wallJumpClip);

            slide = lib.GetClipByName(ctrl.slideClip);
            //climb = lib.GetClipByName(ctrl.climbClip);
        }
    }

    private bool mIsLeft;
    private tk2dSpriteAnimationClip mOverrideClip;
    private State mState;

    private tk2dSpriteAnimation mDefaultAnimLib;
    private ClipData mDefaultClipDat;

    private ClipData[] mLibClips;

    private int mAnimLibIndex = -1; //-1 is default
    private bool mAnimVelocitySpeedEnabled;

    private tk2dBaseSprite[] mMultiSprites; //if multisprites is true

    public tk2dSpriteAnimationClip overrideClip {
        get { return mOverrideClip; }
    }

    public bool isLeft { 
        get { return mIsLeft; }
        set {
            if(mIsLeft != value) {
                mIsLeft = value;

                bool flip = mIsLeft ? leftFlip : !leftFlip;

                if(multiSprites) {
                    for(int i = 0; i < mMultiSprites.Length; i++)
                        mMultiSprites[i].FlipX = flip;
                }

                if(anim)
                    anim.Sprite.FlipX = flip;

                if(flipCallback != null)
                    flipCallback(this);
            }
        }
    }

    public State state {
        get { return mState; }
        set {
            if(mState != value) {
                mState = value;
            }
        }
    }

    /// <summary>
    /// Set to true to make framerate based on velocity
    /// </summary>
    public bool useVelocitySpeed {
        get { return mAnimVelocitySpeedEnabled; }
        set {
            if(mAnimVelocitySpeedEnabled != value) {
                mAnimVelocitySpeedEnabled = value;

                if(!mAnimVelocitySpeedEnabled)
                    anim.ClipFps = 0.0f;
            }
        }
    }

    /// <summary>
    /// Swap the animation library, set to -1 to revert to default
    /// </summary>
    public int animLibIndex {
        get { return mAnimLibIndex; }
        set {
            if(mAnimLibIndex != value) {
                tk2dSpriteAnimationClip lastClip = anim.CurrentClip;
                
                anim.Stop();

                tk2dSpriteAnimation newLib = value >= 0 && value < animLibs.Length ? animLibs[value] : mDefaultAnimLib;

                anim.Library = newLib;
                mAnimLibIndex = value;

                if(lastClip != null) {
                    float lastFrame = lastClip.wrapMode == tk2dSpriteAnimationClip.WrapMode.Single ? 0.0f : (float)anim.CurrentFrame;

                    tk2dSpriteAnimationClip newClip = newLib.GetClipByName(lastClip.name);
                    if(newClip != null) {
                        anim.Play(newClip, lastFrame / lastClip.fps, lastClip.fps);
                    }

                    if(mOverrideClip != null) {
                        if(newClip != null && mOverrideClip.name == newClip.name) {
                            mOverrideClip = newClip;
                        }
                        else {
                            if(mOverrideClip.wrapMode == tk2dSpriteAnimationClip.WrapMode.Once && clipFinishCallback != null) {
                                clipFinishCallback(this, mOverrideClip);
                            }

                            mOverrideClip = null;
                        }
                    }
                }
            }
        }
    }

    public void ResetAnimation() {
        mAnimVelocitySpeedEnabled = false;
        mOverrideClip = null;
        mIsLeft = defaultLeft;

        if(anim) {
            anim.ClipFps = 0.0f;
            anim.Sprite.FlipX = mIsLeft ? leftFlip : !leftFlip;
        }

        if(multiSprites) {
            for(int i = 0; i < mMultiSprites.Length; i++)
                mMultiSprites[i].FlipX = mIsLeft ? leftFlip : !leftFlip;
        }

        if(wallStickParticle) {
            wallStickParticle.loop = false;
            wallStickParticle.Stop();
        }
    }

    public void PlayOverrideClip(string clipName) {
        //assume its loop type is 'once'
        tk2dSpriteAnimationClip clip = anim.GetClipByName(clipName);
        if(clip != null) {
            mOverrideClip = clip;
            anim.Play(mOverrideClip);
        }
    }

    public void StopOverrideClip() {
        if(mOverrideClip != null) {
            /*if(mOverrideClip.wrapMode == tk2dSpriteAnimationClip.WrapMode.Once && overrideClipFinishCallback != null) {
                overrideClipFinishCallback(this, mOverrideClip);
            }*/

            anim.Stop();
            mOverrideClip = null;
        }
    }

    public void RefreshClips() {
        if(anim) {
            mDefaultAnimLib = anim.Library;
            mDefaultClipDat = new ClipData(this, mDefaultAnimLib);
        }
    }

    void OnDestroy() {
        flipCallback = null;
        clipFinishCallback = null;
        clipFrameEventCallback = null;
    }

    void Awake() {
        if(anim == null)
            anim = GetComponent<tk2dSpriteAnimator>();

        mIsLeft = defaultLeft;

        if(anim) {
            anim.AnimationCompleted += OnAnimationComplete;
            anim.AnimationEventTriggered += OnAnimationFrameEvent;


            anim.Sprite.FlipX = mIsLeft ? leftFlip : !leftFlip;

            mDefaultAnimLib = anim.Library;
            mDefaultClipDat = new ClipData(this, mDefaultAnimLib);
                    
            mLibClips = new ClipData[animLibs.Length];
            for(int i = 0, max = animLibs.Length; i < max; i++) {
                mLibClips[i] = new ClipData(this, animLibs[i]);
            }
        }

        if(multiSprites) {
            mMultiSprites = GetComponentsInChildren<tk2dBaseSprite>(true);
            for(int i = 0; i < mMultiSprites.Length; i++)
                mMultiSprites[i].FlipX = mIsLeft ? leftFlip : !leftFlip;
        }

        if(controller == null)
            controller = GetComponent<PlatformerController>();
    }

    tk2dSpriteAnimationClip GetMidAirClip(tk2dSpriteAnimationClip[] clips) {
        if(clips == null || clips.Length == 0)
            return null;

        int ind = controller.jumpCounterCurrent;

        return ind >= clips.Length ? clips[clips.Length - 1] : clips[ind];
    }

    // Update is called once per frame
    void Update() {
        if(controller == null)
            return;

        if(mAnimVelocitySpeedEnabled) {
            float spd = controller.rigidbody.velocity.magnitude;
            anim.ClipFps = spd > minSpeed ? spd * framePerMeter : 0.0f;
        }

        if(mOverrideClip != null)
            return;

        bool left = mIsLeft;

        ClipData dat = mAnimLibIndex == -1 ? mDefaultClipDat : mLibClips[mAnimLibIndex];

        switch(mState) {
            case State.None:
                if(controller.isJumpWall) {
                    if(anim) anim.Play(dat.wallJump);

                    left = controller.localVelocity.x < 0.0f;
                }
                else if(controller.isWallStick) {
                    if(wallStickParticle) {
                        if(wallStickParticle.isStopped) {
                            wallStickParticle.Play();
                        }

                        wallStickParticle.loop = true;
                    }

                    if(anim) anim.Play(dat.wallStick);

                    left = M8.MathUtil.CheckSide(controller.wallStickCollide.normal, controller.dirHolder.up) == M8.MathUtil.Side.Right;

                }
                else {
                    if(wallStickParticle)
                        wallStickParticle.loop = false;

                    if(anim) {
                        if(controller.isGrounded) {
                            if(controller.moveSide != 0.0f) {
                                anim.Play(dat.move);
                            }
                            else {
                                anim.Play(dat.idle);
                            }
                        }
                        else {
                            tk2dSpriteAnimationClip clip;

                            if(controller.localVelocity.y <= 0.0f) {
                                clip = GetMidAirClip(dat.downs);
                            }
                            else {
                                clip = GetMidAirClip(dat.ups);
                            }

                            if(clip != null)
                                anim.Play(clip);
                        }
                    }

                    if(controller.moveSide != 0.0f) {
                        left = controller.moveSide < 0.0f;
                    }
                }
                break;

            case State.Slide:
                if(anim)
                    anim.Play(dat.slide);

                if(controller.moveSide != 0.0f) {
                    left = controller.moveSide < 0.0f;
                }
                break;
        }

        isLeft = left;
    }

    void OnAnimationComplete(tk2dSpriteAnimator _anim, tk2dSpriteAnimationClip _clip) {
        if(_clip == mOverrideClip) {
            mOverrideClip = null;

            if(clipFinishCallback != null) {
                clipFinishCallback(this, _clip);
            }
        }
    }

    void OnAnimationFrameEvent(tk2dSpriteAnimator _anim, tk2dSpriteAnimationClip _clip, int _frame) {

        if(clipFrameEventCallback != null)
            clipFrameEventCallback(this, _clip, _frame);
    }
}
