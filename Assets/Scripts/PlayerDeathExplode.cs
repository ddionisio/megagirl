using UnityEngine;
using System.Collections;

public class PlayerDeathExplode : MonoBehaviour {
    public GameObject template;
    public int count = 8;
    public float speed;

    private Transform[] mStuffs;
    private bool mStarted;

    void Awake() {
        mStuffs = new Transform[count];

        float rot = 0.0f;
        float rotStep = 360.0f / ((float)count);

        Vector3 pos = transform.position;

        for(int i = 0; i < count; i++) {
            GameObject go = (GameObject)GameObject.Instantiate(template, pos, Quaternion.Euler(0.0f, 0.0f, rot));
            mStuffs[i] = go.transform;
            mStuffs[i].parent = transform;
            rot += rotStep;
        }
    }

    // Use this for initialization
    void Start() {
        mStarted = true;
    }

    // Update is called once per frame
    void Update() {
        if(mStarted) {
            for(int i = 0; i < count; i++) {
                Vector3 p = mStuffs[i].position;
                p += mStuffs[i].up * speed * Time.deltaTime;
                mStuffs[i].position = p;
            }
        }
    }
}
