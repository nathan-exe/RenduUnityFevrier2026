Shader "Vegetation/RaymarchedTree"
{
    // The Properties block of the Unity shader.
    Properties 
    { 
         [HideInInspector] _boundingBoxMin_ls ("_boundingBoxMin_ls", Vector) = (0, 0,0, 1)
         [HideInInspector] _boundingBoxMax_ls  ("_boundingBoxMax_ls", Vector) = (0, 0, 0, 1)

        //raymarching
        _threshold ("Raymarch hit threshold", Float) = .1
        _maxIterations ("max Iterations", Int) = 10
        _smoothing ("smoothing", Float) = 1
        
        //material
        [HDR]_albedo ("albedo", Color) = (1,1,1, 1)
        
    }

    // The SubShader block containing the Shader code.
    SubShader
    {
        // SubShader Tags define when and under which conditions a SubShader block or
        // a pass is executed.
        ZWrite On 
        ZTest LEqual
        Cull Off
        Tags {  "RenderType" = "Opaque" "Queue" = "Geometry"  "RenderPipeline" = "UniversalPipeline" }
        
        Pass
        {
            // The HLSL code block. Unity SRP uses the HLSL language.
            HLSLPROGRAM
            // This line defines the name of the vertex shader.
            #pragma vertex vertex
            // This line defines the name of the fragment shader.
            #pragma fragment frag
            
            // hlsl includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "ShaderHelpers.hlsl"
            #include "Raymarching.hlsl"
            
            //bounding box
            float4 _boundingBoxMin_ls;
            float4 _boundingBoxMax_ls;

            //raymarching
            float _threshold;
            int _maxIterations;
            float _smoothing;

            //scene
            StructuredBuffer<Segment> _segments_ls; //todo : binary space partitionning 
            int _segmentCount;
            matrix _treeTransform_ls_to_ws;

            //material
            float3 _albedo;
            
            //retourne la distance signée avec un segment épaissis; une capsule
            //https://iquilezles.org/articles/distfunctions/
            SdfResult SegmentSDF(float3 localPos,Segment segment)
            {
                SdfResult output;
                
                //H = le point M (local pos) projeté sur le segment AB.
                // on retourne la distance entre H et M - le rayon de la capsule.
                float3 A = float4(segment.a,1);
                float3 B = float4(segment.b,1);
                float3 AM = localPos - A;
                float3 AB = B - A;
                float normeAB = length(AB);
                
                float projectionLength = dot(AB,AM)/(normeAB * normeAB);
                projectionLength = saturate(projectionLength);
                float3 H = A + AB*(projectionLength);

                output.t = projectionLength;
                output.h = H;
                output.sdf =  length(localPos - H) - segment.radius;//todo : lerp(radiusA, radiusB, t)
                return output;
            }

            //retourne la distance signée avec l'ensemble des branches de l'arbre
            SceneHit SceneSDF(float3 localPos)
            {
                //todo : octree ou binary space partitionning pour éviter d'itérer à travers tous les segments.
                //todo : interpolation d'attributs entre les 2 segments les plus proches
                
                SceneHit hit;
                hit.distance = 1000000;
                hit.segID = 0;
                for (int i = 0; i<_segmentCount;i++)
                {
                    SdfResult result = SegmentSDF(localPos,_segments_ls[i]);
                    float oldDistance = hit.distance;
                    
                    
                    hit.distance = smooth_min(hit.distance,result.sdf,_smoothing*_segments_ls[i].radius);
                    if (oldDistance > result.sdf)
                    {
                        hit.smoothFactor =  abs(hit.distance-result.sdf)/(_smoothing*_segments_ls[i].radius);
                        hit.secondClosestSegID = hit.segID;
                        hit.segID = i;
                    }
                }
                
                return hit;
            }
            
            
            //== shader functions ==

            struct VertexAttributes
            {
                //vertex position in object space
                float4 positionOs   : POSITION;
                float4 normalOs   : NORMAL;
            };

            struct V2f
            {
                float4 positionCS  : SV_POSITION;
                float3 posLs  : TEXCOORD0;
                float3 posWs  : TEXCOORD1;
                float3 normalWs  : TEXCOORD2;
            };
            
            struct fragOutput
            {
                half4 color : SV_Target;
                float depth : SV_Depth;
            };

            //vertex shader
            V2f vertex(VertexAttributes vertex)
            {
                V2f OUT; 

                OUT.positionCS = TransformObjectToHClip(vertex.positionOs.xyz);
                OUT.posWs = mul(unity_ObjectToWorld,vertex.positionOs);
                OUT.posLs =  mul(Inverse(_treeTransform_ls_to_ws),float4( OUT.posWs,1));
                OUT.normalWs = TransformObjectToWorldNormal(vertex.normalOs);
                    
                // Returning the output. 
                return OUT;
            }
            
            // fragment shader
            fragOutput frag(V2f IN) 
            {
                fragOutput output;
                
                //on clip les backfaces ou les front faces selon si la cam
                //est dans la bounding box pour eviter de dessiner l'arbre deux fois à chaque fois.
                float3 localCameraPos = mul(Inverse(_treeTransform_ls_to_ws),float4(_WorldSpaceCameraPos,1));
                bool cameraIsInsideBoundingBox = is_in_bounding_box(localCameraPos,_boundingBoxMin_ls-.1,_boundingBoxMax_ls+.1);
                bool backface = dot(IN.normalWs,GetWorldSpaceNormalizeViewDir(IN.posWs.xyz))<0;
                clip(!cameraIsInsideBoundingBox ^ backface ? 1 : -1);

                //definition du rayon sur lequel on va se déplacer
                float3 localRayOrigin = cameraIsInsideBoundingBox ? localCameraPos : IN.posLs;
                const float3 localRayDirection = mul(Inverse(_treeTransform_ls_to_ws),-GetWorldSpaceNormalizeViewDir(IN.posWs.xyz));// normalize(IN.worldPos.xyz- _WorldSpaceCameraPos.xyz );
                const float maxRayLength = ComputeMaxRayLengthInBoundingBox(localRayOrigin,localRayDirection,_boundingBoxMin_ls ,_boundingBoxMax_ls);
                float rayLength = 0;
                
                //raymarching : on avance le long d'un rayon jusqu'à ce que la distance avec la scène soit quasi nulle.
                //https://iquilezles.org/articles/raymarchingdf/
                for (int i =0; i<_maxIterations;i++)
                {
                    float3 samplePoint = localRayOrigin+localRayDirection*rayLength;
                    SceneHit sceneHit = SceneSDF(samplePoint);

                    //distance quasi nulle <=> surface touchée
                    if (sceneHit.distance<=_threshold)
                    {
                        //samplePoint = rayOrigin+rayDirection*rayLength;

                        //compute normal
                        Segment hitSegment = _segments_ls[sceneHit.segID];
                        SdfResult h =  SegmentSDF(samplePoint,hitSegment);
                        SdfResult h2 =  SegmentSDF(samplePoint,_segments_ls[sceneHit.secondClosestSegID]);
                        float3 normal = normalize(samplePoint-lerp(h.h,h2.h,sceneHit.smoothFactor));
                        
                        //lightning
                        float lambert = saturate(dot(normal,_MainLightPosition));
                        float specular = pow(saturate(dot(reflect(_MainLightPosition, normal), localRayDirection)), 2);
                        float fresnel = pow(saturate(dot(reflect(localRayDirection, normal), localRayDirection)), 1.4);
                        float3 light = lerp(unity_AmbientSky *1.5 ,_MainLightColor,lambert) + specular * (_MainLightColor)*.1 + fresnel * unity_AmbientSky;
                        float3 color = _albedo * light;
                        
                        //write to depth
                        float4 linearDepth = TransformWorldToHClip(mul(_treeTransform_ls_to_ws,float4( samplePoint,1)));
                        float depth = linearDepth.z / linearDepth.w;
                        output.depth = depth;
                        output.color = float4(color,.4);
                        return output;
                        
                        //return lerp( float4(1,0,0,1) , float4(color,1),_segments[sceneHit.segID].age);
                    }
                    
                    rayLength += sceneHit.distance;
                    clip((rayLength < maxRayLength)-.5);
                }

                //nombre max de steps dépassé
                clip(-1);
                output.color = float4(1,0,0,1);
                output.depth = 1;
                return output;
            }
            ENDHLSL
        }

//shadow pass
//Pass
//        {
//            Tags {"LightMode"="ShadowCaster" }
//            
//            // The HLSL code block. Unity SRP uses the HLSL language.
//            HLSLPROGRAM
//            // This line defines the name of the vertex shader.
//            #pragma vertex vertex
//            // This line defines the name of the fragment shader.
//            #pragma fragment frag
//            
//            // hlsl includes
//            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
//            #include "ShaderHelpers.hlsl"
//
//            // un segment défini par deux points et un rayon.
//            struct Segment
//            {
//                half3 a,b;
//                half radius;
//                half age;
//            };
//            
//            struct VertexAttributes
//            {
//                //vertex position in object space
//                float4 positionOs   : POSITION;
//            };
//
//            struct V2f
//            {
//                float4 positionCS  : SV_POSITION;
//                float4 worldPos  : TEXCOORD0;
//            };
//
//            //bounding box
//            float3 _boundingBoxMin_ls;
//            float3 _boundingBoxMax_ls;
//
//            //raymarching
//            float _threshold;
//            int _maxIterations;
//            float _smoothing;
//
//            //scene
//            StructuredBuffer<Segment> _segments_ls; //todo : binary space partitionning 
//            int _segmentCount;
//            matrix _treeTransform_ls_to_ws;
//            
//            float ComputeMaxRayLengthInBoundingBox(float3 origin,float3 direction,float3 boxMin, float3 boxMax){
//                float3 T_1, T_2; // vectors to hold the T-values for every direction
//                float t_near = -Max_float(); 
//                float t_far = Max_float();
//                
//                for (int i = 0; i < 3; i++)
//                { //we test slabs in every direction
//                    if (direction[i] == 0)
//                    { // ray parallel to planes in this direction
//                        if ((origin[i] < boxMin[i]) || (origin[i] > boxMax[i]))
//                        {
//                            return false; // parallel AND outside box : no intersection possible
//                        }
//                    }
//                    else
//                    { // ray not parallel to planes in this direction
//                        T_1[i] = (boxMin[i] - origin[i]) / direction[i];
//                        T_2[i] = (boxMax[i] - origin[i]) / direction[i];
//
//                        if(T_1[i] > T_2[i]){ // we want T_1 to hold values for intersection with near plane
//                            float temp = T_1[i];
//                            T_1[i] = T_2[i];
//                            T_2[i] = temp;
//                        }
//                        if (T_1[i] > t_near){
//                            t_near = T_1[i];
//                        }
//                        if (T_2[i] < t_far){
//                            t_far = T_2[i];
//                        }
//                        if( (t_near > t_far) || (t_far < 0) ){
//                            return false;
//                        }
//                    }
//                }
//               
//                return t_far; // if we made it here, there was an intersection - YAY
//
//
//            }
//            
//            bool is_in_bounding_box(float3 pos,float3 boxMin, float3 boxMax){ 
//                return 
//                pos.x <= boxMax.x && pos.x >= boxMin.x 
//                && pos.y <= boxMax.y && pos.y >= boxMin.y 
//                && pos.z <= boxMax.z && pos.z >= boxMin.z;
//            }
//
//            struct SdfResult
//            {
//                float sdf;//la distance signée avec le segment
//                float3 t;//distance AH
//                float3 h;//le point projeté sur le centre du segment
//            };
//
//            //retourne la distance signée avec un segment épaissis; une capsule
//            //https://iquilezles.org/articles/distfunctions/
//            SdfResult SegmentSDF(float3 pos,Segment segment)
//            {
//                SdfResult output;
//                
//                //H = le point M (world pos) projeté sur le segment AB.
//                // on retourne la distance entre H et M - le rayon de la capsule.
//                float3 A = segment.a;
//                float3 B = segment.b;
//                float3 AM = pos - A;
//                float3 AB = B - A;
//                float normeAB = length(AB);
//                //float3 normalizedAB = normalize(ab);
//                //float3 h = segment.a + (dot(normalizedAB,am) * normalizedAB) - worldpos;
//
//                float projectionLength = dot(AB,AM)/(normeAB * normeAB);
//                projectionLength = saturate(projectionLength);
//                float3 H = A + AB*(projectionLength);
//
//                output.t = projectionLength;
//                output.h = H;
//                output.sdf =  length(pos - H) - segment.radius;//todo : lerp(radiusA, radiusB, t)
//                return output;
//            }
//
//            struct SceneHit
//            {
//                float distance;
//                int segID;
//                int secondClosestSegID;
//                float smoothFactor;
//            };
//
//            //retourne la distance signée avec l'ensemble des branches de l'arbre
//            SceneHit SceneSDF(float3 pos)
//            {
//                //todo : octree ou binary space partitionning pour éviter d'itérer à travers tous les segments.
//                //todo : interpolation d'attributs entre les 2 segments les plus proches
//                
//                SceneHit hit;
//                hit.distance = 1000000;
//                hit.segID = 0;
//                for (int i = 0; i<_segmentCount;i++)
//                {
//                    if (_segments_ls[i].radius<0.02) continue;
//                    
//                    SdfResult result = SegmentSDF(pos,_segments_ls[i]);
//                    float oldDistance = hit.distance;
//                    
//                    
//                    hit.distance = min(hit.distance,result.sdf);
//                    if (oldDistance > result.sdf)
//                    {
//                        hit.segID = i;
//                    }
//                }
//                
//                return hit;
//            }
//            
//            
//            //== shader functions ==
//
//            //vertex shader
//            V2f vertex(VertexAttributes IN)
//            {
//                V2f OUT; 
//
//                OUT.positionCS = TransformObjectToHClip(IN.positionOs.xyz);
//                OUT.worldPos = mul(unity_ObjectToWorld,IN.positionOs);
//                    
//                // Returning the output. 
//                return OUT;
//            }
//
//            struct fragOutput
//            {
//                float depth : SV_Depth;
//            };
//            
//            // fragment shader
//            fragOutput frag(V2f IN) 
//            {
//                fragOutput output;
//                
//                //define bounding box
//                float3 bbSize_ls = (_boundingBoxMax_ls - _boundingBoxMin_ls);
//                float3 bbInvSize_ls = 1.0/bbSize_ls;
//                
//                //define ray
//                
//                
//                const float3 rayDirection = normalize(_MainLightPosition.xyz);
//                float3 rayOrigin = IN.worldPos.xyz-rayDirection*(length(bbSize_ls));
//                const float maxRayLength = ComputeMaxRayLengthInBoundingBox(rayOrigin,rayDirection,_boundingBoxMin_ls ,_boundingBoxMax_ls);
//                
//                float rayLength = 0;
//                //raymarching : on avance le long d'un rayon jusqu'à ce que la distance avec la scène soit quasi nulle.
//                //https://iquilezles.org/articles/raymarchingdf/
//                _maxIterations = _maxIterations/2;
//                for (int i =0; i<_maxIterations;i++)
//                {
//                    float3 samplePoint = rayOrigin+rayDirection*rayLength;
//                    SceneHit sceneHit = SceneSDF(samplePoint);
//
//                    //distance quasi nulle <=> surface touchée
//                    if (sceneHit.distance<=_threshold*5)
//                    {
//                        //write to depth
//                        float4 linearDepth = TransformWorldToHClip(samplePoint);
//                        float depth = linearDepth.z / linearDepth.w;
//                        output.depth = depth;
//                        
//                        return output;
//                        
//                        //return lerp( float4(1,0,0,1) , float4(color,1),_segments[sceneHit.segID].age);
//                    }
//                    
//                    rayLength += sceneHit.distance;
//                    clip((rayLength < maxRayLength)-.1);
//                }
//
//                //nombre max de steps dépassé
//                clip(-1);
//                output.depth = 0;
//                return output;
//            }
//            ENDHLSL
//        }
    }
}
