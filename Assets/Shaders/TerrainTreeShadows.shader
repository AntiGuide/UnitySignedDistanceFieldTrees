Shader "Hidden/TerrainTreeShadows"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always
        Blend DstColor Zero
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5
            #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
            #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/Colors.hlsl"
            
            float4x4 unity_CameraInvProjection;
            
            TEXTURE2D_SAMPLER2D(_LeafTexture, sampler_LeafTexture);
            TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);
            float3    _SunDirection;
            float4x4  _InverseView;
            half4 _ShadowColor;
            float _FadeStart;
            float _FadeDistance;
            float _LeafTextureScale;
            float _LeafTextureSoftness;
            
            struct Ray
            {
                float3 origin;
                float3 direction;
            };
            
            struct Sphere
            {
                float3 center;
                float  sqrRadius;
            };
            
            float IntersectSphere(Ray r, Sphere s)
            {
                float d = dot(r.direction, r.origin - s.center);
                float3 c;
                if(d < 0){
                    c = r.origin - s.center;
                } else {
                    c = cross(r.direction, r.origin - s.center);
                }
                
                float  sqrMagnitude = dot(c,c);
                return saturate((s.sqrRadius - sqrMagnitude) / s.sqrRadius);
            }
            
            StructuredBuffer<Sphere> _Spheres;
            
            float RaytraceScene(Ray r)
            {
                uint size, stride;
                _Spheres.GetDimensions(size, stride);
                float returnValue = 0;
                for(uint i = 0; i < size; ++i)
                {
                    returnValue = max(returnValue, IntersectSphere(r, _Spheres[i]));
                }
                
                float leaf = SAMPLE_TEXTURE2D(_LeafTexture, sampler_LeafTexture, r.origin.xz * _LeafTextureScale);
                
                return returnValue > leaf;
            }
            
            struct Input
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 rayDirection : TEXCOORD1;
            };
            
            // Vertex shader that procedurally outputs a full screen triangle
            Varyings Vert(uint vertexID : SV_VertexID)
            {
                // Render settings
                float far = _ProjectionParams.z;
                float2 orthoSize = unity_OrthoParams.xy;
                float isOrtho = unity_OrthoParams.w; // 0: perspective, 1: orthographic
        
                // Vertex ID -> clip space vertex position
                float x = (vertexID != 1) ? -1 : 3;
                float y = (vertexID == 2) ? -3 : 1;
                float3 vpos = float3(x, y, 1.0);
        
                // Perspective: view space vertex position of the far plane
                float3 rayPers = mul(unity_CameraInvProjection, vpos.xyzz * far).xyz;
        
                // Orthographic: view space vertex position
                float3 rayOrtho = float3(orthoSize * vpos.xy, 0);
        
                Varyings o;
                o.vertex = float4(vpos.x, -vpos.y, 1, 1);
                o.uv = (vpos.xy + 1) / 2;
                o.rayDirection = lerp(rayPers, rayOrtho, isOrtho);
                return o;
            }
            
            float3 ComputeViewSpacePosition(Varyings input)
            {
                // Render settings
                float near = _ProjectionParams.y;
                float far = _ProjectionParams.z;
                float isOrtho = unity_OrthoParams.w; // 0: perspective, 1: orthographic
        
                // Z buffer sample
                float z = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, input.uv);
        
                // Far plane exclusion
                #if !defined(EXCLUDE_FAR_PLANE)
                    float mask = 1;
                #elif defined(UNITY_REVERSED_Z)
                    float mask = z > 0;
                #else
                    float mask = z < 1;
                #endif
        
                // Perspective: view space position = ray * depth
                float3 vposPers = input.rayDirection * Linear01Depth(z);
        
                // Orthographic: linear depth (with reverse-Z support)
                #if defined(UNITY_REVERSED_Z)
                    float depthOrtho = -lerp(far, near, z);
                #else
                    float depthOrtho = -lerp(near, far, z);
                #endif
        
                // Orthographic: view space position
                float3 vposOrtho = float3(input.rayDirection.xy, depthOrtho);
        
                // Result: view space position
                return lerp(vposPers, vposOrtho, isOrtho) * mask;
            }

            half4 Frag (Varyings i) : SV_Target
            {
                
                float3 vpos = ComputeViewSpacePosition(i);
                float fade = saturate((abs(vpos.z) - _FadeStart) / _FadeDistance);
                if(fade <= 0.0){
                    return half4(1,1,1,1);
                }
                
                if(abs(vpos.z) > _ProjectionParams.z * 0.99){
                    return half4(1,1,1,1);
                }
                
                float3 wpos = mul(_InverseView, float4(vpos, 1)).xyz;
                Ray r;
                r.origin = wpos;
                r.direction = normalize(_SunDirection);
                float shadow = RaytraceScene(r);
                
                return lerp(half4(1,1,1,1), _ShadowColor, shadow * fade);
            }
            ENDHLSL
        }
    }
}