Import the Vehicles Standard Packages (Assets > Import Package > Vehicles)
Import Unity Post Processing Stack: https://www.assetstore.unity3d.com/en/#!/content/83912
Hit play and drive through the procedurally generated city

A new city will be generated whenever you play. Have a look at the script ToonGameController
It randomizes the seed and builds the city to generate a new one every time you hit play
-------------------------------
	void Start () {
		if (dungeon != null)
        {
            dungeon.Config.Seed = (uint)Random.Range(0, 100000);
            dungeon.Build();
        }
	}
-------------------------------
