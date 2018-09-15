using UnityEngine;
using System.Collections;

public class ParticleController : MonoBehaviour {
    public bool playOnEnable;
    public float playOnEnableDelay = 0.1f;

    public bool stopOnDisable;
    public bool clearOnStop;
        
    private bool mStarted;

    public void Play(bool withChildren) {
        GetComponent<ParticleSystem>().Play(withChildren);
    }

    public void Stop() {
        GetComponent<ParticleSystem>().Stop();
    }

    public void Pause() {
        GetComponent<ParticleSystem>().Pause();
    }

    public void SetLoop(bool loop) {
        GetComponent<ParticleSystem>().loop = loop;
    }

    void OnEnable() {
        if(mStarted && playOnEnable && !GetComponent<ParticleSystem>().isPlaying) {
            GetComponent<ParticleSystem>().Clear();

            if(playOnEnableDelay > 0.0f)
                Invoke("DoPlay", playOnEnableDelay);
            else
                DoPlay();
        }
            
    }

    void DoPlay() {
        GetComponent<ParticleSystem>().Play();
    }

    void OnDisable() {
        CancelInvoke();

        if(mStarted && stopOnDisable) {
            GetComponent<ParticleSystem>().Stop();

            if(clearOnStop)
                GetComponent<ParticleSystem>().Clear();
        }
    }

    void Start() {
        mStarted = true;
        OnEnable();
    }
}
