//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using System.Collections;
using DungeonArchitect;
using DungeonArchitect.Utils;
using SharpNav;
using SharpNav.Crowds;

namespace DungeonArchitect.Navigation {
	public abstract class DungeonNavAgent : MonoBehaviour {
		public abstract void Resume();
		public abstract void Stop();
		public abstract float GetRemainingDistance();
		public abstract Vector3 Destination { get; set; }
		public abstract Vector3 Velocity { get; set; }
		public abstract Vector3 Direction { get; }
	}
}
