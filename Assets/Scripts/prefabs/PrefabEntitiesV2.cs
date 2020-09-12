using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

// based on the Tutorial from CodeMonkey
// Creating a pref using GameObjectConversionSystem
public class PrefabEntitiesV2 : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public static Entity prefabEntity;
    public GameObject prefabGameObject;


    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        Entity prefabEntity = conversionSystem.GetPrimaryEntity(prefabGameObject);
        PrefabEntitiesV2.prefabEntity = prefabEntity;
    }



    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(prefabGameObject);
    }
}
