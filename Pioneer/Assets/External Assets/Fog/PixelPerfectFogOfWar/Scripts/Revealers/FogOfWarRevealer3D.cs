using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using System.Runtime.CompilerServices;
#if UNITY_EDITOR
using UnityEngine.Profiling;
using UnityEditor;
#endif

namespace FOW
{
	public class FogOfWarRevealer3D : FogOfWarRevealer
	{
		private NativeArray<RaycastCommand> RaycastCommandsNative;
		private NativeArray<RaycastHit> RaycastHits;
		private NativeArray<float3> Vector3Directions;
		private JobHandle IterationOneJobHandle;
		private Phase1SetupJob SetupJob;
		private JobHandle SetupJobJobHandle;
		private GetVector2Data DataJob;
		private JobHandle Vector2NormalJobHandle;
		private PhysicsScene physicsScene;
#if UNITY_2022_2_OR_NEWER
		public QueryParameters RayQueryParameters;
#endif
		protected override void _InitRevealer(int StepCount)
		{
            physicsScene = gameObject.scene.GetPhysicsScene();

            //if (RaycastCommands != null)
            if (RaycastCommandsNative.IsCreated)
				CleanupRevealer();

			//RaycastCommands = new RaycastCommand[StepCount];
			RaycastCommandsNative = new NativeArray<RaycastCommand>(StepCount, Allocator.Persistent);
			RaycastHits = new NativeArray<RaycastHit>(StepCount, Allocator.Persistent);
			Vector3Directions = new NativeArray<float3>(StepCount, Allocator.Persistent);

#if UNITY_2022_2_OR_NEWER
			RayQueryParameters = new QueryParameters(ObstacleMask, false, QueryTriggerInteraction.UseGlobal, false);
#endif
            SetupJob = new Phase1SetupJob()
			{
				GamePlane = (int)FogOfWarWorld.instance.gamePlane,
				RayAngles = FirstIteration.RayAngles,
				Vector3Directions = Vector3Directions,
				RaycastCommandsNative = RaycastCommandsNative,
			};

			DataJob = new GetVector2Data()
			{
				GamePlane = (int)FogOfWarWorld.instance.gamePlane,
				RaycastHits = RaycastHits,
				Hits = FirstIteration.Hits,
				Distances = FirstIteration.Distances,

				InDirections = Vector3Directions,
				OutPoints = FirstIteration.Points,
				OutDirections = FirstIteration.Directions,
				OutNormals = FirstIteration.Normals
			};
			for (int i = 0; i < StepCount; i++)
            {
				//RaycastCommands[i] = new RaycastCommand(Vector3.zero, Vector3.up, layerMask: ObstacleMask);
				//RaycastCommands[i].layerMask = ObstacleMask;
			}
		}

		protected override void CleanupRevealer()
        {
			if (!RaycastCommandsNative.IsCreated)
				return;
			RaycastCommandsNative.Dispose();
			RaycastHits.Dispose();
			Vector3Directions.Dispose();
		}
		
		protected override void IterationOne(int NumSteps, float firstAngle, float angleStep)
        {
#if UNITY_EDITOR
			Profiler.BeginSample("pt1");	//if this is taking a super long time on some frames only, update unity!
#endif
			SetupJob.FirstAngle = firstAngle;
			SetupJob.AngleStep = angleStep;
			SetupJob.RayDistance = RayDistance;
			SetupJob.EyePosition = EyePosition;
#if UNITY_2022_2_OR_NEWER
			RayQueryParameters.layerMask = ObstacleMask;
			SetupJob.Parameters = RayQueryParameters;
#else
			SetupJob.LayerMask = ObstacleMask;
#endif
			SetupJobJobHandle = SetupJob.Schedule(NumSteps, CommandsPerJob, default(JobHandle));

#if UNITY_EDITOR
			if (DebugMode && DrawInitialRays)
			{
				SetupJobJobHandle.Complete();
				for (int i = 0; i < NumSteps; i++)
				{
					Debug.DrawRay(EyePosition, Vector3Directions[i] * RayDistance, Color.white);
				}
			}
#endif

			//IterationOneJobHandle = RaycastCommand.ScheduleBatch(RaycastCommandsNative, RaycastHits, 64);
			//Debug.Log(commandsPerJob);
			IterationOneJobHandle = RaycastCommand.ScheduleBatch(RaycastCommandsNative, RaycastHits, CommandsPerJob, SetupJobJobHandle);
			//JobHandle.ScheduleBatchedJobs();

			//IterationOneJobHandle.Complete();
#if UNITY_EDITOR
			Profiler.EndSample();
			Profiler.BeginSample("pt2");
#endif
			//DataJob.RayDistance = ViewRadius;
			DataJob.RayDistance = RayDistance;
			DataJob.EyePosition = EyePosition;
			//Vector2NormalJobHandle = DataJob.Schedule(NumSteps, 32, IterationOneJobHandle);
			Vector2NormalJobHandle = DataJob.Schedule(NumSteps, CommandsPerJob, IterationOneJobHandle);
			//Vector2NormalJobHandle.Complete();
#if UNITY_EDITOR
			Profiler.EndSample();
#endif


			//PointsJobHandle = PointsJob.Schedule(NumSteps, 32);
			PointsJobHandle = PointsJob.Schedule(NumSteps, CommandsPerJob, Vector2NormalJobHandle);
			//PointsJobHandle.Complete();	//now called in phase 2
		}

		[BurstCompile]
		struct Phase1SetupJob : IJobParallelFor
        {
			public int GamePlane;
			public float FirstAngle;
			public float AngleStep;
			public float RayDistance;
			public Vector3 EyePosition;
#if UNITY_2022_2_OR_NEWER
			public QueryParameters Parameters;
#else
			public int LayerMask;
#endif
			[WriteOnly] public NativeArray<float> RayAngles;
			[WriteOnly] public NativeArray<float3> Vector3Directions;
			[WriteOnly] public NativeArray<RaycastCommand> RaycastCommandsNative;
			public PhysicsScene PhysicsScene;
			public void Execute(int id)
            {
				float angle = FirstAngle + (AngleStep * id);
				RayAngles[id] = angle;
				float3 dir = DirFromAngle(angle);
				Vector3Directions[id] = dir;

#if UNITY_2022_2_OR_NEWER
				RaycastCommandsNative[id] = new RaycastCommand(EyePosition, dir, Parameters, RayDistance);
#else
				RaycastCommandsNative[id] = new RaycastCommand(EyePosition, dir, RayDistance, layerMask: LayerMask);
#endif
			}
			float3 DirFromAngle(float angleInDegrees)
			{
				float3 direction = new float3();
				switch (GamePlane)
				{
					case 0:
						direction.x = Mathf.Cos(angleInDegrees * Mathf.Deg2Rad);
						direction.z = Mathf.Sin(angleInDegrees * Mathf.Deg2Rad);
						return direction;
					case 1:
						direction.x = Mathf.Cos(angleInDegrees * Mathf.Deg2Rad);
						direction.y = Mathf.Sin(angleInDegrees * Mathf.Deg2Rad);
						return direction;
					case 2: break;
				}
				direction.z = Mathf.Cos(angleInDegrees * Mathf.Deg2Rad);
				direction.y = Mathf.Sin(angleInDegrees * Mathf.Deg2Rad);
				return direction;
			}
		}
		[BurstCompile]
		struct GetVector2Data : IJobParallelFor
		{
			public int GamePlane;
			public float RayDistance;
			public float3 EyePosition;
			[ReadOnly] public NativeArray<RaycastHit> RaycastHits;
			[WriteOnly] public NativeArray<bool> Hits;
			[WriteOnly] public NativeArray<float> Distances;

			[ReadOnly] public NativeArray<float3> InDirections;
			[WriteOnly] public NativeArray<float2> OutPoints;
			[WriteOnly] public NativeArray<float2> OutDirections;
			[WriteOnly] public NativeArray<float2> OutNormals;
			public void Execute(int id)
			{
				//if (RaycastHits[id].distance)
				float3 point3d;
				float3 normal3d;
				//if (!approximately(RaycastHits[id].distance, RayDistance))
				if (!approximately(RaycastHits[id].distance, 0))
				{
					Hits[id] = true;
					Distances[id] = RaycastHits[id].distance;
					point3d = RaycastHits[id].point;
					normal3d = RaycastHits[id].normal;
				}
				else
				{
					Hits[id] = false;
					Distances[id] = RayDistance;
					point3d = EyePosition + (InDirections[id] * RayDistance);
					normal3d = -InDirections[id];
				}
				float2 point = new float2();
				float2 direction = new float2();
				float2 norm = new float2();
				switch (GamePlane)
				{
					case 0:
						point.x = point3d.x;
						point.y = point3d.z;

						direction.x = InDirections[id].x;
						direction.y = InDirections[id].z;

						norm.x = normal3d.x;
						norm.y = normal3d.z;
						break;
					case 1:
						point.x = point3d.x;
						point.y = point3d.y;

						direction.x = InDirections[id].x;
						direction.y = InDirections[id].y;

						norm.x = normal3d.x;
						norm.y = normal3d.y;
						break;
					case 2:
						point.x = point3d.z;
						point.y = point3d.y;

						direction.x = InDirections[id].z;
						direction.y = InDirections[id].y;

						norm.x = normal3d.z;
						norm.y = normal3d.y;
						break;
				}
				OutPoints[id] = point;
				OutDirections[id] = math.normalize(direction);
				//OutDirections[id] = direction.normalized;
				OutNormals[id] = math.normalize(norm);
				//OutNormals[id] = norm.normalized;
			}

			bool approximately(float a, float b)
			{
				return math.abs(b - a) < math.max(0.000001f * math.max(math.abs(a), math.abs(b)), math.EPSILON * 8);
			}
		}

		protected override void _FindEdge()
        {
			NativeArray<RaycastCommand> EdgeRaycastCommands = new NativeArray<RaycastCommand>(NumberOfPoints, Allocator.TempJob);
			NativeArray<float3> RayDirections = new NativeArray<float3>(NumberOfPoints, Allocator.TempJob);

			NativeArray<SightRay> SightRayArray = new NativeArray<SightRay>(NumberOfPoints, Allocator.TempJob);
			NativeArray<SightSegment> SightSeg = new NativeArray<SightSegment>(ViewPoints, Allocator.TempJob);
			NativeArray<EdgeResolveData> EdgeResolveArray = new NativeArray<EdgeResolveData>(NumberOfPoints, Allocator.TempJob);

			EdgeJob.SightRays = SightRayArray;
			EdgeJob.SightSegments = SightSeg;
			EdgeJob.EdgeData = EdgeResolveArray;
			EdgeJob.EdgeNormals = new NativeArray<float2>(EdgeNormals, Allocator.TempJob);
			EdgeJob.MaxAcceptableEdgeAngleDifference = MaxAcceptableEdgeAngleDifference;
			EdgeJob.DoubleHitMaxAngleDelta = DoubleHitMaxAngleDelta;
			EdgeJob.EdgeDstThreshold = EdgeDstThreshold;

			for (int i = 0; i < NumberOfPoints; i++)
			{
				//EdgeRaycastCommands[i] = new RaycastCommand(EyePosition, Vector3Directions[i], RayDistance, layerMask: ObstacleMask);
				//RayCast(currentAngle, ref currentRay);
				EdgeResolveData data = new EdgeResolveData();
				data.CurrentAngle = ViewPoints[i].Angle;
				data.AngleAdd = EdgeAngles[i];
				data.Sign = 1;

				data.AngleAdd /= 2;
				data.CurrentAngle += data.AngleAdd;
				data.Break = false;
				EdgeResolveArray[i] = data;
			}
			for (int r = 0; r < MaxEdgeResolveIterations; r++)
			{
				//if (EdgeResolveArray[r].Break)
				//	continue;
                for (int i = 0; i < NumberOfPoints; i++)
                {
					//RayCast(currentAngle, ref currentRay);
					RayDirections[i] = DirFromAngle(EdgeResolveArray[i].CurrentAngle, true);
#if UNITY_2022_2_OR_NEWER
					EdgeRaycastCommands[i] = new RaycastCommand(EyePosition, RayDirections[i], RayQueryParameters, RayDistance);
#else
					EdgeRaycastCommands[i] = new RaycastCommand(EyePosition, RayDirections[i], RayDistance, layerMask: ObstacleMask);
#endif
				}
                JobHandle rayCastJobHandle = RaycastCommand.ScheduleBatch(EdgeRaycastCommands, RaycastHits, CommandsPerJob, default(JobHandle));
				SightRayFromRaycastHit SightRayJob = new SightRayFromRaycastHit()
				{
					GamePlane = (int)FogOfWarWorld.instance.gamePlane,
					RayDistance = RayDistance,
					EyePosition = EyePosition,
					RaycastHits = RaycastHits,
					SightRays = SightRayArray,
					InDirections = RayDirections,
				};
                JobHandle SightRayHandle = SightRayJob.Schedule(NumberOfPoints, CommandsPerJob, rayCastJobHandle);
				EdgeJobHandle = EdgeJob.Schedule(NumberOfPoints, CommandsPerJob, SightRayHandle);
                EdgeJobHandle.Complete();
            }
            ViewPoints = SightSeg.ToArray();
			EdgeRaycastCommands.Dispose();
			RayDirections.Dispose();
			SightRayArray.Dispose();
			SightSeg.Dispose();
			EdgeResolveArray.Dispose();
			EdgeJob.EdgeNormals.Dispose();
		}

		[BurstCompile]
		struct SightRayFromRaycastHit : IJobParallelFor
		{
			public int GamePlane;
			public float RayDistance;
			public float3 EyePosition;
			[ReadOnly] public NativeArray<RaycastHit> RaycastHits;
			[ReadOnly] public NativeArray<float3> InDirections;
			[WriteOnly] public NativeArray<SightRay> SightRays;
			public void Execute(int id)
			{
				SightRay ray = new SightRay();
				float3 point3d;
				float3 normal3d;
				if (!approximately(RaycastHits[id].distance, 0))
				{
					ray.hit = true;
					ray.distance = RaycastHits[id].distance;
					point3d = RaycastHits[id].point;
					normal3d = RaycastHits[id].normal;
				}
				else
				{
					ray.hit = false;
					ray.distance = RayDistance;
					point3d = EyePosition + (InDirections[id] * RayDistance);
					normal3d = -InDirections[id];
				}
				float2 point = new float2();
				float2 direction = new float2();
				float2 norm = new float2();
				switch (GamePlane)
				{
					case 0:
						point.x = point3d.x;
						point.y = point3d.z;

						direction.x = InDirections[id].x;
						direction.y = InDirections[id].z;

						norm.x = normal3d.x;
						norm.y = normal3d.z;
						break;
					case 1:
						point.x = point3d.x;
						point.y = point3d.y;

						direction.x = InDirections[id].x;
						direction.y = InDirections[id].y;

						norm.x = normal3d.x;
						norm.y = normal3d.y;
						break;
					case 2:
						point.x = point3d.z;
						point.y = point3d.y;

						direction.x = InDirections[id].z;
						direction.y = InDirections[id].y;

						norm.x = normal3d.z;
						norm.y = normal3d.y;
						break;
				}
				ray.point = point;
				ray.direction = math.normalize(direction);
				//OutDirections[id] = direction.normalized;
				ray.normal = math.normalize(norm);
				//OutNormals[id] = norm.normalized;

				SightRays[id] = ray;
			}

			bool approximately(float a, float b)
			{
				return math.abs(b - a) < math.max(0.000001f * math.max(math.abs(a), math.abs(b)), math.EPSILON * 8);
			}
		}

		RaycastHit RayHit;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override void RayCast(float angle, ref SightRay ray)
        {
			Vector3 direction = DirFromAngle(angle, true);
			ray.angle = angle;
			ray.direction = GetVector2D(direction);
			if (physicsScene.Raycast(EyePosition, direction, out RayHit, RayDistance, ObstacleMask))
            {
				ray.hit = true;
				ray.normal = GetVector2D(RayHit.normal);
				ray.distance = RayHit.distance;
				ray.point = GetVector2D(RayHit.point);
			}
			else
            {
				ray.hit = false;
				//ray.normal = GetVector2D(Vector3.up);
				ray.normal = -ray.direction;
				ray.distance = RayDistance;
				ray.point = GetVector2D(CachedTransform.position) + (ray.direction * RayDistance);
			}
		}

		float2 vec2d;
		float2 GetVector2D(Vector3 vector)
        {
			switch (FogOfWarWorld.instance.gamePlane)
			{
				case FogOfWarWorld.GamePlane.XZ:
					vec2d.x = vector.x;
					vec2d.y = vector.z;
					return vec2d;
				case FogOfWarWorld.GamePlane.XY:
					vec2d.x = vector.x;
					vec2d.y = vector.y;
					return vec2d;
				case FogOfWarWorld.GamePlane.ZY:
					vec2d.x = vector.z;
					vec2d.y = vector.y;
					return vec2d;
			}
			vec2d.x = vector.x;
			vec2d.y = vector.z;
			return vec2d;
        }

		protected override float GetEuler()
        {
            switch (FogOfWarWorld.instance.gamePlane)
            {
                case FogOfWarWorld.GamePlane.XZ: return CachedTransform.eulerAngles.y;
                case FogOfWarWorld.GamePlane.XY:
					Vector3 up = CachedTransform.up;
					up.z = 0;
					up.Normalize();
					float ang = Vector3.SignedAngle(up, Vector3.up, FogOfWarWorld.UpVector);
					return -ang;
					//return -CachedTransform.rotation.eulerAngles.z;
                case FogOfWarWorld.GamePlane.ZY:
					Vector3 upz = CachedTransform.up;
					upz.x = 0;
					upz.Normalize();
					float angz = Vector3.SignedAngle(upz, Vector3.up, FogOfWarWorld.UpVector);
					return -angz;
					//return CachedTransform.eulerAngles.x;
			}
			return CachedTransform.eulerAngles.y;
        }

		public override Vector3 GetEyePosition()
        {
			Vector3 eyePos = CachedTransform.position + FogOfWarWorld.UpVector * EyeOffset;
			if (FogOfWarWorld.instance.PixelateFog && FogOfWarWorld.instance.RoundRevealerPosition)
            {
				eyePos *= FogOfWarWorld.instance.PixelDensity;
				Vector3 PixelGridOffset = new Vector3(FogOfWarWorld.instance.PixelGridOffset.x, 0, FogOfWarWorld.instance.PixelGridOffset.y);
				eyePos -= PixelGridOffset;
				eyePos = (Vector3)(Vector3Int.RoundToInt(eyePos));
				eyePos += PixelGridOffset;
				eyePos /= FogOfWarWorld.instance.PixelDensity;
			}
			return eyePos;
		}

		Vector3 hiderPosition;
		Vector3 revealerOrigin;
		private float unobscuredSightDist;
		protected override void _RevealHiders()
		{
#if UNITY_EDITOR
			Profiler.BeginSample("Revealing Hiders");
#endif
			FogOfWarHider hiderInQuestion;
			//float distToHider;
			//float heightDist = 0;
			EyePosition = GetEyePosition();
			ForwardVectorCached = GetForward();
			float sightDist = ViewRadius;
			if (FogOfWarWorld.instance.UsingSoftening)
				sightDist += RevealHiderInFadeOutZonePercentage * SoftenDistance;

			unobscuredSightDist = UnobscuredRadius;
			if (FogOfWarWorld.instance.UsingSoftening)
				unobscuredSightDist += RevealHiderInFadeOutZonePercentage * FogOfWarWorld.instance.UnobscuredSoftenDistance;

			sightDist = Mathf.Max(sightDist, UnobscuredRadius);
			//foreach (FogOfWarHider hiderInQuestion in FogOfWarWorld.HidersList)
			for (int i = 0; i < Mathf.Min(MaxHidersSampledPerFrame, FogOfWarWorld.NumHiders); i++)
			{
				_lastHiderIndex = (_lastHiderIndex + 1) % FogOfWarWorld.NumHiders;
				hiderInQuestion = FogOfWarWorld.HidersList[_lastHiderIndex];
				float minDistToHider = DistBetweenVectors(hiderInQuestion.CachedTransform.position, EyePosition) - hiderInQuestion.MaxDistBetweenSamplePoints;
				bool seen = CanSeeHider(hiderInQuestion, sightDist, minDistToHider);

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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool CanSeeHider(FogOfWarHider hiderInQuestion, float sightDist, float minDistToHider)
        {
			//float minDistToHider = DistBetweenVectors(hiderInQuestion.CachedTransform.position, EyePosition) - hiderInQuestion.MaxDistBetweenSamplePoints;

			if (minDistToHider > sightDist)
				return false;

			for (int j = 0; j < hiderInQuestion.SamplePoints.Length; j++)
            {
				if (CanSeeHiderSamplePoint(hiderInQuestion.SamplePoints[j], sightDist))
					return true;
            }

			return false;
		}

		float heightDist;
		bool CanSeeHiderSamplePoint(Transform samplePoint, float sightDist)
        {
			float distToHider = DistBetweenVectors(samplePoint.position, EyePosition);

			if (distToHider < unobscuredSightDist)
				return true;

			switch (FogOfWarWorld.instance.gamePlane)
			{
				case FogOfWarWorld.GamePlane.XZ: heightDist = Mathf.Abs(EyePosition.y - samplePoint.position.y); break;
				case FogOfWarWorld.GamePlane.XY: heightDist = Mathf.Abs(EyePosition.z - samplePoint.position.z); break;
				case FogOfWarWorld.GamePlane.ZY: heightDist = Mathf.Abs(EyePosition.x - samplePoint.position.x); break;
			}

			if (heightDist > VisionHeight)
				return false;

			//if ((distToHider < sightDist && Mathf.Abs(AngleBetweenVector2(samplePoint.position - EyePosition, ForwardVectorCached)) <= ViewAngle / 2))
			if (Mathf.Abs(AngleBetweenVector2(samplePoint.position - EyePosition, ForwardVectorCached)) <= ViewAngle / 2)
			{
				revealerOrigin = EyePosition;
				if (CalculateHidersAtHiderHeight)
					SetRevealerOrigin(EyePosition, samplePoint.position);

                hiderPosition = samplePoint.position;
                if (SampleHidersAtRevealerHeight)
					SetHiderPosition(samplePoint.position, EyePosition);
				//else
				//	hiderPosition = samplePoint.position;


				if (!physicsScene.Raycast(revealerOrigin, hiderPosition - revealerOrigin, out RayHit, distToHider, ObstacleMask))
				{
#if UNITY_EDITOR
                    if (DrawHiderSamples)
                        Debug.DrawLine(revealerOrigin, hiderPosition, Color.green);
#endif
                    return true;
				}
#if UNITY_EDITOR
                else
				{
					if (DebugLogHiderBlockerName)
						Debug.Log(RayHit.collider.gameObject.name);
                    if (DrawHiderSamples)
					{
                        Debug.DrawLine(revealerOrigin, RayHit.point, Color.green);
                        Debug.DrawLine(RayHit.point, hiderPosition, Color.red);
                    }
                }
#endif
            }

            return false;
		}

		void SetHiderPosition(Vector3 point, Vector3 eyePosition)
        {
			switch (FogOfWarWorld.instance.gamePlane)
			{
				case FogOfWarWorld.GamePlane.XZ:
					//hiderPosition.x = point.x;
					hiderPosition.y = eyePosition.y;
					//hiderPosition.z = point.z;
					break;
				case FogOfWarWorld.GamePlane.XY:
					//hiderPosition.x = point.x;
					//hiderPosition.y = point.y;
					hiderPosition.z = eyePosition.z;
					break;
				case FogOfWarWorld.GamePlane.ZY:
					hiderPosition.x = eyePosition.x;
					//hiderPosition.y = point.y;
					//hiderPosition.z = point.z;
					break;
			}
		}

        void SetRevealerOrigin(Vector3 point, Vector3 _hiderPosition)
        {
            switch (FogOfWarWorld.instance.gamePlane)
            {
                case FogOfWarWorld.GamePlane.XZ:
                    revealerOrigin.y = _hiderPosition.y;
                    break;
                case FogOfWarWorld.GamePlane.XY:
                    revealerOrigin.z = _hiderPosition.z;
                    break;
                case FogOfWarWorld.GamePlane.ZY:
                    revealerOrigin.x = _hiderPosition.x;
                    break;
            }
        }

        protected override bool _TestPoint(Vector3 point)
        {
			float sightDist = ViewRadius;
			if (FogOfWarWorld.instance.UsingSoftening)
				sightDist += RevealHiderInFadeOutZonePercentage * SoftenDistance;

			EyePosition = GetEyePosition();
			ForwardVectorCached = GetForward();
			float distToPoint = DistBetweenVectors(point, EyePosition);
			if (distToPoint < UnobscuredRadius || (distToPoint < sightDist && Mathf.Abs(AngleBetweenVector2(point - EyePosition, ForwardVectorCached)) < ViewAngle / 2))
			{
				SetHiderPosition(point, EyePosition);
				if (!physicsScene.Raycast(EyePosition, hiderPosition - CachedTransform.position, distToPoint, ObstacleMask))
					return true;
			}
			return false;
		}

		protected override void SetCenterAndHeight()
        {
			switch (FogOfWarWorld.instance.gamePlane)
			{
				case FogOfWarWorld.GamePlane.XZ:
					center.x = EyePosition.x;
					center.y = EyePosition.z;
					heightPos = EyePosition.y;
					break;
				case FogOfWarWorld.GamePlane.XY:
					center.x = EyePosition.x;
					center.y = EyePosition.y;
					heightPos = EyePosition.z;
					break;
				case FogOfWarWorld.GamePlane.ZY:
					center.x = EyePosition.z;
					center.y = EyePosition.y;
					heightPos = EyePosition.x;
					break;
			}
		}

		Vector2 vec1;
		Vector2 vec2;
		Vector2 _vec1Rotated90;
		protected override float AngleBetweenVector2(Vector3 _vec1, Vector3 _vec2)
		{
            switch (FogOfWarWorld.instance.gamePlane)
            {
                case FogOfWarWorld.GamePlane.XZ:
                    vec1.x = _vec1.x;
                    vec1.y = _vec1.z;
                    vec2.x = _vec2.x;
                    vec2.y = _vec2.z;
                    break;
                case FogOfWarWorld.GamePlane.XY:
                    vec1.x = _vec1.x;
                    vec1.y = _vec1.y;
                    vec2.x = _vec2.x;
                    vec2.y = _vec2.y;
                    break;
                case FogOfWarWorld.GamePlane.ZY:
                    vec1.x = _vec1.z;
                    vec1.y = _vec1.y;
                    vec2.x = _vec2.z;
                    vec2.y = _vec2.y;
                    break;
            }

			//vec1 = vec1.normalized;
			//vec2 = vec2.normalized;
			_vec1Rotated90.x = -vec1.y;
			_vec1Rotated90.y = vec1.x;
			//Vector2 vec1Rotated90 = new Vector2(-vec1.y, vec1.x);
			float sign = (Vector2.Dot(_vec1Rotated90, vec2) < 0) ? -1.0f : 1.0f;
			return Vector2.Angle(vec1, vec2) * sign;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		float DistBetweenVectors(Vector3 _vec1, Vector3 _vec2)
		{
            switch (FogOfWarWorld.instance.gamePlane)
            {
                case FogOfWarWorld.GamePlane.XZ:
                    vec1.x = _vec1.x;
                    vec1.y = _vec1.z;
                    vec2.x = _vec2.x;
                    vec2.y = _vec2.z;
                    break;
                case FogOfWarWorld.GamePlane.XY:
                    
                    break;
                case FogOfWarWorld.GamePlane.ZY:
                    vec1.x = _vec1.z;
                    vec1.y = _vec1.y;
                    vec2.x = _vec2.z;
                    vec2.y = _vec2.y;
                    break;
            }
            //return Vector2.SqrMagnitude(vec1 - vec2);
            return Vector2.Distance(vec1, vec2);
		}

		Vector3 ForwardVectorCached;
        Vector3 GetForward()
        {
            switch (FogOfWarWorld.instance.gamePlane)
            {
                case FogOfWarWorld.GamePlane.XZ: return CachedTransform.forward;
				case FogOfWarWorld.GamePlane.XY: return CachedTransform.up;
				//case FogOfWarWorld.GamePlane.XY: return new Vector3(-CachedTransform.up.x, CachedTransform.up.y, 0).normalized;
				//case FogOfWarWorld.GamePlane.XY: return -CachedTransform.right;
				case FogOfWarWorld.GamePlane.ZY: return CachedTransform.up;
            }
            return CachedTransform.forward;
        }

		Vector3 direction = Vector3.zero;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
		{
            switch (FogOfWarWorld.instance.gamePlane)
            {
                case FogOfWarWorld.GamePlane.XZ:
                    if (!angleIsGlobal)
                    {
                        angleInDegrees += CachedTransform.eulerAngles.y;
                    }
                    direction.x = Mathf.Cos(angleInDegrees * Mathf.Deg2Rad);
                    direction.z = Mathf.Sin(angleInDegrees * Mathf.Deg2Rad);
                    return direction;
                case FogOfWarWorld.GamePlane.XY:
                    if (!angleIsGlobal)
                    {
                        angleInDegrees += CachedTransform.eulerAngles.z;
                    }
                    direction.x = Mathf.Cos(angleInDegrees * Mathf.Deg2Rad);
                    direction.y = Mathf.Sin(angleInDegrees * Mathf.Deg2Rad);
                    return direction;
                case FogOfWarWorld.GamePlane.ZY: break;
            }
            if (!angleIsGlobal)
            {
                angleInDegrees += CachedTransform.eulerAngles.x;
            }
            direction.z = Mathf.Cos(angleInDegrees * Mathf.Deg2Rad);
            direction.y = Mathf.Sin(angleInDegrees * Mathf.Deg2Rad);
            return direction;
        }

        protected override Vector3 _Get3Dfrom2D(Vector2 pos)
        {
			switch (FogOfWarWorld.instance.gamePlane)
            {
				case FogOfWarWorld.GamePlane.XZ: return new Vector3(pos.x, CachedTransform.position.y, pos.y);
				case FogOfWarWorld.GamePlane.XY: return new Vector3(pos.x, pos.y, CachedTransform.position.z);
				case FogOfWarWorld.GamePlane.ZY: return new Vector3(CachedTransform.position.x, pos.y, pos.x);
			}
			return new Vector3(pos.x, CachedTransform.position.y, pos.y);
		}
    }
}