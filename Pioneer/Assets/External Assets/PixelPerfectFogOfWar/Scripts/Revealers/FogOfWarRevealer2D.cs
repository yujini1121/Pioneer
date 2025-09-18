using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.Profiling;
#endif

namespace FOW
{
    public class FogOfWarRevealer2D : FogOfWarRevealer
    {
        private RaycastHit2D[] InitialRayResults;
        private PhysicsScene2D physicsScene2D;
        
        protected override void _InitRevealer(int StepCount)
        {
            InitialRayResults = new RaycastHit2D[StepCount];
            physicsScene2D = gameObject.scene.GetPhysicsScene2D();
        }

        protected override void CleanupRevealer()
        {

        }

        protected override void IterationOne(int NumSteps, float firstAngle, float angleStep)
        {
            for (int i = 0; i < NumSteps; i++)
            {
                FirstIteration.RayAngles[i] = firstAngle + (angleStep * i);
                FirstIteration.Directions[i] = DirectionFromAngle(FirstIteration.RayAngles[i], true);
                RayHit = physicsScene2D.Raycast(EyePosition, FirstIteration.Directions[i], RayDistance, ObstacleMask);
                if (RayHit.collider != null)
                {
                    FirstIteration.Hits[i] = true;
                    FirstIteration.Normals[i] = RayHit.normal;
                    FirstIteration.Distances[i] = RayHit.distance;
                    FirstIteration.Points[i] = RayHit.point;
                }
                else
                {
                    FirstIteration.Hits[i] = false;
                    FirstIteration.Normals[i] = -FirstIteration.Directions[i];
                    FirstIteration.Distances[i] = RayDistance;
                    FirstIteration.Points[i] = GetPositionxy(EyePosition) + FirstIteration.Directions[i] * RayDistance;
                }
            }

            PointsJobHandle = PointsJob.Schedule(NumSteps, CommandsPerJob, default(JobHandle));
        }

        private RaycastHit2D RayHit;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void RayCast(float angle, ref SightRay ray)
        {
            Vector2 direction = DirectionFromAngle(angle, true);
            ray.angle = angle;
            ray.direction = direction;
            RayHit = physicsScene2D.Raycast(EyePosition, direction, RayDistance, ObstacleMask);

            if (RayHit.collider != null)
            {
                ray.hit = true;
                ray.normal = RayHit.normal;
                ray.distance = RayHit.distance;
                ray.point = RayHit.point;
            }
            else
            {
                ray.hit = false;
                ray.normal = -direction;
                ray.distance = RayDistance;
                ray.point = GetPositionxy(EyePosition) + ray.direction * RayDistance;
            }
        }

        private float2 pos2d;
        private float2 GetPositionxy(Vector3 pos)
        {
            pos2d.x = pos.x;
            pos2d.y = pos.y;
            return pos2d;
        }

        protected override void _FindEdge()
        {

        }

        protected override float GetEuler()
        {
            Vector3 up = transform.up;
            up.z = 0;
            up.Normalize();
            float ang = Vector3.SignedAngle(up, Vector3.up, -Vector3.forward);
            return -ang;
            //return transform.eulerAngles.z;
        }

        public override Vector3 GetEyePosition()
        {
            Vector3 eyePos = transform.position;
            if (FogOfWarWorld.instance.PixelateFog && FogOfWarWorld.instance.RoundRevealerPosition)
            {
                eyePos *= FogOfWarWorld.instance.PixelDensity;
                Vector3 PixelGridOffset = new Vector3(FogOfWarWorld.instance.PixelGridOffset.x, FogOfWarWorld.instance.PixelGridOffset.y, 0);
                eyePos -= PixelGridOffset;
                eyePos = (Vector3)(Vector3Int.RoundToInt(eyePos));
                eyePos += PixelGridOffset;
                eyePos /= FogOfWarWorld.instance.PixelDensity;
            }
            return eyePos;
        }

        Vector3 hiderPosition;
        private float unobscuredSightDist;
        protected override void _RevealHiders()
        {
#if UNITY_EDITOR
            Profiler.BeginSample("Revealing Hiders");
#endif
            FogOfWarHider hiderInQuestion;
            float distToHider;
            EyePosition = GetEyePosition();
            ForwardVectorCached = GetForward();
            float sightDist = ViewRadius;
            if (FogOfWarWorld.instance.UsingSoftening)
                sightDist += RevealHiderInFadeOutZonePercentage * SoftenDistance;

            unobscuredSightDist = UnobscuredRadius;
            if (FogOfWarWorld.instance.UsingSoftening)
                unobscuredSightDist += RevealHiderInFadeOutZonePercentage * FogOfWarWorld.instance.UnobscuredSoftenDistance;

            //foreach (FogOfWarHider hiderInQuestion in FogOfWarWorld.HidersList)
            for (int i = 0; i < Mathf.Min(MaxHidersSampledPerFrame, FogOfWarWorld.NumHiders); i++)
            {
                _lastHiderIndex = (_lastHiderIndex + 1) % FogOfWarWorld.NumHiders;
                hiderInQuestion = FogOfWarWorld.HidersList[_lastHiderIndex];
                bool seen = false;
                Transform samplePoint;
                float minDistToHider = distBetweenVectors(hiderInQuestion.transform.position, EyePosition) - hiderInQuestion.MaxDistBetweenSamplePoints;
                if (minDistToHider < UnobscuredRadius || (minDistToHider < sightDist))
                {
                    for (int j = 0; j < hiderInQuestion.SamplePoints.Length; j++)
                    {
                        samplePoint = hiderInQuestion.SamplePoints[j];
                        distToHider = distBetweenVectors(samplePoint.position, EyePosition);
                        if (distToHider < UnobscuredRadius || (distToHider < sightDist && Mathf.Abs(AngleBetweenVector2(samplePoint.position - EyePosition, ForwardVectorCached)) < ViewAngle / 2))
                        {
                            SetHiderPosition(samplePoint.position);
                            if (!physicsScene2D.Raycast(EyePosition, hiderPosition - EyePosition, distToHider, ObstacleMask))
                            {
                                seen = true;
                                break;
                            }
                        }
                    }
                }
                if (UnobscuredRadius < 0 && (minDistToHider + 1.5f * hiderInQuestion.MaxDistBetweenSamplePoints) < -UnobscuredRadius)
                    seen = false;

                if (seen)
                {
                    if (!HidersSeen.Contains(hiderInQuestion))
                    {
                        HidersSeen.Add(hiderInQuestion);
                        hiderInQuestion.AddObserver(this);
                    }
                }
                else
                {
                    if (HidersSeen.Contains(hiderInQuestion))
                    {
                        HidersSeen.Remove(hiderInQuestion);
                        hiderInQuestion.RemoveObserver(this);
                    }
                }
            }
#if UNITY_EDITOR
            Profiler.EndSample();
#endif
        }

        void SetHiderPosition(Vector3 point)
        {
            hiderPosition.x = point.x;
            hiderPosition.y = point.y;
            //hiderPosition.z = getEyePos().z;
        }

        protected override bool _TestPoint(Vector3 point)
        {
            float sightDist = ViewRadius;
            if (FogOfWarWorld.instance.UsingSoftening)
                sightDist += RevealHiderInFadeOutZonePercentage * SoftenDistance;

            EyePosition = GetEyePosition();
            float distToPoint = distBetweenVectors(point, EyePosition);
            if (distToPoint < UnobscuredRadius || (distToPoint < sightDist && Mathf.Abs(AngleBetweenVector2(point - EyePosition, GetForward())) < ViewAngle / 2))
            {
                SetHiderPosition(point);
                if (!physicsScene2D.Raycast(EyePosition, hiderPosition - transform.position, distToPoint, ObstacleMask))
                    return true;
            }
            return false;
        }

        protected override void SetCenterAndHeight()
        {
            center.x = EyePosition.x;
            center.y = EyePosition.y;
            heightPos = transform.position.z;
        }

        Vector2 vec1;
        Vector2 vec2;
        Vector2 _vec1Rotated90;
        protected override float AngleBetweenVector2(Vector3 _vec1, Vector3 _vec2)
        {
            vec1.x = _vec1.x;
            vec1.y = _vec1.y;
            vec2.x = _vec2.x;
            vec2.y = _vec2.y;

            //vec1 = vec1.normalized;
            //vec2 = vec2.normalized;
            _vec1Rotated90.x = -vec1.y;
            _vec1Rotated90.y = vec1.x;
            //Vector2 vec1Rotated90 = new Vector2(-vec1.y, vec1.x);
            float sign = (Vector2.Dot(_vec1Rotated90, vec2) < 0) ? -1.0f : 1.0f;
            return Vector2.Angle(vec1, vec2) * sign;
        }
        float distBetweenVectors(Vector3 _vec1, Vector3 _vec2)
        {
            vec1.x = _vec1.x;
            vec1.y = _vec1.y;
            vec2.x = _vec2.x;
            vec2.y = _vec2.y;
            return Vector2.Distance(vec1, vec2);
        }

        Vector3 ForwardVectorCached;
        Vector3 GetForward()
        {
            return new Vector3(transform.up.x, transform.up.y, 0).normalized;
            //return new Vector3(-transform.up.x, transform.up.y, 0).normalized;
        }

        RaycastHit2D rayHit;

        Vector2 direction2d = Vector3.zero;
        Vector2 DirectionFromAngle(float angleInDegrees, bool angleIsGlobal)
        {
            if (!angleIsGlobal)
            {
                angleInDegrees += transform.eulerAngles.z;
            }
            direction2d.x = Mathf.Cos(angleInDegrees * Mathf.Deg2Rad);
            direction2d.y = Mathf.Sin(angleInDegrees * Mathf.Deg2Rad);
            return direction2d;
        }

        Vector3 direction = Vector3.zero;
        public override Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
        {
            if (!angleIsGlobal)
            {
                angleInDegrees += transform.eulerAngles.z;
            }
            direction.x = Mathf.Cos(angleInDegrees * Mathf.Deg2Rad);
            direction.y = Mathf.Sin(angleInDegrees * Mathf.Deg2Rad);
            return direction;
        }

        protected override Vector3 _Get3Dfrom2D(Vector2 pos)
        {
            return new Vector3(pos.x, pos.y, 0);
        }
    }
}
