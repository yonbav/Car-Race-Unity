using UnityEngine;
using System.Collections;

namespace DungeonArchitect.Builders.Isaac
{
    public abstract class IsaacRoomLayoutBuilder : MonoBehaviour
    {

        public abstract IsaacRoomLayout GenerateLayout(IsaacRoom room, System.Random random, int roomWidth, int roomHeight);
    }
}