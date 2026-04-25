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
        _albedo ("albedo", 2D) = "white" {} 
        [HDR]_tint ("tint", Color) = (1,1,1, 1)
        
        
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
            float3 _tint;
            sampler2D  _albedo;
            
            // === sdf 2D ===
            
            /*float CapsuleSdf2D(float2 p,float2 a, float2 b, float radius)
            {
                //https://iquilezles.org/articles/distfunctions2d/ (segment un peu pimp avec les 2 radius à la fin)
                float2 pa = p-a, ba = b-a;
                float h = clamp( dot(pa,ba)/dot(ba,ba), 0.0, 1.0 );
                return length( pa - ba*h ) - radius;//- lerp(radiusB,radiusA,h);
            }

            float2 TreeSpacePointToScreenPoint(float3 positionTs, matrix treeToClip)
            {
                float aspectRatio = _ScreenSize.y*_ScreenSize.z; //  height/width
                float3 positionCs = mul(treeToClip, float4(positionTs,1));
                return positionCs.xy/positionCs.z * float2(1,aspectRatio);
            }

            float TreeSpaceRadiusToScreenSpaceRadius(float3 PosTs,float3 cameraPosWs, float radiusTs)
            {
                float radiusWs = radiusTs; //todo : scaling
                float3 PosWs = mul(_treeTransform_ls_to_ws,float4(PosTs,1)); //world space

                float distanceToCamera = distance (PosWs, cameraPosWs);

                return length(radiusWs/distanceToCamera);
            }
            
            //retourne la distance signée avec l'ensemble des branches de l'arbre, en 2D
            float SceneSDF_2D(float3 positionWs)
            {
                float aspectRatio = _ScreenSize.y*_ScreenSize.z; //  height/width
                matrix worldToScreen = mul( unity_CameraProjection,  unity_WorldToCamera);
                matrix treeToScreen = mul( worldToScreen, _treeTransform_ls_to_ws);
                float3 posCs = mul(worldToScreen, float4(positionWs,1));
                float2 screenPos = posCs.xy/posCs.z * float2(1,aspectRatio);
                float3 cameraPosWs = mul(unity_CameraToWorld,float4(0,0,0,1));
                
                float distance = 100000000;
                for (int i = 0; i<_segmentCount;i++)
                {
                    float2 a2D = TreeSpacePointToScreenPoint(_segments_ls[i].a,treeToScreen);
                    float2 b2D = TreeSpacePointToScreenPoint(_segments_ls[i].b,treeToScreen);
                    float r2D = TreeSpaceRadiusToScreenSpaceRadius(_segments_ls[i].a,cameraPosWs,_segments_ls[i].radius);
                    //float smoothing2D = TreeSpaceRadiusToScreenSpaceRadius(_smoothing);
                    float sdf = CapsuleSdf2D(screenPos,a2D,b2D,r2D);
                    distance = min(sdf,distance);
                    //distance = smooth_min(distance,sdf,smoothing2D*_segments_ls[i].radius);
                }
                
                return distance;
            }*/

            

            // === sdf 3D ===
            
            //retourne la distance signée avec un segment épaissis; une capsule
            //https://iquilezles.org/articles/distfunctions/
            SdfResult SegmentSDF(float3 localPos,Segment segment)
            {
                SdfResult output;
                
                //H = le point M (local pos) projeté sur le segment AB.
                // on retourne la distance entre H et M - le rayon de la capsule.
                float3 AM = localPos - segment.a;
                float3 AB = segment.b - segment.a;
                
                output.t =  saturate(dot(AB,AM)/ dot(AB,AB)); // t : la longueur normalisée de la projection de M sur le segment AB 
                output.h  = segment.a + AB * (output.t);

                output.sdf =  length(localPos - output.h) - segment.radius;//todo : lerp(radiusA, radiusB, t)
                return output;
            }
            
            //retourne la distance signée avec l'ensemble des branches de l'arbre
            SceneHit SceneSDF(float3 localPos, float minBranchRadius = 0)
            {
                //todo : octree ou binary space partitionning pour éviter d'itérer à travers tous les segments.
                //todo : interpolation d'attributs entre les 2 segments les plus proches
                
                SceneHit hit;
                hit.distance = 1000000;
                hit.segID = 0;
                for (int i = 0; i<_segmentCount && hit.distance>_threshold && _segments_ls[i].radius>minBranchRadius;i++)
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
                float4 posWs  : TEXCOORD1;
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
                OUT.posLs =  mul(Inverse(_treeTransform_ls_to_ws), OUT.posWs);
                OUT.normalWs = TransformObjectToWorldNormal(vertex.normalOs);
                    
                // Returning the output. 
                return OUT;
            }


            float4 ShadeTree(float3 normalWs, float3 rayDirectionWs, float2 uv)
            {
                //light
                float lambert = saturate(dot(normalWs,_MainLightPosition));
                float specular = pow(saturate(dot(reflect(_MainLightPosition, normalWs), rayDirectionWs)), 2);
                float fresnel = pow(saturate(dot(reflect(rayDirectionWs, normalWs), rayDirectionWs)), 1.4);
                float3 light = lerp(unity_AmbientSky *1.5 ,_MainLightColor,lambert) + specular * (_MainLightColor)*.1 + fresnel * unity_AmbientSky;

                //color
                float3 color = _tint * tex2D(_albedo,uv);
                
                return float4(color * light,1);
            }
            
            // fragment shader
            fragOutput frag(V2f IN) 
            {
                fragOutput output;

                // === pixel culling ===
                
                //on clip les backfaces ou les front faces selon si la cam
                //est dans la bounding box pour eviter de dessiner l'arbre deux fois à chaque fois. -> +5fps
                float3 localCameraPos = mul(Inverse(_treeTransform_ls_to_ws),float4(_WorldSpaceCameraPos,1));
                bool cameraIsInsideBoundingBox = is_in_bounding_box(localCameraPos,_boundingBoxMin_ls-.1,_boundingBoxMax_ls+.1);
                bool backface = dot(IN.normalWs,GetWorldSpaceNormalizeViewDir(IN.posWs.xyz))<0;
                clip(!cameraIsInsideBoundingBox ^ backface ? 1 : -1);

                //on fait une premiere etape de raymarching en 2D, screenspace pour clip tous les pixels de la bb qui ne toucheront aucune branche. -> -5fps
                //clip(-SceneSDF_2D(IN.posWs)+.01);

                
                /// === preparation raymarching ===
                
                //on determine le LOD
                float DepthBasedQualityLevel = 1.0-saturate(
                    distance(_WorldSpaceCameraPos.xyz,mul(unity_ObjectToWorld,float4(0,0,0,1)).xyz)
                    * 1/200//_ProjectionParams.w
                    );//normalized distance to camera
                DepthBasedQualityLevel *= DepthBasedQualityLevel*DepthBasedQualityLevel;
                DepthBasedQualityLevel *= DepthBasedQualityLevel*DepthBasedQualityLevel;
                float branchClippingRadiusThreshold = min(.1-DepthBasedQualityLevel,0.1*_segments_ls[0].radius);
                
                //definition du rayon sur lequel on va se déplacer
                float3 localRayOrigin = cameraIsInsideBoundingBox ? localCameraPos : IN.posLs;
                const float3 localRayDirection = mul((float3x3)Inverse(_treeTransform_ls_to_ws),-GetWorldSpaceNormalizeViewDir(IN.posWs.xyz).xyz);// normalize(IN.worldPos.xyz- _WorldSpaceCameraPos.xyz );
                const float maxRayLength = ComputeMaxRayLengthInBoundingBox(localRayOrigin,localRayDirection,_boundingBoxMin_ls ,_boundingBoxMax_ls);
                float rayLength = 0;

                
                // === raymarching ===
                
                //on avance le long d'un rayon jusqu'à ce que la distance avec la scène soit quasi nulle.
                //https://iquilezles.org/articles/raymarchingdf/
                bool hitAnySegment = false;
                SceneHit sceneHit;
                float3 samplePoint;
                for (int i =0; i<_maxIterations;i++)
                {
                    samplePoint = localRayOrigin+localRayDirection*rayLength;
                    sceneHit = SceneSDF(samplePoint,branchClippingRadiusThreshold);

                    //distance quasi nulle <=> surface touchée
                    if (sceneHit.distance<=_threshold)
                    {
                        hitAnySegment = true;
                        break;
                    }
                    
                    rayLength += sceneHit.distance+_threshold;
                    clip((maxRayLength-rayLength));
                }
                clip(hitAnySegment-.5f);
                
                // === shading du pixel ===
                
                //compute normal
                SdfResult closestHit =  SegmentSDF(samplePoint,_segments_ls[sceneHit.segID]);
                SdfResult SecondClosestHit = SegmentSDF(samplePoint,_segments_ls[sceneHit.secondClosestSegID]);
                float3 normal = normalize(samplePoint-lerp(closestHit.h,SecondClosestHit.h,sceneHit.smoothFactor));

                //compute UV
                float2 uv = float2(0,0);
                uv.y = -lerp(closestHit.t,1-SecondClosestHit.t,sceneHit.smoothFactor);
                 
                
                //lighting
                output.color = ShadeTree(
                    mul((float3x3)_treeTransform_ls_to_ws,normal),
                    mul((float3x3)_treeTransform_ls_to_ws,localRayDirection),
                    uv);
                
                //write to depth
                float4 linearDepth = TransformWorldToHClip(mul(_treeTransform_ls_to_ws,float4( samplePoint,1)));
                float depth = linearDepth.z / linearDepth.w;
                output.depth = depth;
                
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
