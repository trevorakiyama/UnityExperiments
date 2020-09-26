using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

// based on the Tutorial from CodeMonkey
// Creating a pref using GameObjectConversionSystem
public class PrefabEntitiesV2 : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    //public static Entity[] preFabEntities = new Entity[2];

    public static Entity preFabEntity;


    public GameObject prefabGameObject0;
    public GameObject prefabGameObject1;


    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        //PrefabEntitiesV2.preFabEntities[0] = conversionSystem.GetPrimaryEntity(prefabGameObject0);
        //PrefabEntitiesV2.preFabEntities[1] = conversionSystem.GetPrimaryEntity(prefabGameObject1);


        preFabEntity = conversionSystem.GetPrimaryEntity(prefabGameObject0);
    }


    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(prefabGameObject0);
        //referencedPrefabs.Add(prefabGameObject1);
    }
}
