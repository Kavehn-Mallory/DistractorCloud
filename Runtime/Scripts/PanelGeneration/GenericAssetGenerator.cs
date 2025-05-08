using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace DistractorClouds.PanelGeneration
{
    public abstract class GenericAssetGenerator<T> where T : MonoBehaviour
    {

        public int DefaultCapacity = 1000;
        public int MaxCapacity = 10000;
        public bool CollectionCheck = true;

        public int DistractorLayer = 3;

        public abstract Transform ParentTransform { get; }
        public abstract T Prefab { get; }

        public ObjectPool<T> ObjectPool
        {
            get
            {
                if (_objectPool == null)
                {
                    _objectPool = new ObjectPool<T>(CreateObject, OnGetObject, OnReleaseObject, OnDestroyObject,
                        CollectionCheck, DefaultCapacity, MaxCapacity);
                }

                return _objectPool;
            }
        }

        public List<GroupObject<T>> ActiveObjects = new();

        private ObjectPool<T> _objectPool;


        public virtual void SpawnObjectsForPath(AssetSpawnPoint[] spawnPoints, int startGroup = 0, int endGroup = -1)
        {
            Reset();
            foreach (var spawnPoint in spawnPoints)
            {
                if (IsInGroup(spawnPoint.Group, startGroup, endGroup))
                {
                    var spawnedObject = ObjectPool.Get();
                    spawnedObject.transform.SetPositionAndRotation(spawnPoint.Position, Quaternion.identity);
                    ActiveObjects.Add(new GroupObject<T>
                    {
                        ActiveObject = spawnedObject,
                        Group = spawnPoint.Group
                    });
                }
            }
            ActiveObjects.Sort(ComparePointCloudObjectsByGroup);
        }
        
        public static int ComparePointCloudObjectsByGroup(GroupObject<T> x,
            GroupObject<T> y)
        {
            return x.Group.CompareTo(y.Group);
        }

        public virtual void Reset()
        {
            foreach (var activeObject in ActiveObjects)
            {
                ObjectPool.Release(activeObject.ActiveObject);
            }
            ActiveObjects.Clear();
        }

        private bool IsInGroup(int spawnPointGroup, int startGroup, int endGroup)
        {
            return spawnPointGroup >= startGroup && (spawnPointGroup <= endGroup || endGroup == -1);
        }


        public abstract void OnDestroyObject(T obj);

        public abstract void OnReleaseObject(T obj);

        public abstract void OnGetObject(T obj);

        public virtual T CreateObject()
        {
            var instance = Object.Instantiate(Prefab, ParentTransform, true);
            instance.gameObject.layer = DistractorLayer;
            return instance;
        }

        public virtual void Dispose()
        {
            _objectPool?.Dispose();
        }
        
        
    }

    public struct GroupObject<T> where T : MonoBehaviour
    {
        public T ActiveObject;
        public int Group;
    }
    
    
}