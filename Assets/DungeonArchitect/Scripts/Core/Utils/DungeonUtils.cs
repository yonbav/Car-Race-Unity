using UnityEngine;
using System.Collections.Generic;
using DungeonArchitect;


public class DungeonUtils {

    public static List<GameObject> GetDungeonObjects(Dungeon dungeon)
    {
        var result = new List<GameObject>();

        var components = GameObject.FindObjectsOfType<DungeonSceneProviderData>();
        foreach (var component in components)
        {
            if (component.dungeon == dungeon)
            {
                result.Add(component.gameObject);
            }
        }

        return result;
    }
}
