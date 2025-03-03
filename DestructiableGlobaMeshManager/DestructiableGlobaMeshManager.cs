using UnityEngine;
using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using UnityEngine.Events;


public class DestructiableGlobaMeshManager : MonoBehaviour
{
    public RayGun raygun;
    public DestructibleGlobalMeshSpawner meshSpawner;
    private List<GameObject> segements = new List<GameObject>();
    private DestructibleMeshComponent currentComponent;//to compare the reverseSegement in DestroyMeshSegements
    
    void Start()
    {
        raygun.OnShootAndHit.AddListener(DestroyMeshSegements);
        meshSpawner.OnDestructibleMeshCreated.AddListener(SetupDestructiableComponent);//to call function everytime "OnDestructibleMeshCreated" is done
    }

    public void SetupDestructiableComponent(DestructibleMeshComponent component)
    {
        
        currentComponent = component;
        //initial and add segements to component, these segements are ones created in DestructibleMesh in playmode
        component.GetDestructibleMeshSegments(segements);
        //since every segements are created without collider, so then add it
        foreach (var item in segements)
        {
            item.AddComponent<MeshCollider>();
        }
    }
    public void DestroyMeshSegements(GameObject segement) {
        //validate the segement exsistance and compare the segment with Reserve space
        if (segements.Contains(segement) && segement != currentComponent.ReservedSegment) {
            currentComponent.DestroySegment(segement);
        }  
    }
}

