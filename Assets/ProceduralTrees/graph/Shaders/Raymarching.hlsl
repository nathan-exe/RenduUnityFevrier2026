// === data ===

// un segment défini par deux points et un rayon.
struct Segment
{
    float3 a,b;
    float radiusA,RadiusB;
    float age;
};

struct SdfResult
{
    float sdf;//la distance signée avec le segment
    float3 t;//distance AH
    float3 h;//le point projeté sur le centre du segment
};

struct SceneHit
{
    float distance;
    int segID;
    int secondClosestSegID;
    float smoothFactor;
};
 
// === math ===

//https://iquilezles.org/articles/smin/
float smooth_min( float a, float b, float k )
{
    // k *= 4.0;
    // float h = max( k-abs(a-b), 0.0 )/k;
    // return min(a,b) - h*h*k*(1.0/4.0);
    // k *= 2.0;
    // float x = b-a;
    // return 0.5*( a+b-sqrt(x*x+k*k) );
    k *= 2.0;
    float x = b-a;
    return 0.5*( a+b-sqrt(x*x+k*k) );
}

// === bounding box helper functions ===

float ComputeMaxRayLengthInBoundingBox(float3 origin,float3 direction,float3 boxMin, float3 boxMax)
{
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