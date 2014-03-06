using UnityEngine;
using System.Collections;

public class AchievementOnStart : AchievementNotifier {
    public float delay = 2.0f;

	// Use this for initialization
	void Start() {
        Notify();
	}
}
