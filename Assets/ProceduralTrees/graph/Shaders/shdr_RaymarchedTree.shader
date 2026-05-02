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

                // output.t =  saturate(dot(AB,AM)/ dot(AB,AB));
                // output.h  = segment.a + AB * (output.t);
                // output.sdf =  length(localPos - output.h) - lerp(segment.radiusA, segment.RadiusB, output.t);
                // return output;
                
                output.unclampedT = dot(AB,AM)/ dot(AB,AB);
                output.unclampedH  = segment.a + AB * output.unclampedT;
                output.clampedT = saturate(output.unclampedT); // t : la longueur normalisée de la projection de M sur le segment AB 

                output.clampedH = segment.a + AB * (output.clampedT);
                output.sdf =  length(localPos - output.clampedH) - lerp(segment.radiusA, segment.RadiusB, output.clampedT);
                
                return output;
            }
            
            //retourne la distance signée avec l'ensemble des branches de l'arbre
            SceneHit SceneSDF(float3 localPos, float minBranchRadius = 0)
            {
                //todo : octree ou binary space partitionning pour éviter d'itérer à travers tous les segments.
                //todo : interpolation d'attributs entre les 2 segments les plus proches
                
                SceneHit hit;
                hit.distance = 100;
                hit.segID = 0;

                float minSdf = 100;
                float minSdf2 = 101;
                for (int i = 0; i<_segmentCount && hit.distance>_threshold && _segments_ls[i].radiusA>minBranchRadius;i++)
                {
                    SdfResult sdfSample = SegmentSDF(localPos,_segments_ls[i]);
                    
                    float smoothingRadius = _smoothing*_segments_ls[i].radiusA;
                    float smoothMinResult = smooth_min(hit.distance,sdfSample.sdf,smoothingRadius);
                    //float smoothMinResult = min(hit.distance,sdfSample.sdf);
                    hit.distance = smoothMinResult;
                    
                    if (sdfSample.sdf<minSdf && abs(minSdf - sdfSample.sdf)>.0001)
                    {
                        if (minSdf<minSdf2)
                        {
                            minSdf2 = minSdf;
                            hit.secondClosestSegID = hit.segID;
                        }
                        
                        hit.segID = i;
                        minSdf = sdfSample.sdf;
                        
                    }else if (sdfSample.sdf < minSdf2)
                    {
                        hit.secondClosestSegID = i;
                        minSdf2 = sdfSample.sdf;
                    }
                }
                hit.smoothFactor = 0;
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
                //return float4(normalWs,1);
                //light
                float lambert = saturate(dot(normalWs,_MainLightPosition));
                float specular = pow(saturate(dot(reflect(_MainLightPosition, normalWs), rayDirectionWs)), 2);
                float fresnel = pow(saturate(dot(reflect(rayDirectionWs, normalWs), rayDirectionWs)), 1.4);
                float3 light = lerp(unity_AmbientSky *1.5 ,_MainLightColor,lambert) + specular * (_MainLightColor)*.1 + fresnel * unity_AmbientSky;

                //color
                float3 color = _tint * tex2D(_albedo,uv);
                color*= light;
                return float4(color,1);
                return float4(tex2D(_albedo,uv).xyz,1);
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
                float branchClippingRadiusThreshold = min(.1-DepthBasedQualityLevel,0.1*_segments_ls[0].radiusA);
                
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
                        sceneHit = SceneSDF(samplePoint,branchClippingRadiusThreshold);
                        break;
                    }
                    
                    rayLength += sceneHit.distance;//+_threshold;
                    clip((maxRayLength-rayLength));
                }
                clip(hitAnySegment-.5f);
                
                // === shading du pixel ===
                SdfResult closestHit =  SegmentSDF(samplePoint,_segments_ls[sceneHit.segID]);
                SdfResult secondClosestHit =  SegmentSDF(samplePoint,_segments_ls[sceneHit.secondClosestSegID]);
                float t = 1.0-saturate(distance(closestHit.clampedH, secondClosestHit.clampedH)*5);
                
                //compute normal
                
                //float3 normal = normalize(mul(_treeTransform_ls_to_ws,(samplePoint-closestHit.unclampedH)));
                float3 normal = normalize(mul(_treeTransform_ls_to_ws,samplePoint-lerp(closestHit.unclampedH,secondClosestHit.unclampedH,t)));

                //compute age
                float age =  _segments_ls[sceneHit.segID].age
                    + closestHit.unclampedT*distance(_segments_ls[sceneHit.segID].a,_segments_ls[sceneHit.segID].b);
                //compute UV
                const float3 mainSegmentDir = (_segments_ls[sceneHit.segID].b-_segments_ls[sceneHit.segID].a);

                float3 referenceVector = float3(0,1,0);//normalize(_segments_ls[sceneHit.secondClosestSegID].b-_segments_ls[sceneHit.secondClosestSegID].a);
                const float3 dir = normalize(mainSegmentDir);
                referenceVector = normalize(projectOnPlane(referenceVector,dir));
                const float angle = FastAngle(normal,referenceVector);
                float2 uv = 0;
                uv.x = angle/PI/2;// * _segments_ls[sceneHit.segID].radius/_segments_ls[0].radius;
                uv.y = age;
                
                //lighting
                output.color = ShadeTree(
                    normal,
                    mul((float3x3)_treeTransform_ls_to_ws,localRayDirection),
                    uv*.5)*(age*.15+1);

                //output.color = float4(t.xxx,1);
                //output.color = float4(uv*.3,0,1);
                //output.color = float4(samplePoint*2%1,1);
                //output.color = float4(float3(sceneHit.distance,sceneHit.distance,sceneHit.distance)*10,1);
                //output.color = float4(SecondClosestHit.clampedH,1);
                
                //write to depth
                float4 linearDepth = TransformWorldToHClip(mul(_treeTransform_ls_to_ws,float4( samplePoint,1)));
                float depth = linearDepth.z / linearDepth.w;
                output.depth = depth;
                
                return output;
                
            }

            
            ENDHLSL
        }

//shadow pass
Pass
        {
            ZWrite On 
            ZTest LEqual
            Cull Back
            Tags { "LightMode"="ShadowCaster"  "RenderType" = "Opaque" "Queue" = "Geometry"  "RenderPipeline" = "UniversalPipeline" }
            
            // The HLSL code block. Unity SRP uses the HLSL language.
            HLSLPROGRAM
            // This line defines the name of the vertex shader.
            #pragma vertex vertex
            // This line defines the name of the fragment shader.
            #pragma fragment frag

            // hlsl includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Raymarching.hlsl"
            #include "ShaderHelpers.hlsl"

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

//retourne la distance signée avec un segment épaissis; une capsule
            //https://iquilezles.org/articles/distfunctions/
            SdfResult SegmentSDF(float3 localPos,Segment segment)
            {
                SdfResult output;
                
                //H = le point M (local pos) projeté sur le segment AB.
                // on retourne la distance entre H et M - le rayon de la capsule.
                float3 AM = localPos - segment.a;
                float3 AB = segment.b - segment.a;
                
                output.clampedT =  saturate(dot(AB,AM)/ dot(AB,AB)); // t : la longueur normalisée de la projection de M sur le segment AB 
                output.clampedH  = segment.a + AB * (output.clampedT);

                output.sdf =  length(localPos - output.clampedH) - lerp(segment.radiusA, segment.RadiusB, output.clampedT);
                return output;
            }
            
            //retourne la distance signée avec l'ensemble des branches de l'arbre
            float SceneSDF(float3 localPos, float minBranchRadius = 0)
            {
                //todo : octree ou binary space partitionning pour éviter d'itérer à travers tous les segments.
                //todo : interpolation d'attributs entre les 2 segments les plus proches
                float distance = 1000;
                for (int i = 0; i<_segmentCount && distance>_threshold && _segments_ls[i].radiusA>minBranchRadius;i++)
                {
                    SdfResult result = SegmentSDF(localPos,_segments_ls[i]);
                    distance = min(distance,result.sdf);
                }
                
                return distance;
            }
            
            //== shader functions ==

            struct VertexAttributes
            {
                //vertex position in object space
                float4 positionOs : POSITION;
            };

            struct V2f
            {
                float4 positionCS  : SV_POSITION;
                float3 posLs  : TEXCOORD0;
                float4 posWs  : TEXCOORD1;
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
                    
                // Returning the output. 
                return OUT;
            }
            
            // fragment shader
            float frag(V2f IN) : SV_Depth 
            {
                // === pixel culling ===
                
                //on clip les backfaces
                
                /// === preparation raymarching ===
                
                //definition du rayon sur lequel on va se déplacer
                const float3 localRayDirection = normalize(mul((float3x3)Inverse(_treeTransform_ls_to_ws),_MainLightPosition));// normalize(IN.worldPos.xyz- _WorldSpaceCameraPos.xyz );
                float3 localRayOrigin = IN.posLs-localRayDirection*100;
                const float maxRayLength = 1000;//ComputeMaxRayLengthInBoundingBox(localRayOrigin,localRayDirection,_boundingBoxMin_ls ,_boundingBoxMax_ls);
 
                //const float3 rayDirection = normalize(_MainLightPosition.xyz);
                //float3 rayOrigin = IN.posLs.xyz-rayDirection*(length(bbSize_ls));
                //onst float maxRayLength = ComputeMaxRayLengthInBoundingBox(rayOrigin,rayDirection,_boundingBoxMin_ls ,_boundingBoxMax_ls);


                float rayLength = 0;
                
                // === raymarching ===
                
                //on avance le long d'un rayon jusqu'à ce que la distance avec la scène soit quasi nulle.
                //https://iquilezles.org/articles/raymarchingdf/
                bool hitAnySegment = false;
                float3 samplePoint;
                float sdf;
                for (int i =0; i<_maxIterations;i++)
                {
                    samplePoint = localRayOrigin+localRayDirection*rayLength;
                    sdf = SceneSDF(samplePoint,0);

                    //distance quasi nulle <=> surface touchée
                    if (sdf<=_threshold)
                    {
                        hitAnySegment = true;
                        break;
                    }
                    
                    rayLength += sdf+_threshold;
                    clip((maxRayLength-rayLength));
                }
                clip(hitAnySegment-.5f);
                
                //write to depth
                float4 linearDepth = TransformWorldToHClip(mul(_treeTransform_ls_to_ws,float4( samplePoint,1)));
                float depth = linearDepth.z / linearDepth.w;
                return depth;
                
            }
            
            ENDHLSL
        }

 
    }
}


