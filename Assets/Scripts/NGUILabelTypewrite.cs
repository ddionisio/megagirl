using UnityEngine;
using System.Collections;
using System.Text;

public class NGUILabelTypewrite : MonoBehaviour {

    public UILabel target;
    public float startDelay;
    public float delay;
    public bool playOnEnable;
    public string text;

    public GameObject endGO;

    private bool mStarted;
    private bool mActive;
    private bool mEnded;

    public bool isPlaying { get { return mActive; } }
    public bool hasEnded { get { return mEnded; } }

    public void Play() {
        if(mActive)
            StopAllCoroutines();

        StartCoroutine(DoType());
    }

    public void Stop(bool fillRemainingText) {
        StopAllCoroutines();
        mActive = false;
        mEnded = true;
        if(endGO) endGO.SetActive(false);

        if(fillRemainingText)
            target.text = text;
    }

    void OnEnable() {
        if(mStarted && playOnEnable)
            Play();
    }

    void OnDisable() {
        Stop(false);
    }

    void Awake() {
        if(target == null)
            target = GetComponent<UILabel>();
    }

	// Use this for initialization
	void Start () {
        mStarted = true;
        if(playOnEnable)
            Play();
	}
	
    IEnumerator DoType() {
        if(endGO) endGO.SetActive(true);

        mEnded = false;
        mActive = true;
        target.text = "";
        YieldInstruction wait; if(delay <= 0.0f) wait = new WaitForFixedUpdate(); else wait = new WaitForSeconds(delay);
        StringBuilder textBuffer = new StringBuilder(text.Length);

        yield return new WaitForSeconds(startDelay);

        for(int i = 0; i < text.Length; i++) {
            textBuffer.Append(text[i]);
            target.text = textBuffer.ToString();
            yield return wait;
        }

        mActive = false;
        mEnded = true;

        if(endGO) endGO.SetActive(false);
    }
}
