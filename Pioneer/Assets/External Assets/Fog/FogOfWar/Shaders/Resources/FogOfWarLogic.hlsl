#ifndef _CONESETUP_
#define _CONESETUP_

struct CircleStruct
{
    float2 circleOrigin;
    int startIndex;
    int numSegments;
    float circleHeight;
    float unobscuredRadius;
    float circleRadius;
    float circleFade;
    float visionHeight;
    float heightFade;
    float revealerOpacity;
};
struct ConeEdgeStruct
{
    float edgeAngle;
    float length;
    bool cutShort;
};

#pragma multi_compile_local HARD SOFT
#pragma multi_compile_local _ INNER_SOFTEN
#pragma multi_compile_local _ SAMPLE_REALTIME
#pragma multi_compile_local _ USE_TEXTURE_BLUR

bool BLEED;
bool BLEND_MAX;
int _fadeType;
bool _invertEffect;

bool _ditherFog;
float _ditherSize;

bool _pixelate;
bool _pixelateWS;
float _pixelDensity;
float2 _pixelOffset;

float _extraRadius;

float _fadeOutDegrees;
float _edgeSoftenDistance;
float _unboscuredFadeOutDistance;

int _NumRevealers;
StructuredBuffer<int> _ActiveCircleIndices;
StructuredBuffer<CircleStruct> _CircleBuffer;
StructuredBuffer<ConeEdgeStruct> _ConeBuffer;
sampler2D _FowRT;
float4 _FowRT_TexelSize;
int _Sample_Blur_Quality;
float _Sample_Blur_Amount;
float4 _worldBounds;
float _worldBoundsSoftenDistance;
float _worldBoundsInfluence;

float _fadePower;

float lineThickness = .1;

int _fowPlane;

//2D variables
float _cameraSize;
float2 _cameraPosition;
float _cameraRotation;

//float Unity_InverseLerp_float4(float4 A, float4 B, float4 T)
//{
//    return (T - A) / (B - A);
//}

bool IsOne(half value)
{
    return (abs(1 - value) < .001);
}

//float SampleBlurredTexture(float2 UV, float Blur)
float SampleBlurredTexture(float2 UV, int quality)
{
    float Out_Alpha = 0;
    float kernelSum = 0.0;
    
    //Blur = min(Blur, 16);
    //int upper = ((Blur - 1) / 2);
    int upper = quality;
    int lower = -upper;
 
    [loop]
    for (int x = lower; x <= upper; ++x)
    {
        [loop]
        for (int y = lower; y <= upper; ++y)
        {
            kernelSum++;
 
            float2 offset = float2(_FowRT_TexelSize.x * x, _FowRT_TexelSize.y * y) * _Sample_Blur_Amount;
            Out_Alpha += 1 - tex2D(_FowRT, UV + offset).w;
        }
    }
 
    Out_Alpha /= kernelSum;
    return Out_Alpha;

}

void TextureSample_float(float2 Position, inout float coneOut)
{
#if SAMPLE_REALTIME
#else
    float2 uv = float2((((Position.x - _worldBounds.y) + (_worldBounds.x / 2)) / _worldBounds.x),
                 (((Position.y - _worldBounds.w) + (_worldBounds.z / 2)) / _worldBounds.z));
    
#if USE_TEXTURE_BLUR
    float texSamp = SampleBlurredTexture(uv, _Sample_Blur_Quality);
#else
    float texSamp = 1-tex2D(_FowRT, uv).w;
#endif
    coneOut = texSamp;
    return;
    if (Position.x > _worldBounds.y + (_worldBounds.x / 2) ||
        Position.x < _worldBounds.y - (_worldBounds.x / 2) ||
        Position.y > _worldBounds.w + (_worldBounds.z / 2) ||
        Position.y < _worldBounds.w - (_worldBounds.z / 2))
    {
        texSamp = 0;
    }
    
    //texSamp = Unity_InverseLerp_float4(0, .52, texSamp);
    //coneOut = float2(uv.x, 0);
    //coneOut = lerp(texSamp, coneOut, coneOut);
    coneOut = max(texSamp, coneOut);
    //coneOut += texSamp;
    coneOut = clamp(coneOut, 0, 1);
#endif
}

//shamelessly stolen from a generated shadergraph
void Dither(float In, float2 uv, out float Out)
{
    uv *= _ditherSize;
    //float2 uv = ScreenPosition.xy * _ScreenParams.xy;
    float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0, 9.0 / 17.0, 3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0, 5.0 / 17.0, 15.0 / 17.0, 7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0, 2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0, 8.0 / 17.0, 14.0 / 17.0, 6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    Out = ceil(saturate(In - DITHER_THRESHOLDS[index]));
}

void CustomCurve_float(float In, out float Out)
{
    Out = In; //fade type 1; linear
    if (_invertEffect)
        Out = 1 - Out;
    switch (_fadeType)
    {
        case 0: //Linear Fade
            return;
        case 1: //Smooth Fade
            Out = sin(Out * 1.570796);
            return;
        case 2: //Smoother Fade
            Out = .5 - (cos(Out * 3.1415926) * .5);
            return;
        case 3: //Smoothstep Fade
            Out = smoothstep(0, 1, In);
            return;
        case 4: //Exponential Fade
            Out = pow(Out, _fadePower);
            return;
    }
}

void CustomCurve_half(half In, out half Out)
{
    Out = In; //fade type 1; linear
    if (_invertEffect)
        Out = 1 - Out;
    switch (_fadeType)
    {
        case 0: //Linear Fade
            return;
        case 1: //Smooth Fade
            Out = sin(Out * 1.570796);
            return;
        case 2: //Smoother Fade
            Out = .5 - (cos(Out * 3.1415926) * .5);
            return;
        case 3: //Smoothstep Fade
            Out = smoothstep(0, 1, In);
            return;
        case 4: //Exponential Fade
            Out = pow(Out, _fadePower);
            return;
    }
}

void OutOfBoundsCheck(float2 Position, inout float4 color)
{
//#if USE_WORLD_BOUNDS
    float OOBX = max(0, ((Position.x + _worldBoundsSoftenDistance) - (_worldBounds.y + (_worldBounds.x / 2))));
    OOBX = max(OOBX, -(Position.x - _worldBoundsSoftenDistance - (_worldBounds.y - (_worldBounds.x / 2))));
    float OOBY = max(0, ((Position.y + _worldBoundsSoftenDistance) - (_worldBounds.w + (_worldBounds.z / 2))));
    OOBY = max(OOBY, -((Position.y - _worldBoundsSoftenDistance) - (_worldBounds.w - (_worldBounds.z / 2))));
    
    float OOB = length((float2(OOBX, OOBY)));
    OOB = saturate(OOB / _worldBoundsSoftenDistance);
    OOB *= _worldBoundsInfluence;
    //CustomCurve_float(OOB, OOB);
    color = lerp(color, float4(0, 0, 0, 1), OOB * _worldBoundsInfluence);
    //if (Position.x > _worldBounds.y + (_worldBounds.x/2) ||
    //    Position.x < _worldBounds.y - (_worldBounds.x/2) ||
    //    Position.y > _worldBounds.w + (_worldBounds.z/2) ||
    //    Position.y < _worldBounds.w - (_worldBounds.z/2))
    //{
    //    color = lerp(color, float4(0, 0, 0, 1), _worldBoundsInfluence);
    //}
//#endif
}

half CalculateFadeZonePercent(half segmentLength, half SoftenDistance, half DistanceToOrigin)
{
    return ((segmentLength + SoftenDistance) - DistanceToOrigin) / SoftenDistance;
}

half SmoothValue(half val)
{
    //return val;
    return smoothstep(0, 1, val);
    //val = clamp(val, 0, 1);
    //return sin(val * 1.570796);;
    //return 1 - (cos(val * 3.14159) * .5 + .5);
}

half angleDiff(half ang1, half ang2)
{
    half diff = (ang1 - ang2 + 180) % 360 - 180;
    return diff > _fadeOutDegrees ? diff - 360 : diff;
}

void LoopRevealerHardFog(CircleStruct circle, half2 relativePosition, half distToRevealerOrigin, half2 Position, inout float RevealerOut)
{
    half _fadeOutDistance = circle.circleFade;
    half deg = degrees(atan2(relativePosition.y, relativePosition.x));
    ConeEdgeStruct previousCone = _ConeBuffer[circle.startIndex];
    half prevAng = previousCone.edgeAngle;
    for (int c = 1; c < circle.numSegments; c++)
    {
        //prevAng = previousCone.edgeAngle - .001;
        //prevAng = previousCone.edgeAngle + .01;
        prevAng = previousCone.edgeAngle;
        ConeEdgeStruct currentCone = _ConeBuffer[circle.startIndex + c];

        float degDiff = angleDiff(deg + 360, currentCone.edgeAngle);
        float segmentAngle = currentCone.edgeAngle - prevAng;
                
        //if (deg > prevAng && currentCone.edgeAngle > deg)
        if (degDiff > -segmentAngle && degDiff <= 0)
        {
            //float lerpVal = (deg - prevAng) / (currentCone.edgeAngle - prevAng);
            //float DistToSegmentEnd = lerp(previousCone.length, currentCone.length, lerpVal);
            float DistToSegmentEnd = currentCone.length;
            //if (abs(previousCone.length - circle.circleRadius) > .001 || abs(currentCone.length - circle.circleRadius) > .001)
            if (previousCone.cutShort && currentCone.cutShort)  //draw straight line thru points
            {
                float2 start = circle.circleOrigin + float2(cos(radians(prevAng)), sin(radians(prevAng))) * previousCone.length;
                float2 end = circle.circleOrigin + float2(cos(radians(currentCone.edgeAngle)), sin(radians(currentCone.edgeAngle))) * currentCone.length;
                        
                float a1 = end.y - start.y;
                float b1 = start.x - end.x;
                float c1 = a1 * start.x + b1 * start.y;
                    
                //float a2 = Position.y - circle.circleOrigin.y;
                float a2 = relativePosition.y;
                //float b2 = circle.circleOrigin.x - Position.x;
                float b2 = -relativePosition.x;
                float c2 = a2 * circle.circleOrigin.x + b2 * circle.circleOrigin.y;
                    
                float determinant = (a1 * b2) - (a2 * b1);
                    
                float x = (b2 * c1 - b1 * c2) / determinant;
                float y = (a1 * c2 - a2 * c1) / determinant;
                    
                float2 intercection = float2(x, y);
                DistToSegmentEnd = distance(intercection, circle.circleOrigin) + _extraRadius;
                        
                if (BLEED)
                {
                    //to add the cone
                    float2 rotPoint = (start + end) / 2;
                    float2 arcOrigin = rotPoint + (float2(-(end.y - rotPoint.y), (end.x - rotPoint.x)) * 3);
                    float arcLength = distance(start, arcOrigin);
                    float2 newRelativePosition = arcOrigin + normalize(Position - arcOrigin) * arcLength;
                    DistToSegmentEnd += distance(intercection, newRelativePosition) / 2;
                }
            }
            DistToSegmentEnd = max(DistToSegmentEnd, circle.unobscuredRadius);
                    
            if (distToRevealerOrigin < DistToSegmentEnd)
            {
                RevealerOut = 1;
                return;
            }
        }
                
        previousCone = currentCone;
    }
}

void FOW_Hard(float2 Position, float height, out float Out)
{
    Out = 0;
    if (_pixelateWS)
    {
        Position *= _pixelDensity;
        Position -= _pixelOffset;
        Position = round(Position);
        Position += _pixelOffset;
        Position /= _pixelDensity;
    }
#if SAMPLE_REALTIME
#else
    return;
#endif
    for (int i = 0; i < _NumRevealers; i++)
    {
        CircleStruct circle = _CircleBuffer[_ActiveCircleIndices[i]];
        float2 relativePosition = Position - circle.circleOrigin;
        if (_pixelate)
        {
            relativePosition *= _pixelDensity;
            relativePosition = round(relativePosition);
            relativePosition /= _pixelDensity;
        }
        float distToRevealerOrigin = length(relativePosition);

        if (distToRevealerOrigin > circle.circleRadius)
            continue;

#if IGNORE_HEIGHT
        float heightDist = 0;
#else
        float heightDist = abs(height - circle.circleHeight);
#endif

        if (heightDist > circle.visionHeight)
            continue;

        if (circle.unobscuredRadius < 0 && distToRevealerOrigin < -circle.unobscuredRadius)     //negative unobscured radius
            continue;

        half RevealerOut = 0;

        if (distToRevealerOrigin < circle.unobscuredRadius)
            RevealerOut = 1;
        else
            LoopRevealerHardFog(circle, relativePosition, distToRevealerOrigin, Position, RevealerOut);

        RevealerOut *= circle.revealerOpacity;
        if (BLEND_MAX)
            Out = max(Out, RevealerOut);
        else
            Out = min(1, Out + RevealerOut);

        if (IsOne(Out)) 
            return;
    }
}

void LoopRevealerSoftFog(CircleStruct circle, half2 relativePosition, half distToRevealerOrigin, half2 Position, inout float RevealerOut)
{
    half _fadeOutDistance = circle.circleFade;
    half deg = degrees(atan2(relativePosition.y, relativePosition.x));
    ConeEdgeStruct previousCone = _ConeBuffer[circle.startIndex];
    half prevAng = previousCone.edgeAngle;
#if INNER_SOFTEN
    for (int c = 0; c < circle.numSegments; c++)
#else
    for (int c = 1; c < circle.numSegments; c++)
#endif
    {
        //prevAng = previousCone.edgeAngle - .001;
        //prevAng = previousCone.edgeAngle + .01;
        prevAng = previousCone.edgeAngle;
        ConeEdgeStruct currentCone = _ConeBuffer[circle.startIndex + c];

        half degDiff = angleDiff(deg + 360, currentCone.edgeAngle);
        half segmentAngle = currentCone.edgeAngle - prevAng;
#if INNER_SOFTEN
        if (degDiff > -segmentAngle - _fadeOutDegrees && degDiff < _fadeOutDegrees)
#else
        if (degDiff > -segmentAngle && degDiff <= 0)
#endif
        {
            //half lerpVal = clamp(segmentAngle-degDiff, 0, segmentAngle)/segmentAngle;
            //half DistToSegmentEnd = lerp(previousCone.length, currentCone.length, lerpVal);
            half DistToSegmentEnd = currentCone.length;
            half newBlurDistance = (DistToSegmentEnd / circle.circleRadius) * _fadeOutDistance;
            newBlurDistance = _fadeOutDistance;
                    
#if INNER_SOFTEN
            if (!(degDiff > -segmentAngle && degDiff < 0))
            {
                half softDistToSegmentEnd = DistToSegmentEnd;
                half softnewBlurDistance = newBlurDistance;

                half angDiff = degDiff / _fadeOutDegrees;
                if (degDiff < 0)
                {
                    angDiff = clamp(((segmentAngle - degDiff) / _fadeOutDegrees), 0, 1);
                }
                //float arcLen = (2 * (DistToSegmentEnd * DistToSegmentEnd)) - (2 * DistToSegmentEnd * DistToSegmentEnd * cos(radians(_fadeOutDegrees)));
                        
                if (previousCone.cutShort)
                {
                        
                    if (currentCone.cutShort)
                    {
                        softnewBlurDistance = 0;
                        //softDistToSegmentEnd = 0;
                    }
                    if ((c == 0 || c == circle.numSegments - 1))
                    {
                        softnewBlurDistance = DistToSegmentEnd - circle.circleRadius;
                        softDistToSegmentEnd = circle.circleRadius;
                    }
                            
                    if (DistToSegmentEnd > circle.circleRadius)
                    {
                        softnewBlurDistance = DistToSegmentEnd - circle.circleRadius;
                        softDistToSegmentEnd = circle.circleRadius;
                    }
                    //if (currentCone.cutShort && !(c == 0 || c == circle.numSegments-1))
                    //{
                        //softnewBlurDistance = 0;
                        //softDistToSegmentEnd = 0;
                    //}
                }
                else
                {
                    softDistToSegmentEnd = min(previousCone.length, currentCone.length);
                }
                //softnewBlurDistance+= arcLen;

                if (distToRevealerOrigin < softDistToSegmentEnd + softnewBlurDistance)
                {
                    //if (distToRevealerOrigin < softDistToSegmentEnd)
                    //{
                    //    //RevealerOut = max(RevealerOut, cos(angDiff * 1.570796));
                    //    RevealerOut = max(RevealerOut, SmoothValue((1-angDiff)));
                    //}
                    //else
                    ////RevealerOut = max(RevealerOut, lerp(0, cos(angDiff * 1.570796), clamp(((softDistToSegmentEnd + _fadeOutDistance) - distToRevealerOrigin) / _fadeOutDistance, 0, 1)));
                    
                    half b = CalculateFadeZonePercent(softDistToSegmentEnd, _fadeOutDistance, distToRevealerOrigin);
                    half x = (1-angDiff);
                    RevealerOut = max(RevealerOut, SmoothValue(b) * SmoothValue(x) );
                }
                previousCone = currentCone;
                continue;
            }
#endif
            //Out = 1;
            //previousCone = currentCone;
            //continue;
            //if (abs(previousCone.length - circle.circleRadius) > .001 || abs(currentCone.length - circle.circleRadius) > .001)
            half finalMultiplier = 1;
            if (previousCone.cutShort && currentCone.cutShort)  //draw straight line thru points
            {
                half2 start = circle.circleOrigin + half2(cos(radians(prevAng)), sin(radians(prevAng))) * previousCone.length;
                half2 end = circle.circleOrigin + half2(cos(radians(currentCone.edgeAngle)), sin(radians(currentCone.edgeAngle))) * currentCone.length;
                        
                half a1 = end.y - start.y;
                half b1 = start.x - end.x;
                half c1 = a1 * start.x + b1 * start.y;
                    
                //half a2 = Position.y - circle.circleOrigin.y;
                half a2 = relativePosition.y;
                //half b2 = circle.circleOrigin.x - Position.x;
                half b2 = -relativePosition.x;
                half c2 = a2 * circle.circleOrigin.x + b2 * circle.circleOrigin.y;
                    
                half determinant = (a1 * b2) - (a2 * b1);
                    
                half x = (b2 * c1 - b1 * c2) / determinant;
                half y = (a1 * c2 - a2 * c1) / determinant;
                    
                half2 intercection = half2(x, y);
                DistToSegmentEnd = distance(intercection, circle.circleOrigin);
                        
                finalMultiplier = CalculateFadeZonePercent(_extraRadius, _edgeSoftenDistance, distToRevealerOrigin - DistToSegmentEnd);
                finalMultiplier = saturate(finalMultiplier);
                        
                newBlurDistance = 0;
                if (DistToSegmentEnd > circle.circleRadius)
                {
                    newBlurDistance = max(0, DistToSegmentEnd - circle.circleRadius);
                    DistToSegmentEnd = circle.circleRadius;
                }
                newBlurDistance += _edgeSoftenDistance;
                        
                //_fadeOutDistance = _edgeSoftenDistance;
                //DistToSegmentEnd += _extraRadius;
                newBlurDistance += _extraRadius;
                        
                if (BLEED)
                {
                    //to add the cone
                    half2 rotPoint = (start + end) / 2;
                    half2 arcOrigin = rotPoint + (half2(-(end.y - rotPoint.y), (end.x - rotPoint.x)) * 3);
                    half arcLength = distance(start, arcOrigin);
                    half2 newRelativePosition = arcOrigin + normalize(Position - arcOrigin) * arcLength;
                    newBlurDistance += distance(intercection, newRelativePosition) / 2;
                }
            }
            newBlurDistance = min(newBlurDistance, circle.circleRadius + _fadeOutDistance);
            DistToSegmentEnd = max(DistToSegmentEnd, circle.unobscuredRadius);
            DistToSegmentEnd = min(DistToSegmentEnd, circle.circleRadius);
                    
            if (distToRevealerOrigin < DistToSegmentEnd + newBlurDistance)
            {
                //if (distToRevealerOrigin < DistToSegmentEnd)
                //{
                //    RevealerOut = 1;
                //    break;
                //}
                RevealerOut = max(RevealerOut, SmoothValue(CalculateFadeZonePercent(DistToSegmentEnd, _fadeOutDistance, distToRevealerOrigin)) * finalMultiplier);
                previousCone = currentCone;
            }
            if (IsOne(RevealerOut)) 
                break;
        }
                
        previousCone = currentCone;
    }
}

void FOW_Soft(float2 Position, float height, out float Out)
{
    Out = 0;
    if (_pixelateWS)
    {
        Position *= _pixelDensity;
        Position -= _pixelOffset;
        Position = round(Position);
        Position += _pixelOffset;
        Position /= _pixelDensity;
    }
#if SAMPLE_REALTIME
#else
    return;
#endif
    for (int i = 0; i < _NumRevealers; i++)
    {
        CircleStruct circle = _CircleBuffer[_ActiveCircleIndices[i]];
        half2 relativePosition = Position - circle.circleOrigin;
        if (_pixelate)
        {
            relativePosition *= _pixelDensity;
            relativePosition = round(relativePosition);
            relativePosition /= _pixelDensity;
        }
        half distToRevealerOrigin = length(relativePosition);
        //float distToRevealerOrigin = distance(Position, circle.circleOrigin);
        half _fadeOutDistance = circle.circleFade;
        if (distToRevealerOrigin < circle.circleRadius + _fadeOutDistance)
        {
            half RevealerOut = 0;
#if IGNORE_HEIGHT
            half heightDist = 0;
#else
            half heightDist = abs(height - circle.circleHeight);
#endif

            if (heightDist > circle.visionHeight)
            {
                if (heightDist > circle.visionHeight + circle.heightFade)
                    continue;
                heightDist = 1 - (heightDist - circle.visionHeight) / circle.heightFade;
            }
            else
                heightDist = 1;

            if (circle.unobscuredRadius < 0)    //negative unobscured radius
            {
                if (distToRevealerOrigin < -circle.unobscuredRadius + _unboscuredFadeOutDistance)
                {
                    if (distToRevealerOrigin < -circle.unobscuredRadius)
                        continue;
                    //Out = max(Out, heightDist * lerp(1, 0, (distToRevealerOrigin - circle.unobscuredRadius) / _unboscuredFadeOutDistance));
                    heightDist *= (distToRevealerOrigin - -circle.unobscuredRadius) / _unboscuredFadeOutDistance;
                }
            }

            if (distToRevealerOrigin < circle.unobscuredRadius + _unboscuredFadeOutDistance)
            {
                if (distToRevealerOrigin < circle.unobscuredRadius)
                    RevealerOut = 1;
                else
                    RevealerOut = max(RevealerOut, lerp(1, 0, (distToRevealerOrigin - circle.unobscuredRadius) / _unboscuredFadeOutDistance));
            }

            if (!IsOne(RevealerOut))
                LoopRevealerSoftFog(circle, relativePosition, distToRevealerOrigin, Position, RevealerOut);
            
            RevealerOut = clamp(abs(RevealerOut), 0, 1);
            RevealerOut *= circle.revealerOpacity;
            RevealerOut *= heightDist;
            if (BLEND_MAX)
                Out = max(Out, RevealerOut);
            else
                Out = min(1, Out + RevealerOut);
            
            if (IsOne(Out))
                break;
        }
    }
    
    if (_ditherFog)
        Dither(Out, abs(Position + float2(5000, 5000)), Out);
}

float2 closestPointOnLine(float2 p1, float2 p2, float2 pnt)
{
    float2 direction = normalize(p1 - p2);
    float2 vec = pnt - p1;
    float dst = dot(vec, direction);
    return p1 + direction * dst;
}

void FOW_Outline_float(float2 Position, float height, out float Out)
{
    Out = 0;
#if SAMPLE_REALTIME
#else
    return;
#endif
    
    for (int i = 0; i < _NumRevealers; i++)
    {
        CircleStruct circle = _CircleBuffer[_ActiveCircleIndices[i]];
        float distToRevealerOrigin = distance(Position, circle.circleOrigin);
        if (distToRevealerOrigin < circle.circleRadius + lineThickness)
        {
#if IGNORE_HEIGHT
            float heightDist = 0;
#else
            float heightDist = abs(height - circle.circleHeight);
#endif

            if (heightDist > circle.visionHeight)
                continue;

//#if MIN_DIST_ON
            if (circle.unobscuredRadius < 0 && distToRevealerOrigin < -circle.unobscuredRadius)
                continue;
//#endif

            float2 relativePosition = Position - circle.circleOrigin;
            float deg = degrees(atan2(relativePosition.y, relativePosition.x));
            
            ConeEdgeStruct previousCone = _ConeBuffer[circle.startIndex];
            //float prevAng = previousCone.edgeAngle - .001;
            //float prevAng = previousCone.edgeAngle + .01;
            float prevAng = previousCone.edgeAngle;
            for (int c = 0; c < circle.numSegments; c++)
            {
                //prevAng = previousCone.edgeAngle - .001;
                //prevAng = previousCone.edgeAngle + .01;
                prevAng = previousCone.edgeAngle;
                ConeEdgeStruct currentCone = _ConeBuffer[circle.startIndex + c];

                if (previousCone.cutShort != currentCone.cutShort)
                {
                    float2 previousPoint = circle.circleOrigin + float2(previousCone.length * cos(radians(previousCone.edgeAngle)), previousCone.length * sin(radians(previousCone.edgeAngle)));
                    float2 currentPoint = circle.circleOrigin + float2(currentCone.length * cos(radians(currentCone.edgeAngle)), currentCone.length * sin(radians(currentCone.edgeAngle)));
                
                    float len = distance(previousPoint, currentPoint) + lineThickness;
                    float dstTop1 = distance(previousPoint, Position);
                    float dstTop2 = distance(currentPoint, Position);
                
                    float2 ClosestPointOnLine = closestPointOnLine(previousPoint, currentPoint, Position);
                    float dstToLine = distance(ClosestPointOnLine, Position);
                
                //dst = distance(currentPoint, Position);
                    if (dstToLine < lineThickness && dstTop1 < len && dstTop2 < len)
                    {
                        Out = 1;
                        return;
                    }
                }
                float degDiff = angleDiff(deg + 360, currentCone.edgeAngle);
                float segmentAngle = currentCone.edgeAngle - prevAng;
                
                //if (deg > prevAng && currentCone.edgeAngle > deg)
                if (degDiff > -segmentAngle && degDiff < 0)
                {
                    //float lerpVal = (deg - prevAng) / (currentCone.edgeAngle - prevAng);
                    //float DistToSegmentEnd = lerp(previousCone.length, currentCone.length, lerpVal);
                    float DistToSegmentEnd = currentCone.length;
                    //if (abs(previousCone.length - circle.circleRadius) > .001 || abs(currentCone.length - circle.circleRadius) > .001)
                    if (previousCone.cutShort && currentCone.cutShort)
                    {
                        float2 start = circle.circleOrigin + float2(cos(radians(prevAng)), sin(radians(prevAng))) * previousCone.length;
                        float2 end = circle.circleOrigin + float2(cos(radians(currentCone.edgeAngle)), sin(radians(currentCone.edgeAngle))) * currentCone.length;
                        
                        float a1 = end.y - start.y;
                        float b1 = start.x - end.x;
                        float c1 = a1 * start.x + b1 * start.y;
                    
                        float a2 = Position.y - circle.circleOrigin.y;
                        float b2 = circle.circleOrigin.x - Position.x;
                        float c2 = a2 * circle.circleOrigin.x + b2 * circle.circleOrigin.y;
                    
                        float determinant = (a1 * b2) - (a2 * b1);
                    
                        float x = (b2 * c1 - b1 * c2) / determinant;
                        float y = (a1 * c2 - a2 * c1) / determinant;
                    
                        float2 intercection = float2(x, y);
                        DistToSegmentEnd = distance(intercection, circle.circleOrigin);
                    }
                    DistToSegmentEnd = max(DistToSegmentEnd, circle.unobscuredRadius);
                    
                    if (distance(distToRevealerOrigin, DistToSegmentEnd) < lineThickness)
                    {
                        Out = 1;
                        return;
                    }
                }
                
                previousCone = currentCone;
            }
        }
    }
}

//shadergraph rotate node
void FOW_Rotate_Degrees_float(float2 UV, float2 Center, float Rotation, out float2 Out)
{
    Rotation = Rotation * (3.1415926f / 180.0f);
    UV -= Center;
    float s = sin(Rotation);
    float c = cos(Rotation);
    float2x2 rMatrix = float2x2(c, -s, s, c);
    rMatrix *= 0.5;
    rMatrix += 0.5;
    rMatrix = rMatrix * 2 - 1;
    UV.xy = mul(UV.xy, rMatrix);
    UV += Center;
    Out = UV;
}

void FOW_Rotate_Degrees_half(half2 UV, half2 Center, half Rotation, out half2 Out)
{
    Rotation = Rotation * (3.1415926f / 180.0f);
    UV -= Center;
    half s = sin(Rotation);
    half c = cos(Rotation);
    half2x2 rMatrix = half2x2(c, -s, s, c);
    rMatrix *= 0.5;
    rMatrix += 0.5;
    rMatrix = rMatrix * 2 - 1;
    UV.xy = mul(UV.xy, rMatrix);
    UV += Center;
    Out = UV;
}

void FOW_Sample_Raw_float(float2 Position, float height, out float Out)
{
    Out = 0;
#if HARD
                FOW_Hard(Position, height, Out);
#elif SOFT
                FOW_Soft(Position, height, Out);
#endif
}

void FOW_Sample_Raw_half(half2 Position, half height, out half Out)
{
    Out = 0;
#if HARD
                FOW_Hard(Position, height, Out);
#elif SOFT
                FOW_Soft(Position, height, Out);
#endif
}

void FOW_Sample_float(float2 Position, float height, out float Out)
{
    FOW_Sample_Raw_float(Position, height, Out);
    CustomCurve_float(Out, Out);
    TextureSample_float(Position, Out);
}

void FOW_Sample_half(half2 Position, half height, out half Out)
{
    Out = 0;
    FOW_Sample_Raw_half(Position, height, Out);
    CustomCurve_half(Out, Out);
    TextureSample_float(Position, Out);
}

void GetFowSpacePosition(float3 PositionWS, out float2 PositionFS, out float height)
{
    [branch]
    switch (_fowPlane)
    {
        case 0: //2D
            PositionFS = PositionWS.xy;
            height = 0;
            return;
        case 1: //XZ
            PositionFS = PositionWS.xz;
            height = PositionWS.y;
            return;
        case 2: //XY
            PositionFS = PositionWS.xy;
            height = PositionWS.z;
            return;
        case 3: //ZY
            PositionFS = PositionWS.zy;
            height = PositionWS.x;
            return;
        default:
            PositionFS = PositionWS.xy;
            height = PositionWS.z;
            return;
    }
}
//used for partial hiders
void FOW_Sample_WS_float(float3 PositionWS, out float Out)
{
    float2 pos = float2(0, 0);
    float height = 0;
    GetFowSpacePosition(PositionWS, pos, height);
    FOW_Sample_float(pos, height, Out);
}

void FOW_Sample_WS_half(float3 PositionWS, out half Out)
{
    half2 pos = float2(0, 0);
    half height = 0;
    GetFowSpacePosition(PositionWS, pos, height);
    FOW_Sample_float(pos, height, Out);
}

#endif