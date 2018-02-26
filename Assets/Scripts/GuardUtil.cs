using System;
using UnityEngine;

public class GuardUtil : MonoBehaviour {
	public static event Action OnGuardCaughtPlayer;

	public enum State { Patrol, Alert, Investigate, Chase }
    public State state;

	// Use this for initialization
	void Start () {
        print("Guard Util Starting");
	}

    public void GuardOnCaughtPlayer() {
		if (OnGuardCaughtPlayer != null)
		{
			OnGuardCaughtPlayer();
		}
    }
}
