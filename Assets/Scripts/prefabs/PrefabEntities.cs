using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;


// based on the Tutorial from CodeMonkey
// Creating a pref using BlobAssetStore
public class PrefabEntities : MonoBehaviour, IConvertGameObjectToEntity
{
    public static Entity prefabEntity;
    public GameObject prefabGameObject;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        using (BlobAssetStore blobAssetStore = new BlobAssetStore())
        {
            Entity prefabEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefabGameObject,
                GameObjectConversionSettings.FromWorld(dstManager.World, blobAssetStore));
            PrefabEntities.prefabEntity = prefabEntity;
        }
    }
}
