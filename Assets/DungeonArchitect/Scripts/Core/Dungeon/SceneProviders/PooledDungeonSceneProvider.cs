//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using System.Collections.Generic;
using DungeonArchitect.Utils;

namespace DungeonArchitect
{
    /// <summary>
    /// Implementation of the Scene provider that adds object pooling over the existing functionality.
    /// This is useful for quick rebuilding and better performance, as object in the scene are reused
    /// while rebuilding, instead of destroying everything and rebuilding
    /// </summary>
	public class PooledDungeonSceneProvider : DungeonSceneProvider {
		// Pools list of game objects by their node ids
		Dictionary<string, Queue<GameObject>> pooledObjects = new Dictionary<string, Queue<GameObject>>();

		public override void OnDungeonBuildStart() {
            base.OnDungeonBuildStart();
			pooledObjects.Clear ();
			var items = GameObject.FindObjectsOfType<DungeonSceneProviderData>();
			foreach (var item in items) {
                if (item == null) continue;
                if (item.dungeon != this.dungeon) continue;

				if (!pooledObjects.ContainsKey(item.NodeId)) {
					pooledObjects.Add(item.NodeId, new Queue<GameObject>());
				}
				pooledObjects[item.NodeId].Enqueue(item.gameObject);
			}
		}

		public override void OnDungeonBuildStop() {
			// Destroy all unused objects from the pool
			foreach (var objects in pooledObjects.Values) {
				foreach (var obj in objects) {
					if (Application.isPlaying) {
						Destroy(obj);
					} else {
						DestroyImmediate(obj);
					}
				}
			}

			pooledObjects.Clear ();
		}

		public override GameObject AddSprite(SpritePropTypeData spriteProp, Matrix4x4 transform) {
			if (spriteProp == null) return null;
			string NodeId = spriteProp.NodeId;
			
			if (spriteProp.sprite == null) {
				return null;
			}

			FlipSpriteTransform(ref transform, spriteProp.sprite);

			GameObject item = null;
			// Try to reuse an object from the pool
			if (pooledObjects.ContainsKey (NodeId) && pooledObjects [NodeId].Count > 0) {
				item = pooledObjects [NodeId].Dequeue ();
				SetTransform (item.transform, transform);
			} else {
				// Pool is exhausted for this object
				item = BuildSpriteObject(spriteProp, transform, NodeId);
			}
			item.isStatic = spriteProp.IsStaticObject;

            return item;
		}
        
        public override void InvalidateNodeCache(string NodeId) {
            if (pooledObjects.ContainsKey(NodeId))
            {
                foreach (var obj in pooledObjects[NodeId])
                {
                    if (Application.isPlaying)
                    {
                        Destroy(obj);
                    }
                    else
                    {
                        DestroyImmediate(obj);
                    }
                }
                pooledObjects[NodeId].Clear();
            }
        }
		
        public override GameObject AddGameObject(GameObjectPropTypeData gameObjectProp, Matrix4x4 transform)
        {
			if (gameObjectProp == null) return null;
			var MeshTemplate = gameObjectProp.Template;
			string NodeId = gameObjectProp.NodeId;

			if (MeshTemplate == null) {
                return null;
			}
			
			// If we are in 2D mode, then flip the YZ axis
			{
				var mode2D = false;
				if (config != null) {
					mode2D = config.Mode2D;
				}
				if (mode2D) {
					var position = Matrix.GetTranslation(ref transform);
					FlipSpritePosition(ref position);
					Matrix.SetTranslation(ref transform, position);
				}
			}

			GameObject item = null;
			// Try to reuse an object from the pool
			if (pooledObjects.ContainsKey (NodeId) && pooledObjects [NodeId].Count > 0) {
				item = pooledObjects [NodeId].Dequeue ();
				SetTransform (item.transform, transform);
			} else {
				// Pool is exhausted for this object
				item = BuildGameObject(gameObjectProp, transform);


            }
			item.isStatic = gameObjectProp.IsStaticObject;
            if (gameObjectProp.IsStaticObject)
            {
                RecursivelySetStatic(item.transform);
            }

            return item;
		}

		public override GameObject AddGameObjectFromArray(GameObjectArrayPropTypeData gameObjectArrayProp, int index, Matrix4x4 transform)
		{
			if (gameObjectArrayProp == null) return null;
			string NodeId = gameObjectArrayProp.NodeId + "_" + index.ToString();

			// If we are in 2D mode, then flip the YZ axis
			{
				var mode2D = false;
				if (config != null) {
					mode2D = config.Mode2D;
				}
				if (mode2D) {
					var position = Matrix.GetTranslation(ref transform);
					FlipSpritePosition(ref position);
					Matrix.SetTranslation(ref transform, position);
				}
			}

			GameObject item = null;
			// Try to reuse an object from the pool
			if (pooledObjects.ContainsKey (NodeId) && pooledObjects [NodeId].Count > 0) {
				item = pooledObjects [NodeId].Dequeue ();
				SetTransform (item.transform, transform);
			} else {
				// Pool is exhausted for this object
				item = BuildGameObjectFromArray(gameObjectArrayProp, index, transform);
			}

			item.isStatic = gameObjectArrayProp.IsStaticObject;
			if (gameObjectArrayProp.IsStaticObject)
			{
				RecursivelySetStatic(item.transform);
			}

			return item;
		}


        void RecursivelySetStatic(Transform trans)
        {
            var obj = trans.gameObject;
            obj.isStatic = true;
            for (int i = 0; i < trans.childCount; i++)
            {
                var child = trans.GetChild(i);
                RecursivelySetStatic(child);
            }
        }

	}
}
