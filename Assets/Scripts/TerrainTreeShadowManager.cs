using System;
using UnityEngine;

public class TerrainTreeShadowManager : MonoBehaviour {
    public static TerrainTreeShadowManager Instance;
    
    public Terrain terrain;
    public Light sun;
    public Vector3 sphereCenterOffset;
    public float sphereRadius;
    public Sphere[] spheres;

    public struct Sphere {
        public Vector3 center;
        public float sqrRadius;
    }
    
    private void OnEnable() {
        Instance = this;
        
        var treeCount = terrain.terrainData.treeInstances.Length;
        var sizeSphere = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Sphere));
        
        spheres = new Sphere[treeCount];
        
        for (var i = 0; i < terrain.terrainData.treeInstances.Length; i++) {
            var tree = terrain.terrainData.treeInstances[i];
            var position = Vector3.Scale(tree.position, terrain.terrainData.size);
            position = terrain.transform.TransformPoint(position);
            spheres[i].center = position + sphereCenterOffset;
            spheres[i].sqrRadius = sphereRadius * sphereRadius;
        }
    }

    private void OnDisable() {
        spheres = null;
        Instance = null;
    }
}
