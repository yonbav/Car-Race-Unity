//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using System.Collections;

namespace DungeonArchitect
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class MetaAttribute : System.Attribute
    {
        public string displayText;
        public MetaAttribute(string displayText)
        {
            this.displayText = displayText;
        }
    }
}
