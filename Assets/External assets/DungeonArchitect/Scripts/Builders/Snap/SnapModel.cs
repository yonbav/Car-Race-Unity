using UnityEngine;
using System.Collections.Generic;
using DungeonArchitect;
using DungeonArchitect.Utils;

namespace DungeonArchitect.Builders.Snap
{

    [System.Serializable]
    public class SnapModule
    {
        /// <summary>
        /// The instance id of the module
        /// </summary>
        public string InstanceID;
    }

    [System.Serializable]
    public class SnapModuleConnection
    {
        /// <summary>
        /// The instance ID of the spawned module (See ModuleInstance structure)
        /// </summary>
        public string ModuleAInstanceID;

        /// <summary>
        /// The index of the door(see ModuleInfo structure)
        /// </summary>
        public int DoorAIndex;

        /// <summary>
        /// The instance ID of the spawned module (See ModuleInstance structure)
        /// </summary>
        public string ModuleBInstanceID;

        /// <summary>
        /// The index of the door(see ModuleInfo structure)
        /// </summary>
        public int DoorBIndex;
    }

    public class SnapModel : DungeonModel
    {
        [HideInInspector]
        public SnapModule[] modules;

        [HideInInspector]
        public SnapModuleConnection[] connections;


        public override void ResetModel()
        {
            modules = new SnapModule[0];
            connections = new SnapModuleConnection[0];
        }
    }

}