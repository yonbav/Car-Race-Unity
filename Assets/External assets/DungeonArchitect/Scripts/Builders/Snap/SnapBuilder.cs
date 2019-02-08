using UnityEngine;
using System.Collections.Generic;
using DungeonArchitect;
using DungeonArchitect.Utils;
using DungeonArchitect.Builders.Snap.Impl;

namespace DungeonArchitect.Builders.Snap
{

    public class SnapBuilder : DungeonBuilder
    {
        SnapConfig snapConfig;
        SnapModel snapModel;
        
        new System.Random random;

        /// <summary>
        /// Builds the dungeon layout.  In this method, you should build your dungeon layout and save it in your model file
        /// No markers should be emitted here.   (EmitMarkers function will be called later by the engine to do that)
        /// </summary>
        /// <param name="config">The builder configuration</param>
        /// <param name="model">The dungeon model that the builder will populate</param>
        public override void BuildDungeon(DungeonConfig config, DungeonModel model)
        {
            base.BuildDungeon(config, model);

            propSockets.Clear();
        }

        /// <summary>
        /// Override the builder's emit marker function to emit our own markers based on the layout that we built
        /// You should emit your markers based on the layout you have saved in the model generated previously
        /// When the user is designing the theme interactively, this function will be called whenever the graph state changes,
        /// so the theme engine can populate the scene (BuildDungeon will not be called if there is no need to rebuild the layout again)
        /// </summary>
        public override void EmitMarkers()
        {
            base.EmitMarkers();

        }
        
        public override bool IsThemingSupported() { return false; }

        // This is called by the builders that do not support theming
        public override void BuildNonThemedDungeon(DungeonSceneProvider sceneProvider) {

            random = new System.Random((int)config.Seed);
            propSockets.Clear();

            // We know that the dungeon prefab would have the appropriate config and models attached to it
            // Cast and save it for future reference
            snapConfig = config as SnapConfig;
            snapModel = model as SnapModel;

            if (snapConfig == null)
            {
                Debug.LogError("No snap config script found in dungeon game object");
                return;
            }

            if (snapModel == null)
            {
                Debug.LogError("No snap model script found in dungeon game object");
                return;
            }
            
            // Generate the module info list
            var ModuleInfos = new List<ModuleInfo>();
            {
                var RegisteredModuleList = new List<GameObject>();
                RegisteredModuleList.AddRange(snapConfig.Modules);
                RegisteredModuleList.AddRange(snapConfig.StartModules);
                RegisteredModuleList.AddRange(snapConfig.EndModules);
                RegisteredModuleList.AddRange(snapConfig.BranchEndModules);
                var RegisteredModules = new HashSet<GameObject>(RegisteredModuleList);

                foreach (var RegisteredModule in RegisteredModules)
                {
                    var moduleInfo = GenerateModuleInfo(RegisteredModule);
                    ModuleInfos.Add(moduleInfo);
                }
            }


            var StartNode = new ModuleGrowthNode();
            StartNode.IncomingModuleDoorIndex = -1;
            StartNode.startNode = true;
            StartNode.ModuleTransform = Matrix4x4.identity;

            var OccupiedBounds = new List<Bounds>();

            var LayoutBuildState = new SnapLayoutBuildState();
            LayoutBuildState.ModuleInfoList = ModuleInfos;

            // Build the main branch
            ModuleBuildNode BuildNode = BuildLayoutRecursive(StartNode, OccupiedBounds, 1, snapConfig.MainBranchSize, true, false, LayoutBuildState);
            
            // Build the side branches
            {
                var MainBranchNodes = new List<ModuleBuildNode>();

                // Grab the nodes in the main branch
                {
                    ModuleBuildNode BranchNode = BuildNode;
                    while (BranchNode != null)
                    {
                        BranchNode.bMainBranch = true;
                        MainBranchNodes.Add(BranchNode);

                        // Move forward
                        if (BranchNode.Extensions.Count == 0)
                        {
                            break;
                        }

                        BranchNode = BranchNode.Extensions[0];
                    }
                }


                // Iterate through the nodes in the main branch and start branching out
                for (int i = 0; i < MainBranchNodes.Count; i++)
                {
                    ModuleBuildNode BranchStartNode = MainBranchNodes[i];
                    ModuleBuildNode BranchNextNode = i + 1 < MainBranchNodes.Count ? MainBranchNodes[i + 1] : null;

                    ModuleInfo BranchModule = BranchStartNode.Module;
                    int IncomingDoorIndex = BranchStartNode.IncomingDoorIndex;
                    int OutgoingDoorIndex = BranchNextNode != null ? BranchNextNode.IncomingDoorIndex : -1;
                    int NumDoors = BranchModule.ConnectionTransforms.Length;
                    for (int DoorIndex = 0; DoorIndex < NumDoors; DoorIndex++)
                    {
                        if (DoorIndex == IncomingDoorIndex || DoorIndex == OutgoingDoorIndex)
                        {
                            // These doors are already extended
                            continue;
                        }

                        bool bGrowFromHere = (random.NextFloat() < snapConfig.SideBranchProbability);
                        if (!bGrowFromHere)
                        {
                            continue;
                        }

                        // TODO: Optimize me.  it recalculates the the bounds for the whole tree for every main branch node
                        OccupiedBounds.Clear();
                        CalculateOccupiedBounds(BuildNode, OccupiedBounds);

                        var BranchGrowNode = new ModuleGrowthNode();
                        BranchGrowNode.IncomingModuleDoorIndex = DoorIndex;
                        BranchGrowNode.IncomingModule = BranchStartNode.Module;
                        BranchGrowNode.ModuleTransform = BranchStartNode.AttachmentConfig.AttachedModuleTransform;

                        LayoutBuildState = new SnapLayoutBuildState();
                        LayoutBuildState.ModuleInfoList = ModuleInfos;
                        ModuleBuildNode BranchBuildNode = BuildLayoutRecursive(BranchGrowNode, OccupiedBounds, 1, snapConfig.SideBranchSize, false, false, LayoutBuildState);
                        if (BranchBuildNode != null)
                        {
                            // Make sure we don't end up with an undesirable leaf node
                            if (BranchBuildNode.Extensions.Count == 0 && BranchBuildNode.Module != null && snapConfig.SideBranchSize > 1)
                            {
                                continue;
                            }

                            BranchBuildNode.Parent = BranchStartNode;
                            BranchStartNode.Extensions.Add(BranchBuildNode);
                        }
                    }
                }
            }

            snapModel.ResetModel();


            sceneProvider.OnDungeonBuildStart();

            // Spawn the modules and register them in the model
            {
                var spawnedModuleList = new List<SnapModule>();
                TraverseTree(BuildNode, delegate (ModuleBuildNode Node)
                {
                    // Spawn a module at this location
                    ModuleInfo moduleInfo = Node.Module;

                    var templateInfo = new GameObjectPropTypeData();
                    templateInfo.Template = moduleInfo.ModuleTemplate;
                    templateInfo.NodeId = moduleInfo.ModuleGuid.ToString();
                    templateInfo.Offset = Matrix4x4.identity;
                    templateInfo.IsStaticObject = true;

                    Node.spawnedModule = sceneProvider.AddGameObject(templateInfo, Node.AttachmentConfig.AttachedModuleTransform);

                    // Register this in the model
                    var snapModule = new SnapModule();
                    snapModule.InstanceID = Node.ModuleInstanceID;
                    spawnedModuleList.Add(snapModule);
                });
                snapModel.modules = spawnedModuleList.ToArray();
            }

            // Generate the list of connections
            {
                var connectionList = new List<SnapModuleConnection>();
                TraverseTree(BuildNode, delegate (ModuleBuildNode Node)
                {
                    if (Node.Parent != null)
                    {
                        var Connection = new SnapModuleConnection();
                        Connection.ModuleAInstanceID = Node.ModuleInstanceID;
                        Connection.DoorAIndex = Node.AttachmentConfig.AttachedModuleDoorIndex;

                        Connection.ModuleBInstanceID = Node.Parent.ModuleInstanceID;
                        Connection.DoorBIndex = Node.IncomingDoorIndex;

                        connectionList.Add(Connection);
                    }
                });
                snapModel.connections = connectionList.ToArray();
            }
            
            sceneProvider.OnDungeonBuildStop();

            FixupDoorStates(BuildNode);
        }

        T GetArrayEntry<T>(int index, T[] array) where T : class
        {
            if (index < 0 || index >= array.Length)
            {
                return null;
            }

            return array[index];
        }

        void FixupDoorStates(ModuleBuildNode rootNode)
        {
            var moduleConnections = new Dictionary<GameObject, SnapConnection[]>();
            TraverseTree(rootNode, delegate (ModuleBuildNode node)
            {
                if (!moduleConnections.ContainsKey(node.spawnedModule))
                {
                    var connections = node.spawnedModule.GetComponentsInChildren<SnapConnection>();
                    moduleConnections.Add(node.spawnedModule, connections);
                }
            });

            // Set everything to wall
            foreach (var connections in moduleConnections.Values)
            {
                foreach (var connection in connections)
                {
                    connection.UpdateDoorState(false);
                }
            }

            var stack = new Stack<ModuleBuildNode>();
            stack.Push(rootNode);
            while (stack.Count > 0)
            {
                ModuleBuildNode top = stack.Pop();
                if (top == null) continue;
                ModuleBuildNode parent = top.Parent;
                if (parent != null)
                {
                    if (top.spawnedModule != null && parent.spawnedModule != null)
                    {
                        int ParentDoorIndex = top.IncomingDoorIndex;
                        int TopDoorIndex = top.AttachmentConfig.AttachedModuleDoorIndex;
                        var parentConnection = GetArrayEntry<SnapConnection>(ParentDoorIndex, moduleConnections[parent.spawnedModule]);
                        var topConnection = GetArrayEntry<SnapConnection>(TopDoorIndex, moduleConnections[top.spawnedModule]);

                        if (parentConnection != null)
                        {
                            parentConnection.UpdateDoorState(true);
                        }
                        if (topConnection != null)
                        {
                            topConnection.UpdateDoorState(true);
                        }
                    }
                }

                foreach (var extension in top.Extensions)
                {
                    stack.Push(extension);
                }
            }
        }

        
        delegate void VisitTreeNodeDelegate(ModuleBuildNode Node);
        void TraverseTree(ModuleBuildNode RootNode, VisitTreeNodeDelegate VisitTreeNode)
        {
            var stack = new Stack<ModuleBuildNode>();
            stack.Push(RootNode);
            
            while (stack.Count > 0)
            {
                ModuleBuildNode Top = stack.Pop();
                if (Top == null) continue;

                VisitTreeNode(Top);

                // Add children
                foreach (ModuleBuildNode Extension in Top.Extensions)
                {
                    stack.Push(Extension);
                }
            }
        }


        static void CalculateOccupiedBounds(ModuleBuildNode Node, List<Bounds> OccupiedBounds)
        {
            if (Node == null) return;
            OccupiedBounds.Add(Node.AttachmentConfig.AttachedModuleWorldBounds);

            foreach (var ChildNode in Node.Extensions)
            {
                CalculateOccupiedBounds(ChildNode, OccupiedBounds);
            }
        }

        static Bounds GetBounds(GameObject target)
        {
            var bounds = new Bounds();

            var renderers = target.GetComponentsInChildren<Renderer>();

            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        void FindConnectionTransforms(GameObject go, out Matrix4x4[] outTransforms)
        {
            if (go == null)
            {
                outTransforms = new Matrix4x4[0];
                return;
            }

            var connections = go.GetComponentsInChildren<SnapConnection>();
            var result = new List<Matrix4x4>();
            foreach (var connection in connections)
            {
                var transform = connection.transform;
                var worldTransform = Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale);

                result.Add(worldTransform);
            }

            outTransforms = result.ToArray();
        }

        ModuleInfo GenerateModuleInfo(GameObject modulePrefab)
        {
            var moduleInfo = new ModuleInfo();
            moduleInfo.ModuleTemplate = modulePrefab;
            moduleInfo.ModuleGuid = System.Guid.NewGuid();
            moduleInfo.Bounds = GetBounds(modulePrefab);

            // Find the transform of the doors
            FindConnectionTransforms(modulePrefab, out moduleInfo.ConnectionTransforms);

            return moduleInfo;
        }

        void DebugLog(string name, ref Matrix4x4 Transform)
        {
            Debug.Log(string.Format(@"{0}: Pos:{1} | Rot:{2} | Scl:{3}", 
                name,
                Matrix.GetTranslation(ref Transform),
                Matrix.GetRotation(ref Transform).eulerAngles,
                Matrix.GetScale(ref Transform)));
        }

        Matrix4x4 FindAttachmentTransform(ref Matrix4x4 ParentModuleTransform, ref Matrix4x4 IncomingDoorTransform, ref Matrix4x4 AttachmentDoorTransform) 
        {
            Matrix4x4 DesiredDoorTransform = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 180, 0), Vector3.one) * IncomingDoorTransform * ParentModuleTransform;
            
            // Calculate the rotation
            Quaternion DesiredRotation;
            {
                Vector3 TargetVector = Matrix.GetRotation(ref DesiredDoorTransform) * new Vector3(1, 0, 0);
                Vector3 SourceVector = Matrix.GetRotation(ref AttachmentDoorTransform) * new Vector3(1, 0, 0);

                Quaternion TargetRot = Quaternion.LookRotation(TargetVector, Vector3.up);
                Quaternion SourceRot = Quaternion.LookRotation(SourceVector, Vector3.up);
                DesiredRotation = TargetRot * Quaternion.Inverse(SourceRot);
            }

            // Calculate the translation
            Vector3 DesiredOffset;
            {
                Vector3 IncomingDoorPosition = Matrix.GetTranslation(ref IncomingDoorTransform);
                IncomingDoorPosition = ParentModuleTransform.MultiplyPoint3x4(IncomingDoorPosition);
                Vector3 ClampTarget = IncomingDoorPosition;

                Vector3 LocalDoorPosition = DesiredRotation * Matrix.GetTranslation(ref AttachmentDoorTransform);
                DesiredOffset = ClampTarget - LocalDoorPosition;
            }
            
            var ModuleTransform = Matrix4x4.TRS(DesiredOffset, DesiredRotation, Vector3.one);
            return ModuleTransform;
        }

        bool FindAttachmentConfiguration(ModuleInfo TargetModule, ModuleInfo IncomingModule, ref Matrix4x4 IncomingModuleTransform,
	            int IncomingDoorIndex, List<Bounds> OccupiedBounds, ref SnapAttachmentConfiguration OutAttachmentConfig)
        {
            int NumDoors = TargetModule.ConnectionTransforms.Length;
            if (IncomingDoorIndex >= NumDoors) return false;

            if (IncomingDoorIndex < 0 || IncomingModule == null)
            {
                OutAttachmentConfig.AttachedModule = TargetModule;
                OutAttachmentConfig.AttachedModuleDoorIndex = random.Range(0, NumDoors - 1);
                OutAttachmentConfig.AttachedModuleWorldBounds = TargetModule.Bounds;
                OutAttachmentConfig.AttachedModuleTransform = Matrix4x4.identity;
                return true;
            }

            if (IncomingDoorIndex >= NumDoors) return false;
            Matrix4x4 IncomingDoorTransform = IncomingModule.ConnectionTransforms[IncomingDoorIndex];
            bool bFoundValid = false;
            int[] ShuffledIndices = MathUtils.GetShuffledIndices(NumDoors, random);
            for (int si = 0; si < ShuffledIndices.Length; si++)
            {
                int Index = ShuffledIndices[si];
                Matrix4x4 AttachmentDoorTransform = TargetModule.ConnectionTransforms[Index];

                // Align the module with a door that fits the incoming door
                Matrix4x4 ModuleTransform = FindAttachmentTransform(ref IncomingModuleTransform, ref IncomingDoorTransform, ref AttachmentDoorTransform);

                if (!snapConfig.RotateModulesToFit)
                {
                    // Rotation is not allowed. Check if we rotated the module
                    var moduleRotation = Matrix.GetRotation(ref ModuleTransform);
                    if (Mathf.Abs(moduleRotation.eulerAngles.y) > 0.1f)
                    {
                        // Module was rotated
                        continue;
                    }
                }

                {
                    // Calculate the bounds of the module 
                    Bounds ModuleWorldBounds = TargetModule.Bounds;
                    ModuleWorldBounds = MathUtils.TransformBounds(ModuleTransform, ModuleWorldBounds);
                    Bounds ContractedModuleWorldBounds = ModuleWorldBounds;
                    ContractedModuleWorldBounds.Expand(-1 * (snapConfig.CollisionTestContraction));

                    // Check if this module would intersect with any of the existing modules
                    bool bIntersects = false;
                    foreach (var OccupiedBound in OccupiedBounds) {
                        if (ContractedModuleWorldBounds.Intersects(OccupiedBound))
                        {
                            // intersects. Do not spawn a module here
                            bIntersects = true;
                            break;
                        }
                    }
                    if (bIntersects)
                    {
                        continue;
                    }

                    // We found a valid module. Use this
                    OutAttachmentConfig.AttachedModule = TargetModule;
                    OutAttachmentConfig.AttachedModuleDoorIndex = Index;
                    OutAttachmentConfig.AttachedModuleWorldBounds = ModuleWorldBounds;
                    OutAttachmentConfig.AttachedModuleTransform = ModuleTransform;
                    bFoundValid = true;
                    break;
                }
            }

	        return bFoundValid;
        }

        ModuleBuildNode BuildLayoutRecursive(ModuleGrowthNode GrowthNode, List<Bounds> OccupiedBounds, 
		        int DepthFromStart, int DesiredDepth, bool bMainBranch, bool bForceIgnoreEndModule, SnapLayoutBuildState RecursiveState) 
        {
            if (RecursiveState.NumTries >= snapConfig.MaxProcessingPower)
            {
                return null;
            }
            RecursiveState.NumTries++;

            if (DepthFromStart > DesiredDepth)
            {
                return null;
            }

            ModuleGrowthNode Top = GrowthNode;

            // Pick a door from this module to extend
            int BestValidMainBranchDifference = int.MaxValue;
            ModuleBuildNode BestBuildNode = null;

            int SnapModuleListLength = RecursiveState.ModuleInfoList.Count;

            int[] ShuffledIndices = MathUtils.GetShuffledIndices(SnapModuleListLength, random);
            for (int si = 0; si < ShuffledIndices.Length; si++)
            {
                int Index = ShuffledIndices[si];

                ModuleInfo Module = RecursiveState.ModuleInfoList[Index];

                var AttachmentConfig = new SnapAttachmentConfiguration();
                if (!FindAttachmentConfiguration(Module, Top.IncomingModule, ref Top.ModuleTransform, Top.IncomingModuleDoorIndex, OccupiedBounds, ref AttachmentConfig))
                {
                    continue;
                }

                var  BuildNode = new ModuleBuildNode();
                BuildNode.AttachmentConfig = AttachmentConfig;
                BuildNode.IncomingDoorIndex = Top.IncomingModuleDoorIndex;
                BuildNode.Module = Module;

                if (DepthFromStart == DesiredDepth)
                {
                    // This has to be the leaf node
                    return BuildNode;
                }

                if (BestBuildNode == null)
                {
                    BestBuildNode = BuildNode;
                }

                // We found a valid module. Use this
                OccupiedBounds.Add(AttachmentConfig.AttachedModuleWorldBounds);

                int AttachmentDoorIndex = AttachmentConfig.AttachedModuleDoorIndex;

                // Extend from this door further
                for (int ExtensionDoorIndex = 0; ExtensionDoorIndex < Module.ConnectionTransforms.Length; ExtensionDoorIndex++)
                {
                    if (ExtensionDoorIndex == AttachmentDoorIndex && Top.IncomingModuleDoorIndex != -1)
                    {
                        // Don't want to extend from the door we came in through
                        continue;
                    }

                    int ModuleCountContribution = 1;

                    // Grow this branch further
                    var NextNode = new ModuleGrowthNode();
                    NextNode.IncomingModuleDoorIndex = ExtensionDoorIndex;
                    NextNode.ModuleTransform = AttachmentConfig.AttachedModuleTransform;
                    NextNode.IncomingModule = Module;
                    ModuleBuildNode ExtensionNode = BuildLayoutRecursive(NextNode, OccupiedBounds, DepthFromStart + ModuleCountContribution, DesiredDepth,
                        bMainBranch, false, RecursiveState);

                    if (ExtensionNode != null)
                    {
                        int BranchLength = DepthFromStart + ExtensionNode.DepthFromLeaf;
                        int ValidDistanceDifference = Mathf.Abs(BranchLength - DesiredDepth);
                        if (ValidDistanceDifference < BestValidMainBranchDifference || RecursiveState.bFoundBestBuild)
                        {
                            BestValidMainBranchDifference = ValidDistanceDifference;
                            BuildNode.Extensions.Clear();
                            ExtensionNode.Parent = BuildNode;
                            BuildNode.Extensions.Add(ExtensionNode);
                            BuildNode.DepthFromLeaf = Mathf.Max(BuildNode.DepthFromLeaf, ExtensionNode.DepthFromLeaf + ModuleCountContribution);

                            BestBuildNode = BuildNode;
                        }

                        if (BranchLength >= DesiredDepth)
                        {
                            // We found a branch with the desired length
                            RecursiveState.bFoundBestBuild = true;
                        }

                        if (RecursiveState.bFoundBestBuild)
                        {
                            break;
                        }
                    }
                }

                // Remove it since we move out 
                OccupiedBounds.Remove(AttachmentConfig.AttachedModuleWorldBounds);

                if (RecursiveState.bFoundBestBuild)
                {
                    break;
                }
            }

            return BestBuildNode;
        }
    }


    namespace Impl
    {
        class ModuleInfo
        {
            /// <summary>
            /// The prefab template of the module
            /// </summary>
            public GameObject ModuleTemplate;

            /// <summary>
            ///  A unique ID assigned to this actor module (unique to the prefab). Will be different on each build
            /// </summary>
            public System.Guid ModuleGuid;

            /// <summary>
            /// The bounds of the prefab
            /// </summary>
            public Bounds Bounds;

            /// <summary>
            /// The local transforms of each SnapConnection child actor in the module actor
            /// </summary>
            public Matrix4x4[] ConnectionTransforms;
        }

        class SnapAttachmentConfiguration
        {
            public ModuleInfo AttachedModule;
            public int AttachedModuleDoorIndex;
            public Bounds AttachedModuleWorldBounds;
            public Matrix4x4 AttachedModuleTransform;
        }

        class ModuleGrowthNode
        {
            public ModuleGrowthNode()
            {
                IncomingModuleDoorIndex = -1;
                startNode = false;
                ModuleTransform = Matrix4x4.identity;
            }

            public Matrix4x4 ModuleTransform;
            public ModuleInfo IncomingModule;
            public int IncomingModuleDoorIndex;
            public bool startNode;
        }

        class ModuleBuildNode
        {

            public static string GenerateModuleInstanceID(System.Guid ModuleGuid)
            {
                return "NODE-SNAPMOD-" + ModuleGuid.ToString();
            }

            public ModuleBuildNode()
            {
                ModuleInstanceID = GenerateModuleInstanceID(System.Guid.NewGuid());
                IncomingDoorIndex = -1;
                DepthFromLeaf = 1;
                bMainBranch = false;
            }

            public string ModuleInstanceID;
            public ModuleInfo Module;
            public int IncomingDoorIndex;
            public SnapAttachmentConfiguration AttachmentConfig;
            public int DepthFromLeaf;
            public List<ModuleBuildNode> Extensions = new List<ModuleBuildNode>();
            public ModuleBuildNode Parent;
            public bool bMainBranch;

            /// <summary>
            /// Reference to the spawned module. This will be set later
            /// </summary>
            public GameObject spawnedModule = null;     
        };

        class SnapLayoutBuildState
        {
            public SnapLayoutBuildState()
            {
                bSafetyBailOut = false;
                NumTries = 0;
                bFoundBestBuild = false;
            }

            /**
            Searching a dense tree can lead to billions of possibilities
            If this flag is set, the search bails out early to avoid a hang
            */
            public bool bSafetyBailOut;
            public int NumTries;
            public bool bFoundBestBuild;
            public List<ModuleInfo> ModuleInfoList = new List<ModuleInfo>();
        };
    }
}