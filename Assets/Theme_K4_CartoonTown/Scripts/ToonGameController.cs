using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DungeonArchitect;

public class ToonGameController : MonoBehaviour {
    public Dungeon dungeon;

	// Use this for initialization
	void Start () {
		if (dungeon != null)
        {
            dungeon.Config.Seed = (uint)Random.Range(0, 100000);
            dungeon.Build();
        }
	}
	
}
