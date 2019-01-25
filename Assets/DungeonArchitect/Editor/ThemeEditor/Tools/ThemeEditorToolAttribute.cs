using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using DungeonArchitect.Editors;

namespace DungeonArchitect.Editors
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ThemeEditorToolAttribute : Attribute
    {
        string path;
        public string Path
        {
            get { return path; }
        }

        int priority;
        public int Priority
        {
            get { return priority; }
        }

        public ThemeEditorToolAttribute(string path, int priority)
        {
            this.path = path;
            this.priority = priority;
        }
    }

    public delegate void ThemeEditorToolFunctionDelegate(DungeonArchitectGraphEditor editor);

    public class ThemeEditorToolFunctionInfo
    {
        public ThemeEditorToolFunctionDelegate function;
        public ThemeEditorToolAttribute attribute;
    }
}
