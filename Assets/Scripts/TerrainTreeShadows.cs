using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class TerrainTreeShadows : MonoBehaviour {

    public Terrain terrain;
    public Light sun;
    public Vector3 sphereCenterOffset;
    public float sphereRadius;
    
    
    private ComputeBuffer spheresBuffer;
    private Material blitMaterial;
    private CommandBuffer cmd;
    
    private static readonly int shaderSpheres = Shader.PropertyToID("_Spheres");
    private static readonly int inverseView = Shader.PropertyToID("_InverseView");
    private static readonly int sunDirection = Shader.PropertyToID("_SunDirection");

    private struct Sphere {
        public Vector3 center;
        public float sqrRadius;
    }
    
    private void OnEnable() {
        var treeCount = terrain.terrainData.treeInstances.Length;
        var sizeSphere = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Sphere));
        spheresBuffer = new ComputeBuffer(treeCount, sizeSphere, ComputeBufferType.Default);
        
        var spheres = new Sphere[treeCount];
        
        for (var i = 0; i < terrain.terrainData.treeInstances.Length; i++) {
            var tree = terrain.terrainData.treeInstances[i];
            var position = Vector3.Scale(tree.position, terrain.terrainData.size);
            position = terrain.transform.TransformPoint(position);
            spheres[i].center = position + sphereCenterOffset;
            spheres[i].sqrRadius = sphereRadius * sphereRadius;
        }
        
        spheresBuffer.SetData(spheres);

        blitMaterial = new Material(Shader.Find("Hidden/TerrainTreeShadows"));
        blitMaterial.SetBuffer(shaderSpheres, spheresBuffer);
        
        cmd = new CommandBuffer {
            name = "Terrain Tree Shadow"
        };
        cmd.Blit(BuiltinRenderTextureType.Depth, BuiltinRenderTextureType.CurrentActive, blitMaterial); // CurrentActive should be screen space shadow map
        sun.AddCommandBuffer(LightEvent.AfterScreenspaceMask, cmd);
    }

    private void OnDisable() {
        sun.RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, cmd);
        cmd.Dispose();
        spheresBuffer.Dispose();
    }

    private void OnPreRender() {
        blitMaterial.SetMatrix(inverseView, transform.localToWorldMatrix);
        blitMaterial.SetVector(sunDirection, -sun.transform.forward);
    }
}
