Shader "Vegetation/RaymarchedTree"
{
    // The Properties block of the Unity shader.
    Properties 
    { 
         [HideInInspector] _boundingBoxCenter ("_boundingBoxCenter", Vector) = (.25, .5, .5, 1)
         [HideInInspector] _boundingBoxSize  ("_boundingBoxSize", Vector) = (.25, .5, .5, 1)

        //raymarching
        _threshold ("Raymarch hit threshold", Float) = .1
        _maxIterations ("max Iterations", Int) = 10
        _smoothing ("smoothing", Float) = 1
        
        //material
        _albedo ("albedo", Color) = (1,1,1, 1)
        
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

            // un segment défini par deux points et un rayon.
            struct Segment
            {
                half3 a,b;
                half radius;
                half age;
            };
            
            struct VertexAttributes
            {
                //vertex position in object space
                float4 positionOS   : POSITION;
            };

            struct V2f
            {
                float4 positionHCS  : SV_POSITION;
                float4 worldPos  : TEXCOORD0;
            };

            //bounding box
            float3 _boundingBoxCenter;
            float3 _boundingBoxSize;

            //raymarching
            float _threshold;
            int _maxIterations;
            float _smoothing;

            //scene
            StructuredBuffer<Segment> _segments; //todo : binary space partitionning 
            int _segmentCount;
            matrix _treeTransform;

            //material
            float3 _albedo;

            //https://iquilezles.org/articles/smin/
            float smooth_min( float a, float b, float k )
            {
                k *= 4.0;
                float h = max( k-abs(a-b), 0.0 )/k;
                return min(a,b) - h*h*k*(1.0/4.0);
            }
            
            float ComputeMaxRayLengthInBoundingBox(float3 origin,float3 direction,float3 boxMin, float3 boxMax){
                float3 T_1, T_2; // vectors to hold the T-values for every direction
                float t_near = -Max_float(); 
                float t_far = Max_float();
                
                for (int i = 0; i < 3; i++)
                { //we test slabs in every direction
                    if (direction[i] == 0)
                    { // ray parallel to planes in this direction
                        if ((origin[i] < boxMin[i]) || (origin[i] > boxMax[i]))
                        {
                            return false; // parallel AND outside box : no intersection possible
                        }
                    }
                    else
                    { // ray not parallel to planes in this direction
                        T_1[i] = (boxMin[i] - origin[i]) / direction[i];
                        T_2[i] = (boxMax[i] - origin[i]) / direction[i];

                        if(T_1[i] > T_2[i]){ // we want T_1 to hold values for intersection with near plane
                            float temp = T_1[i];
                            T_1[i] = T_2[i];
                            T_2[i] = temp;
                        }
                        if (T_1[i] > t_near){
                            t_near = T_1[i];
                        }
                        if (T_2[i] < t_far){
                            t_far = T_2[i];
                        }
                        if( (t_near > t_far) || (t_far < 0) ){
                            return false;
                        }
                    }
                }
               
                return t_far; // if we made it here, there was an intersection - YAY


            }
            
            bool is_in_bounding_box(float3 pos,float3 boxMin, float3 boxMax){ 
                return 
                pos.x <= boxMax.x && pos.x >= boxMin.x 
                && pos.y <= boxMax.y && pos.y >= boxMin.y 
                && pos.z <= boxMax.z && pos.z >= boxMin.z;
            }

            struct SdfResult
            {
                float sdf;//la distance signée avec le segment
                float3 t;//distance AH
                float3 h;//le point projeté sur le centre du segment
            };

            //retourne la distance signée avec un segment épaissis; une capsule
            //https://iquilezles.org/articles/distfunctions/
            SdfResult SegmentSDF(float3 worldpos,Segment segment)
            {
                SdfResult output;
                
                //H = le point M (world pos) projeté sur le segment AB.
                // on retourne la distance entre H et M - le rayon de la capsule.
                float3 A = mul(_treeTransform,float4(segment.a,1));
                float3 B = mul(_treeTransform,float4(segment.b,1));
                float3 AM = worldpos - A;
                float3 AB = B - A;
                float normeAB = length(AB);
                //float3 normalizedAB = normalize(ab);
                //float3 h = segment.a + (dot(normalizedAB,am) * normalizedAB) - worldpos;

                float projectionLength = dot(AB,AM)/(normeAB * normeAB);
                projectionLength = saturate(projectionLength);
                float3 H = A + AB*(projectionLength);

                output.t = projectionLength;
                output.h = H;
                output.sdf =  length(worldpos - H) - segment.radius;//todo : lerp(radiusA, radiusB, t)
                return output;
            }

            struct SceneHit
            {
                float distance;
                int segID;
                int secondClosestSegID;
                float smoothFactor;
            };

            //retourne la distance signée avec l'ensemble des branches de l'arbre
            SceneHit SceneSDF(float3 worldpos)
            {
                //todo : octree ou binary space partitionning pour éviter d'itérer à travers tous les segments.
                //todo : interpolation d'attributs entre les 2 segments les plus proches
                
                SceneHit hit;
                hit.distance = 1000000;
                hit.segID = 0;
                for (int i = 0; i<_segmentCount;i++)
                {
                    SdfResult result = SegmentSDF(worldpos,_segments[i]);
                    float oldDistance = hit.distance;
                    
                    
                    hit.distance = smooth_min(hit.distance,result.sdf,_smoothing*_segments[i].radius);
                    if (oldDistance > result.sdf)
                    {
                        hit.smoothFactor =  abs(hit.distance-result.sdf)/(_smoothing*_segments[i].radius);
                        hit.secondClosestSegID = hit.segID;
                        hit.segID = i;
                    }
                }
                
                return hit;
            }
            
            
            //== shader functions ==

            //vertex shader
            V2f vertex(VertexAttributes IN)
            {
                V2f OUT; 

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.worldPos = mul(unity_ObjectToWorld,IN.positionOS);
                    
                // Returning the output. 
                return OUT;
            }

            struct fragOutput
            {
                half4 color : SV_Target;
                float depth : SV_Depth;
            };

            float4 ComputeOutputColor()
            {
                
            }
            
            // fragment shader
            fragOutput frag(V2f IN) 
            {
                fragOutput output;

                
                
                //define bounding box
                float3 boxMin = _boundingBoxCenter - _boundingBoxSize*0.5;
                float3 boxMax = _boundingBoxCenter + _boundingBoxSize*0.5;
                float3 bbWorldSize = (boxMax - boxMin);
                float3 bbInvWorldSize = 1.0/bbWorldSize;
                
                //define ray
                float3 rayOrigin = is_in_bounding_box(_WorldSpaceCameraPos.xyz,boxMin-1,boxMax+1) ? _WorldSpaceCameraPos.xyz : IN.worldPos.xyz;
                float2 screenPos = mul(unity_WorldToCamera,IN.worldPos.xyz);
                
                const float3 rayDirection = -GetWorldSpaceNormalizeViewDir(IN.worldPos.xyz);// normalize(IN.worldPos.xyz- _WorldSpaceCameraPos.xyz );
                const float maxRayLength = ComputeMaxRayLengthInBoundingBox(rayOrigin,rayDirection,boxMin ,boxMax);
                
                float rayLength = 0;
                //raymarching : on avance le long d'un rayon jusqu'à ce que la distance avec la scène soit quasi nulle.
                //https://iquilezles.org/articles/raymarchingdf/
                for (int i =0; i<_maxIterations;i++)
                {
                    float3 samplePoint = rayOrigin+rayDirection*rayLength;
                    SceneHit sceneHit = SceneSDF(samplePoint);

                    //distance quasi nulle <=> surface touchée
                    if (sceneHit.distance<=_threshold)
                    {
                        
                        float segId01 = (float)sceneHit.segID/(float)_segmentCount;
                        
                        //samplePoint = rayOrigin+rayDirection*rayLength;

                        //compute normal
                        Segment hitSegment = _segments[sceneHit.segID];
                        SdfResult h =  SegmentSDF(samplePoint,hitSegment);
                        SdfResult h2 =  SegmentSDF(samplePoint,_segments[sceneHit.secondClosestSegID]);
                        
                        //float3 normal = normalize(samplePoint-h.h);
                        float3 normal = normalize(samplePoint-lerp(h.h,h2.h,sceneHit.smoothFactor));
                        
                        //lightning
                        float lambert = saturate(dot(normal,_MainLightPosition));
                        float specular = pow(saturate(dot(reflect(_MainLightPosition, normal), rayDirection)), 2);
                        float fresnel = pow(saturate(dot(reflect(rayDirection, normal), rayDirection)), 1.4);
                        float3 light = lerp(unity_AmbientSky *1.5 ,_MainLightColor,lambert) + specular * (_MainLightColor)*.1 + fresnel * unity_AmbientSky;
                        float3 color = _albedo * light;
                        
                        //write to depth
                        float4 linearDepth = TransformWorldToHClip(samplePoint);
                        float depth = linearDepth.z / linearDepth.w;
                        output.depth = depth;
                        
                        output.color = float4(color,1);
                        return output;
                        
                        //return lerp( float4(1,0,0,1) , float4(color,1),_segments[sceneHit.segID].age);
                    }
                    
                    rayLength += sceneHit.distance;
                    clip((rayLength < maxRayLength)-.1);
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
Pass
        {
            Tags {"LightMode"="ShadowCaster" }
            
            // The HLSL code block. Unity SRP uses the HLSL language.
            HLSLPROGRAM
            // This line defines the name of the vertex shader.
            #pragma vertex vertex
            // This line defines the name of the fragment shader.
            #pragma fragment frag
            
            // hlsl includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "ShaderHelpers.hlsl"

            // un segment défini par deux points et un rayon.
            struct Segment
            {
                half3 a,b;
                half radius;
                half age;
            };
            
            struct VertexAttributes
            {
                //vertex position in object space
                float4 positionOS   : POSITION;
            };

            struct V2f
            {
                float4 positionHCS  : SV_POSITION;
                float4 worldPos  : TEXCOORD0;
            };

            //bounding box
            float3 _boundingBoxCenter;
            float3 _boundingBoxSize;

            //raymarching
            float _threshold;
            int _maxIterations;
            float _smoothing;

            //scene
            StructuredBuffer<Segment> _segments; //todo : binary space partitionning 
            int _segmentCount;
            matrix _treeTransform;
            
            float ComputeMaxRayLengthInBoundingBox(float3 origin,float3 direction,float3 boxMin, float3 boxMax){
                float3 T_1, T_2; // vectors to hold the T-values for every direction
                float t_near = -Max_float(); 
                float t_far = Max_float();
                
                for (int i = 0; i < 3; i++)
                { //we test slabs in every direction
                    if (direction[i] == 0)
                    { // ray parallel to planes in this direction
                        if ((origin[i] < boxMin[i]) || (origin[i] > boxMax[i]))
                        {
                            return false; // parallel AND outside box : no intersection possible
                        }
                    }
                    else
                    { // ray not parallel to planes in this direction
                        T_1[i] = (boxMin[i] - origin[i]) / direction[i];
                        T_2[i] = (boxMax[i] - origin[i]) / direction[i];

                        if(T_1[i] > T_2[i]){ // we want T_1 to hold values for intersection with near plane
                            float temp = T_1[i];
                            T_1[i] = T_2[i];
                            T_2[i] = temp;
                        }
                        if (T_1[i] > t_near){
                            t_near = T_1[i];
                        }
                        if (T_2[i] < t_far){
                            t_far = T_2[i];
                        }
                        if( (t_near > t_far) || (t_far < 0) ){
                            return false;
                        }
                    }
                }
               
                return t_far; // if we made it here, there was an intersection - YAY


            }
            
            bool is_in_bounding_box(float3 pos,float3 boxMin, float3 boxMax){ 
                return 
                pos.x <= boxMax.x && pos.x >= boxMin.x 
                && pos.y <= boxMax.y && pos.y >= boxMin.y 
                && pos.z <= boxMax.z && pos.z >= boxMin.z;
            }

            struct SdfResult
            {
                float sdf;//la distance signée avec le segment
                float3 t;//distance AH
                float3 h;//le point projeté sur le centre du segment
            };

            //retourne la distance signée avec un segment épaissis; une capsule
            //https://iquilezles.org/articles/distfunctions/
            SdfResult SegmentSDF(float3 worldpos,Segment segment)
            {
                SdfResult output;
                
                //H = le point M (world pos) projeté sur le segment AB.
                // on retourne la distance entre H et M - le rayon de la capsule.
                float3 A = mul(_treeTransform,float4(segment.a,1));
                float3 B = mul(_treeTransform,float4(segment.b,1));
                float3 AM = worldpos - A;
                float3 AB = B - A;
                float normeAB = length(AB);
                //float3 normalizedAB = normalize(ab);
                //float3 h = segment.a + (dot(normalizedAB,am) * normalizedAB) - worldpos;

                float projectionLength = dot(AB,AM)/(normeAB * normeAB);
                projectionLength = saturate(projectionLength);
                float3 H = A + AB*(projectionLength);

                output.t = projectionLength;
                output.h = H;
                output.sdf =  length(worldpos - H) - segment.radius;//todo : lerp(radiusA, radiusB, t)
                return output;
            }

            struct SceneHit
            {
                float distance;
                int segID;
                int secondClosestSegID;
                float smoothFactor;
            };

            //retourne la distance signée avec l'ensemble des branches de l'arbre
            SceneHit SceneSDF(float3 worldpos)
            {
                //todo : octree ou binary space partitionning pour éviter d'itérer à travers tous les segments.
                //todo : interpolation d'attributs entre les 2 segments les plus proches
                
                SceneHit hit;
                hit.distance = 1000000;
                hit.segID = 0;
                for (int i = 0; i<_segmentCount;i++)
                {
                    if (_segments[i].radius<0.02) continue;
                    
                    SdfResult result = SegmentSDF(worldpos,_segments[i]);
                    float oldDistance = hit.distance;
                    
                    
                    hit.distance = min(hit.distance,result.sdf);
                    if (oldDistance > result.sdf)
                    {
                        hit.segID = i;
                    }
                }
                
                return hit;
            }
            
            
            //== shader functions ==

            //vertex shader
            V2f vertex(VertexAttributes IN)
            {
                V2f OUT; 

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.worldPos = mul(unity_ObjectToWorld,IN.positionOS);
                    
                // Returning the output. 
                return OUT;
            }

            struct fragOutput
            {
                float depth : SV_Depth;
            };
            
            // fragment shader
            fragOutput frag(V2f IN) 
            {
                fragOutput output;
                
                //define bounding box
                float3 boxMin = _boundingBoxCenter - _boundingBoxSize*0.5;
                float3 boxMax = _boundingBoxCenter + _boundingBoxSize*0.5;
                float3 bbWorldSize = (boxMax - boxMin);
                float3 bbInvWorldSize = 1.0/bbWorldSize;
                
                //define ray
                
                
                const float3 rayDirection = normalize(_MainLightPosition.xyz);
                float3 rayOrigin = IN.worldPos.xyz-rayDirection*(length(_boundingBoxSize));
                const float maxRayLength = ComputeMaxRayLengthInBoundingBox(rayOrigin,rayDirection,boxMin ,boxMax);
                
                float rayLength = 0;
                //raymarching : on avance le long d'un rayon jusqu'à ce que la distance avec la scène soit quasi nulle.
                //https://iquilezles.org/articles/raymarchingdf/
                _maxIterations = _maxIterations/2;
                for (int i =0; i<_maxIterations;i++)
                {
                    float3 samplePoint = rayOrigin+rayDirection*rayLength;
                    SceneHit sceneHit = SceneSDF(samplePoint);

                    //distance quasi nulle <=> surface touchée
                    if (sceneHit.distance<=_threshold*5)
                    {
                        //write to depth
                        float4 linearDepth = TransformWorldToHClip(samplePoint);
                        float depth = linearDepth.z / linearDepth.w;
                        output.depth = depth;
                        
                        return output;
                        
                        //return lerp( float4(1,0,0,1) , float4(color,1),_segments[sceneHit.segID].age);
                    }
                    
                    rayLength += sceneHit.distance;
                    clip((rayLength < maxRayLength)-.1);
                }

                //nombre max de steps dépassé
                clip(-1);
                output.depth = 0;
                return output;
            }
            ENDHLSL
        }
    }
}
