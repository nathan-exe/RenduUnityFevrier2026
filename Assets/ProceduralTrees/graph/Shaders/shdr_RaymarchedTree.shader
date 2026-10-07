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
        _lightGradient ("lightGradient", 2D) = "white" {}
        _hueMapOverAge ("Hue Map Over Age", 2D) = "white" {}
        _maxAge ("maxAge", Float) = 1
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
            sampler2D  _lightGradient;
            sampler2D  _hueMapOverAge;
            float _maxAge;
            
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
                float4 posWs  : TEXCOORD1;
                float4 screenPosition : TEXCOORD3;
                float3 posLs  : TEXCOORD0;
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

                OUT.screenPosition = ComputeScreenPos(OUT.positionCS);

                
                // Returning the output. 
                return OUT;
            }
            
            // cone defined by extremes "pa" and "pb", and radius "ra" and "rb"
            float4 raycastAgainstRoundedCone( float3 rayOrigin, float3 rayDirection, float3 A, float3 B, float rA, float rB )
            {
                float3  ba = B - A;
                float3  oa = rayOrigin - A;
                float3  ob = rayOrigin - B;
                float rr = rA - rB;
                float m0 = dot(ba,ba);
                float m1 = dot(ba,oa);
                float m2 = dot(ba,rayDirection);
                float m3 = dot(rayDirection,oa);
                float m5 = dot(oa,oa);
                float m6 = dot(ob,rayDirection);
                float m7 = dot(ob,ob);
                
                // body
                float d2 = m0-rr*rr;
                float k2 = d2    - m2*m2;
                float k1 = d2*m3 - m1*m2 + m2*rr*rA;
                float k0 = d2*m5 - m1*m1 + m1*rr*rA*2.0 - m0*rA*rA;
                float h = k1*k1 - k0*k2;
                if( h<0.0) return float4(-1.0,-1,-1,-1);
                float t = (-sqrt(h)-k1)/k2;
              //if( t<0.0 ) return float4(-1.0);
                float y = m1 - rA*rr + t*m2;
                if( y>0.0 && y<d2 ) return float4(t, normalize(d2*(oa+t*rayDirection)-ba*y));

                // caps
                float h1 = m3*m3 - m5 + rA*rA;
                float h2 = m6*m6 - m7 + rB*rB;
                if( max(h1,h2)<0.0 ) return float4(-1.0,-1,-1,-1);
                float4 r = float4(1e20,1e20,1e20,1e20);
                if( h1>0.0 )
                {        
    	            t = -m3 - sqrt( h1 );
                    r = float4( t, (oa+t*rayDirection)/rA );
                }
                if( h2>0.0 )
                {
    	            t = -m6 - sqrt( h2 );
                    if( t<r.x )
                    r = float4( t, (ob+t*rayDirection)/rB );
                }
                return r;
            }
            
            // fragment shader
            fragOutput frag(V2f IN) 
            {
                fragOutput output;
                
                float2 screenUVs = GetNormalizedScreenSpaceUV(IN.positionCS);
                
                // === pixel culling ===

                clip((screenUVs.x<=1 && screenUVs.x >=0 && (screenUVs.y<=1 && screenUVs.y >=0))-0.5);
                
                //on clip les backfaces ou les front faces selon si la cam
                //est dans la bounding box pour eviter de dessiner l'arbre deux fois à chaque fois. -> +5fps
                float3 localCameraPos = mul(Inverse(_treeTransform_ls_to_ws),float4(_WorldSpaceCameraPos,1));
                bool cameraIsInsideBoundingBox = is_in_bounding_box(localCameraPos,_boundingBoxMin_ls-.1,_boundingBoxMax_ls+.1);
                bool backface = dot(IN.normalWs,GetWorldSpaceNormalizeViewDir(IN.posWs.xyz))<0;
                clip(!cameraIsInsideBoundingBox ^ backface ? 1 : -1);

                //on fait une premiere etape de raymarching en 2D, screenspace pour clip tous les pixels de la bb qui ne toucheront aucune branche. -> -5fps
                //clip(-SceneSDF_2D(IN.posWs)+.01);
                
                /// === preparation raytracing ===
                
                //definition du rayon sur lequel on va se déplacer
                float3 localRayOrigin = cameraIsInsideBoundingBox ? localCameraPos : IN.posLs;
                float3 localRayDirection = mul((float3x3)Inverse(_treeTransform_ls_to_ws),-GetWorldSpaceNormalizeViewDir(IN.posWs.xyz).xyz);// normalize(IN.worldPos.xyz- _WorldSpaceCameraPos.xyz );
                
                // === raytracing ===
                
                //on trouve les candidats au raymarching avec du raycasting //todo : avec l'octree
                float4 closesResult = float4(-1,0,0,0);
                int hitSegmentIndex;
                for (int i=0; i<_segmentCount; i++)
                {
                    float4 raycastResult = raycastAgainstRoundedCone(localRayOrigin, localRayDirection,_segments_ls[i].a,_segments_ls[i].b,_segments_ls[i].radiusA,_segments_ls[i].RadiusB);
                    if (raycastResult.x > 0 && (raycastResult.x < closesResult.x || closesResult.x<0))
                    {
                        hitSegmentIndex = i;
                        closesResult = raycastResult;
                    }
                }

                clip(closesResult.x);
            
                //pixel shading
                float lambert = max(0,dot(closesResult.yzw,_MainLightPosition.xyz));
                float3 col = tex2D(_lightGradient,float2(lambert,0));
                //todo : hue shift in data
                float normalizedAge =  _segments_ls[hitSegmentIndex].age / _maxAge;
                float hueshift = tex2D(_hueMapOverAge,float2(normalizedAge,0.5f)).x * 2 * PI;
                output.color = float4(hueShift(col,hueshift),1);
                lambert = round(lambert*3)/3;
                lambert = pow(lambert,.5);
                output.color *= lambert*.5+.5;
                output.color *= normalizedAge+1;
                //output.color = float4(hueshift,1);
                //output.color = float4(normalizedAge,normalizedAge,normalizedAge,1);

                float3 worldPos = localRayOrigin + closesResult.x * localRayDirection;
                float4 linearDepth = TransformWorldToHClip(mul(_treeTransform_ls_to_ws,float4( worldPos,1)));
                float depth = linearDepth.z / linearDepth.w;
                output.depth = depth;
                return output;
                
                // === shading du pixel ===
                
                //compute normal
                //float3 normal = mul((float3x3)_treeTransform_ls_to_ws,sceneHit.normal);

                //compute age
                // float age =
                //     _segments_ls[sceneHit.segID].age
                //     + closestHit.clampedT*distance(_segments_ls[sceneHit.segID].a,_segments_ls[sceneHit.segID].b);

                //compute UV
                // const float3 mainSegmentDir = (_segments_ls[sceneHit.segID].b-_segments_ls[sceneHit.segID].a);
                //
                // float3 referenceVector = normalize(float3(0,0,1));//normalize(_segments_ls[sceneHit.secondClosestSegID].b-_segments_ls[sceneHit.secondClosestSegID].a);
                // const float3 dir = normalize(mainSegmentDir);
                // referenceVector = normalize(projectOnPlane(referenceVector,dir));
                // const float angle = FastAngle(normal,referenceVector);
                // float2 uv = 0;
                // uv.x = angle/PI/2;// * _segments_ls[sceneHit.segID].radius/_segments_ls[0].radius;
                // uv.y = closestHit.clampedT;
                
                //lighting
                // output.color = ShadeTree(
                //     normal,
                //     mul((float3x3)_treeTransform_ls_to_ws,localRayDirection),
                //     uv*.5)*(age*.2+1);
                
                //write to depth
                //float4 linearDepth = TransformWorldToHClip(mul(_treeTransform_ls_to_ws,float4( samplePoint,1)));
                //float depth = linearDepth.z / linearDepth.w;
                //output.depth = depth;
                
                return output;
                
            }

            // // === pseudo code exemple ===
            //
            // int _branchCount;
            // struct Branch
            // {
            // };
            // float ComputeBranchSdf(float3, Branch){}
            // StructuredBuffer<Branch> _branches;
            //
            // float ComputeSceneSDF(float3 samplePoint)
            // {
            //     //pour chaque branche de l'arbre
            //     float minSdf = 1000;
            //     for ( int i = 0; i<_branchCount; i++)
            //     {
            //         //on calcule la distance avec la branche,
            //         //et on retient la distance la plus proche du point
            //         minSdf = min(
            //             minSdf,
            //             ComputeBranchSdf(samplePoint, _branches[i]));
            //     }
            //     return minSdf;
            // }
            //
            //
            // void RaymarchScene(float3 cameraPosition, float3 rayDirection)
            // {
            //     const int MAX_ITERATIONS = 30;
            //
            //     float distanceToScene = 1000;
            //     float totalTraveledDistance = 0;
            //     for (int i = 0; i<MAX_ITERATIONS; i++)
            //     {
            //         //on calcule la distance avec la scène
            //         float3 samplePoint = cameraPosition + rayDirection * totalTraveledDistance;
            //         distanceToScene = ComputeSceneSDF(samplePoint);
            //
            //         //on avance de cette distance le long du rayon
            //         totalTraveledDistance += distanceToScene;
            //
            //         //sdf < 0.01 : le rayon a touché une branche de l'arbre.
            //         if (distanceToScene < 0.01)
            //             break;
            //     }
            //
            //     //on discard tous les pixels qui n'ont touché aucune branche de l'arbre
            //     clip(0.01-distanceToScene); 
            //     
            //     //todo : shade pixel
            // }
            
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
                // // === pixel culling ===
                //
                // //on clip les backfaces
                //
                // /// === preparation raymarching ===
                //
                // //definition du rayon sur lequel on va se déplacer
                // const float3 localRayDirection = normalize(mul((float3x3)Inverse(_treeTransform_ls_to_ws),_MainLightPosition));// normalize(IN.worldPos.xyz- _WorldSpaceCameraPos.xyz );
                // float3 localRayOrigin = IN.posLs-localRayDirection*100;
                // const float maxRayLength = 1000;//ComputeMaxRayLengthInBoundingBox(localRayOrigin,localRayDirection,_boundingBoxMin_ls ,_boundingBoxMax_ls);
                //
                // //const float3 rayDirection = normalize(_MainLightPosition.xyz);
                // //float3 rayOrigin = IN.posLs.xyz-rayDirection*(length(bbSize_ls));
                // //onst float maxRayLength = ComputeMaxRayLengthInBoundingBox(rayOrigin,rayDirection,_boundingBoxMin_ls ,_boundingBoxMax_ls);
                //
                // //filtrage des branches
                // int possibleSegmentIDs[MAX_CANDIDATES];
                // int stackPointer = 0;
                // for (int i=0; i<_segmentCount && stackPointer<MAX_CANDIDATES;i++)
                // {
                //     if (RayOverlapsSegment(localRayOrigin, localRayDirection,_segments_ls[i],_threshold))
                //     {
                //         possibleSegmentIDs[stackPointer++] = i; 
                //     } 
                // }
                // clip(stackPointer-.5);
                //
                //
                //
                // float rayLength = 0;
                //
                // // === raymarching ===
                //
                // //on avance le long d'un rayon jusqu'à ce que la distance avec la scène soit quasi nulle.
                // //https://iquilezles.org/articles/raymarchingdf/
                // bool hitAnySegment = false;
                // float3 samplePoint;
                // float sdf;
                // for (int i =0; i<_maxIterations;i++)
                // {
                //     samplePoint = localRayOrigin+localRayDirection*rayLength;
                //     //sdf = SceneSDF(samplePoint);
                //     sdf = SceneSDF(samplePoint,possibleSegmentIDs,MAX_CANDIDATES);
                //
                //     //distance quasi nulle <=> surface touchée
                //     if (sdf<=_threshold)
                //     {
                //         hitAnySegment = true;
                //         break;
                //     }
                //     
                //     rayLength += sdf+_threshold;
                //     clip((maxRayLength-rayLength));
                // }
                // clip(hitAnySegment-.5f);
                //
                //
                //
                //write to depth
                //float4 linearDepth = TransformWorldToHClip(mul(_treeTransform_ls_to_ws,float4( samplePoint,1)));
                //float depth = linearDepth.z / linearDepth.w;
                //return depth;
                return 0;
            }
            
            ENDHLSL
        }

 
    }
}


