#ifndef PLAYER_CIRCLE_CUTOUT_INCLUDED
#define PLAYER_CIRCLE_CUTOUT_INCLUDED

float4 _PlayerCutoutParams;
float4 _PlayerCutoutShape;

float PlayerCircleCutoutNoise(float2 pixelPosition)
{
    return frac(sin(dot(pixelPosition, float2(12.9898, 78.233))) * 43758.5453);
}

void ApplyPlayerCircleCutout(float4 positionCS)
{
    if (_PlayerCutoutParams.w < 0.5)
        return;

    float2 screenUV = GetNormalizedScreenSpaceUV(positionCS);
    float2 delta = screenUV - _PlayerCutoutParams.xy;
    delta.x *= _ScreenParams.x / max(_ScreenParams.y, 1.0);

    float radius = max(_PlayerCutoutShape.x, 0.0001);
    float softness = max(_PlayerCutoutShape.y, 0.0001);
    float depthPadding = max(_PlayerCutoutShape.z, 0.0);
    float distanceToCenter = length(delta);

    float fragmentDepth = positionCS.w;
    float isInFrontOfTarget = step(fragmentDepth, _PlayerCutoutParams.z + depthPadding);
    float keepAmount = smoothstep(max(radius - softness, 0.0), radius, distanceToCenter);
    float noise = PlayerCircleCutoutNoise(floor(screenUV * _ScreenParams.xy));

    clip(lerp(1.0, keepAmount - noise, isInFrontOfTarget));
}

#endif
