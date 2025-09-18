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
    public abstract class FogOfWarRevealer : MonoBehaviour
    {
        [Header("Customization Variables")]
        [SerializeField] public float ViewRadius = 15;
        [SerializeField] public float SoftenDistance = 3;

        [Range(1f, 360)]
        [SerializeField] public float ViewAngle = 360;

        [SerializeField] public float UnobscuredRadius = 1f;

        [Range(0, 1)]
        [SerializeField] public float Opacity = 1;

        [SerializeField] protected bool AddCorners = true;

        //[SerializeField] public bool RevealHidersInFadeOutZone = true;
        [Range(0,1)]
        [SerializeField] public float RevealHiderInFadeOutZonePercentage = .5f;

        [Tooltip("how high above this object should the sight be calculated from")]
        [SerializeField] public float EyeOffset = 0;
        [Tooltip("An offset used only in the shader, to determine how high above the revealer vision height should be calculated at")]
        [SerializeField] public float ShaderEyeOffset = 0;
        [SerializeField] public float VisionHeight = 3;
        [SerializeField] public float VisionHeightSoftenDistance = 1.5f;
        [SerializeField] public bool SampleHidersAtRevealerHeight = true;
        [SerializeField] public bool CalculateHidersAtHiderHeight = false;
        [SerializeField] protected LayerMask ObstacleMask;

        [Header("Quality Variables")]
        [SerializeField] public float RaycastResolution = .5f;

        public bool ResolveEdge = true;
        [Range(1, 30)]
        [Tooltip("Higher values will lead to more accurate edge detection, especially at higher distances. however, this will also result in more raycasts.")]
        [SerializeField] protected int MaxEdgeResolveIterations = 10;

        [Range(0, 10)]
        [SerializeField] protected int NumExtraIterations = 4;

        [Range(1, 5)]
        [SerializeField] protected int NumExtraRaysOnIteration = 3;
        protected int IterationRayCount;

        [SerializeField] protected int MaxHidersSampledPerFrame = 50;

        [Header("Technical Variables")]
        [Range(.001f, 1)]
        [Tooltip("Lower values will lead to more accurate edge detection, especially at higher distances. however, this will also result in more raycasts.")]
        [SerializeField] protected float MaxAcceptableEdgeAngleDifference = .005f;
        [Range(.001f, 1)]
        [SerializeField] protected float EdgeDstThreshold = 0.1f;
        //[SerializeField] protected float DoubleHitMaxDelta = 0.1f;
        [SerializeField] protected float DoubleHitMaxAngleDelta = 15;

        [Tooltip("Static revealers are revealers that need the sight function to be called manually, similar to the 'Called Elsewhere' option on FOW world. To change this at runtime, use the SetRevealerAsStatic(bool IsStatic) Method.")]
        [SerializeField] public bool StartRevealerAsStatic = false;
        [HideInInspector] public bool StaticRevealer = false;

        [HideInInspector]
        public int FogOfWarID;
        [HideInInspector]
        public int IndexID;

        //local variables
        protected FogOfWarWorld.RevealerStruct CircleStruct;
        protected bool IsRegistered = false;
        public SightSegment[] ViewPoints;
        protected float[] EdgeAngles;
        protected float2[] EdgeNormals;
        [HideInInspector] public int NumberOfPoints;
        [HideInInspector] public float[] Angles;
        [HideInInspector] public float[] Radii;
        [HideInInspector] public bool[] AreHits;

        [Header("Debugging")]
#if UNITY_EDITOR
        [SerializeField] public bool DebugMode = false;
        [SerializeField] public bool DrawInitialRays = false;
        [SerializeField] protected int SegmentTest = 0;
        [SerializeField] public bool DrawExpectedNextPoints = false;
        [SerializeField] protected bool DrawIteritiveRays;
        [SerializeField] protected bool DrawEdgeResolveRays;
        
        [SerializeField] protected bool DrawExtraCastLines;
        [SerializeField] protected bool DrawHiderSamples;
        [SerializeField] protected bool DebugLogHiderBlockerName;
#endif
        public HashSet<FogOfWarHider> HidersSeen = new HashSet<FogOfWarHider>();
        protected Transform CachedTransform;

        public enum RevealerMode
        {
            ConstantDensity,
            EdgeDetect,
        };

        public struct SightRay
        {
            public bool hit;
            public float2 point;
            public float distance;
            public float angle;
            public float2 normal;
            public float2 direction;

            public void SetData(bool _hit, Vector2 _point, float _distance, Vector2 _normal, Vector2 _direction)
            {
                hit = _hit;
                point = _point;
                distance = _distance;
                normal = _normal;
                direction = _direction;
            }
        }
        public struct SightSegment
        {
            public float Radius;
            public float Angle;
            public bool DidHit;

            public float2 Point;
            public float2 Direction;
            public SightSegment(float rad, float ang, bool hit, float2 point, float2 dir)
            {
                Radius = rad;
                Angle = ang;
                DidHit = hit;
                Point = point;
                Direction = dir;
            }
        }

        private void OnEnable()
        {
            RegisterRevealer();
        }

        private void OnDisable()
        {
            DeregisterRevealer();
            Cleanup();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        public void RegisterRevealer()
        {
            CachedTransform = transform;
            if (StartRevealerAsStatic)
                SetRevealerAsStatic(true);
            else
                SetRevealerAsStatic(false);     //fail-safe in case someone changes the value in debug mode

            NumberOfPoints = 0;
            if (FogOfWarWorld.instance == null)
            {
                if (!FogOfWarWorld.RevealersToRegister.Contains(this))
                {
                    FogOfWarWorld.RevealersToRegister.Add(this);
                }
                return;
            }
            if (IsRegistered)
            {
                Debug.Log("Tried to double register revealer");
                return;
            }
            ViewPoints = new SightSegment[FogOfWarWorld.instance.MaxPossibleSegmentsPerRevealer];
            EdgeAngles = new float[FogOfWarWorld.instance.MaxPossibleSegmentsPerRevealer];
            EdgeNormals = new float2[FogOfWarWorld.instance.MaxPossibleSegmentsPerRevealer];

            Angles = new float[ViewPoints.Length];
            Radii = new float[ViewPoints.Length];
            AreHits = new bool[ViewPoints.Length];

            IsRegistered = true;
            FogOfWarID = FogOfWarWorld.instance.RegisterRevealer(this);
            CircleStruct = new FogOfWarWorld.RevealerStruct();
            LineOfSightPhase1();
            LineOfSightPhase2();
            //_RegisterRevealer();
        }

        public void DeregisterRevealer()
        {
            if (FogOfWarWorld.instance == null)
            {
                if (FogOfWarWorld.RevealersToRegister.Contains(this))
                {
                    FogOfWarWorld.RevealersToRegister.Remove(this);
                }
                return;
            }
            if (!IsRegistered)
            {
                //Debug.Log("Tried to de-register revealer thats not registered");
                return;
            }
            foreach (FogOfWarHider hider in HidersSeen)
            {
                hider?.RemoveObserver(this);
            }
            HidersSeen.Clear();
            IsRegistered = false;
            FogOfWarWorld.instance.DeRegisterRevealer(this);
        }

        /// <summary>
        /// Marks this revealer as static. prevents automatic recalculation of Line Of Sight.
        /// </summary>
        public void SetRevealerAsStatic(bool IsStatic)
        {
            if (IsRegistered)
            {
                if (StaticRevealer && !IsStatic)
                    FogOfWarWorld.numDynamicRevealers++;
                else if (!StaticRevealer && IsStatic)
                    FogOfWarWorld.numDynamicRevealers--;
            }
            
            StaticRevealer = IsStatic;
        }

        /// <summary>
        /// Manually calculate line of sight for this revealer.
        /// </summary>
        public void ManualCalculateLineOfSight()
        {
            LineOfSightPhase1();    //if possible, call phase 1 early in the frame, and phase 2 later in the frame!
            LineOfSightPhase2();
        }

        protected int _lastHiderIndex;
        protected abstract void _RevealHiders();
        public void RevealHiders()
        {
            _RevealHiders();
        }

        protected abstract bool _TestPoint(Vector3 point);
        public bool TestPoint(Vector3 point)
        {
            return _TestPoint(point);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void AddViewPoint(bool hit, float distance, float angle, float step, float2 normal, float2 point, float2 dir)
        {
//#if UNITY_EDITOR
//            Profiler.BeginSample("Add View Point");
//#endif
            if (NumberOfPoints == ViewPoints.Length)
            {
                Debug.LogError("Sight Segment buffer is full! Increase Maximum Segments per Revealer on Fog Of War World!");
                return;
            }
                
            ViewPoints[NumberOfPoints].DidHit = hit;
            ViewPoints[NumberOfPoints].Radius = distance;
            ViewPoints[NumberOfPoints].Angle = angle;

            ViewPoints[NumberOfPoints].Point = point;
            ViewPoints[NumberOfPoints].Direction = dir;

            EdgeAngles[NumberOfPoints] = -step;
            EdgeNormals[NumberOfPoints] = normal;
            NumberOfPoints++;
//#if UNITY_EDITOR
//            Profiler.EndSample();
//#endif
        }

        protected float heightPos;
        protected Vector2 center = new Vector2();
        protected abstract void SetCenterAndHeight();
        private void ApplyData()
        {
#if UNITY_EDITOR
            if (DebugMode)
                UnityEngine.Random.InitState(1);
#endif

            for (int i = 0; i < NumberOfPoints; i++)
            {
                Angles[i] = ViewPoints[i].Angle;
                AreHits[i] = ViewPoints[i].DidHit;
                if (!AreHits[i])
                    ViewPoints[i].Radius = Mathf.Min(ViewPoints[i].Radius, ViewRadius);
                Radii[i] = ViewPoints[i].Radius;
                if (i == NumberOfPoints - 1 && CircleIsComplete)
                {
                    Angles[i] += 360;
                }
            }

            SetCenterAndHeight();
            
            CircleStruct.CircleOrigin = center;
            CircleStruct.NumSegments = NumberOfPoints;
            CircleStruct.UnobscuredRadius = UnobscuredRadius;
            CircleStruct.CircleHeight = heightPos + ShaderEyeOffset;
            CircleStruct.CircleRadius = ViewRadius;
            CircleStruct.CircleFade = SoftenDistance;
            CircleStruct.VisionHeight = VisionHeight;
            CircleStruct.HeightFade = VisionHeightSoftenDistance;
            CircleStruct.Opacity = Opacity;

            FogOfWarWorld.instance.UpdateRevealerData(FogOfWarID, CircleStruct, NumberOfPoints, Angles, Radii, AreHits);
        }

        protected abstract float GetEuler();
        public abstract Vector3 GetEyePosition();
        public abstract Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal);
        protected abstract float AngleBetweenVector2(Vector3 _vec1, Vector3 _vec2);
        protected bool CircleIsComplete;
        
        protected bool Initialized;
        protected Vector3 EyePosition;
        protected int FirstIterationStepCount;
        protected SightIteration FirstIteration;

        protected int CommandsPerJob;
        protected CalculateNextPoints PointsJob;
        protected JobHandle PointsJobHandle;
        public NativeArray<bool> FirstIterationConditions;
        public ConditionCalculations FirstIterationConditionsJob;
        public JobHandle FirstIterationConditionsJobHandle;

        protected float RayDistance;
        public float GetRayDistance() { return RayDistance; }
        protected abstract void _InitRevealer(int StepCount);
        void InitRevealer(int StepCount, float AngleStep)
        {
            //if (FirstIteration.Distances.IsCreated)
            if (FirstIteration != null && FirstIteration.Distances.IsCreated)
                Cleanup();
            for (int i = 0; i < ViewPoints.Length; i++)
                ViewPoints[i] = new SightSegment();
            //InitialPoints = new SightRay[StepCount];
            FirstIterationStepCount = StepCount;
            FirstIteration = new SightIteration();
            FirstIteration.InitializeStruct(StepCount);
            IterationRayCount = NumExtraRaysOnIteration + 2;

            //CommandsPerJob = Mathf.Max(StepCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerCount, 1);
            PointsJob = new CalculateNextPoints()
            {
                UpVector = FogOfWarWorld.UpVector,
                AngleStep = AngleStep,
                Distances = FirstIteration.Distances,
                Points = FirstIteration.Points,
                Directions = FirstIteration.Directions,
                Normals = FirstIteration.Normals,
                ExpectedNextPoints = FirstIteration.NextPoints,
            };
            FirstIterationConditions = new NativeArray<bool>(NumSteps, Allocator.Persistent);
            FirstIterationConditionsJob = new ConditionCalculations()
            {
                Points = FirstIteration.Points,
                NextPoints = FirstIteration.NextPoints,
                Normals = FirstIteration.Normals,
                Hits = FirstIteration.Hits,
                IterateConditions = FirstIterationConditions
            };
            EdgeJob = new FindEdgeJob()
            {

            };
            Initialized = true;
            _InitRevealer(StepCount);
        }

        protected abstract void CleanupRevealer();
        void Cleanup()
        {
            Initialized = false;
            if (FirstIteration == null || !FirstIteration.Distances.IsCreated)
                return;

            FirstIteration.DisposeStruct();
            FirstIterationConditions.Dispose();
            foreach (SightIteration s in SubIterations)
                s.DisposeStruct();
            SubIterations.Clear();
            CleanupRevealer();
        }

        protected abstract void IterationOne(int NumSteps, float firstAngle, float angleStep);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract void RayCast(float angle, ref SightRay ray);

        protected SightRay currentRay;
        private int NumSteps;
        private float AngleStep;
        public void LineOfSightPhase1()
        {
            EdgeDstThreshold = Mathf.Max(.001f, EdgeDstThreshold);
            //Debug.Log("PHASE 1");
            CircleIsComplete = Mathf.Approximately(ViewAngle, 360);
            //Debug.Log(Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerCount);
            //CommandsPerJob = Mathf.Max(Mathf.CeilToInt(FirstIterationStepCount / Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerCount), 1);
            CommandsPerJob = 32;

            EyePosition = GetEyePosition();
            RayDistance = ViewRadius;
            if (FogOfWarWorld.instance.UsingSoftening)
                RayDistance += SoftenDistance;
            NumberOfPoints = 0;
#if UNITY_EDITOR
            Profiler.BeginSample("Line Of Sight");
#endif
            NumSteps = Mathf.Max(2, Mathf.CeilToInt(ViewAngle * RaycastResolution));
            AngleStep = ViewAngle / (NumSteps - 1);

            if (!Initialized || FirstIteration == null || FirstIteration.RayAngles == null || FirstIteration.RayAngles.Length != NumSteps)
            {
                InitRevealer(NumSteps, AngleStep);
            }

#if UNITY_EDITOR
            Profiler.BeginSample("Iteration One");
#endif

            float firstAng = ((-GetEuler() + 360 + 90) % 360) - (ViewAngle / 2);
            IterationOne(NumSteps, firstAng, AngleStep);
            ////PointsJobHandle = PointsJob.Schedule(NumSteps, 32);
            //PointsJobHandle = PointsJob.Schedule(NumSteps, CommandsPerJob);
            //PointsJobHandle.Complete();

            FirstIterationConditionsJob.DoubleHitMaxAngleDelta = DoubleHitMaxAngleDelta;
            FirstIterationConditionsJob.EdgeDstThreshold = EdgeDstThreshold;
            FirstIterationConditionsJob.AddCorners = AddCorners;
            FirstIterationConditionsJobHandle = FirstIterationConditionsJob.Schedule(NumSteps, CommandsPerJob, PointsJobHandle);
            JobHandle.ScheduleBatchedJobs();

#if UNITY_EDITOR
            Profiler.EndSample();
            Profiler.EndSample();
#endif
        }

        public void LineOfSightPhase2()
        {
            //Debug.Log("PHASE 2");
#if UNITY_EDITOR
            Profiler.BeginSample("Line Of Sight");
            Profiler.BeginSample("Complete Phase 1 Work");
#endif

            //PointsJobHandle.Complete();
            FirstIterationConditionsJobHandle.Complete();

#if UNITY_EDITOR
            Profiler.EndSample();
            Profiler.BeginSample("Sorting");
#endif
            AddViewPoint(FirstIteration.Hits[0], FirstIteration.Distances[0], FirstIteration.RayAngles[0], 0, FirstIteration.Normals[0], FirstIteration.Points[0], FirstIteration.Directions[0]);
            //AddViewPoint(new ViewCastInfo(InitialPoints[0].hit, InitialPoints[0].point, InitialPoints[0].distance, InitialPoints[0].angle, Normals[0], InitialPoints[0].direction));
            //Debug.Log(Points[0]);
            //Debug.Log(NextPoints[0]);
            //SortData(ref InitialAngles, ref FirstIteration.Hits, ref FirstIteration.Distances, ref FirstIteration.Points, ref FirstIteration.NextPoints, ref FirstIteration.Normals, AngleStep, NumSteps);
            SortData(ref FirstIteration, AngleStep, NumSteps, 0, true);
            

#if UNITY_EDITOR
            Profiler.EndSample();
            Profiler.BeginSample("Extra Iterations");
#endif
            while (InUseIterations.Count > 0)
                SubIterations.Push(InUseIterations.Pop());
            //CAST EXTRA ITERATIONS

#if UNITY_EDITOR
            Profiler.EndSample();
            Profiler.BeginSample("Add Data");
#endif
            //for (int i = 0; i < FirstIteration.NextIterations.Length; i++)
            //{

            //}

            if (NumberOfPoints == 1)
            {
                if (!ViewPoints[0].DidHit && !ViewPoints[1].DidHit)
                    AddViewPoint(false, ViewPoints[0].Radius, ViewPoints[0].Angle + (ViewAngle / 2), -EdgeAngles[0], new float2(0,0), new float2(0,0), new float2(0,0));
            }
            if (CircleIsComplete)
            {
                if ((FirstIteration.Hits[NumSteps - 1] || FirstIteration.Hits[0]) && (Vector2.Distance(FirstIteration.NextPoints[NumSteps - 1], FirstIteration.Points[0]) > .05f))
                    AddViewPoint(FirstIteration.Hits[NumSteps - 1], FirstIteration.Distances[NumSteps - 1], FirstIteration.RayAngles[NumSteps - 1], 0, FirstIteration.Normals[NumSteps - 1], FirstIteration.Points[NumSteps - 1], FirstIteration.Directions[NumSteps - 1]);
                AddViewPoint(FirstIteration.Hits[0], FirstIteration.Distances[0], FirstIteration.RayAngles[0], 0, FirstIteration.Normals[0], FirstIteration.Points[0], FirstIteration.Directions[0]);
            }
            else
            {
                int n = NumSteps - 1;
                AddViewPoint(FirstIteration.Hits[n], FirstIteration.Distances[n], FirstIteration.RayAngles[n], 0, FirstIteration.Normals[n], FirstIteration.Points[n], FirstIteration.Directions[n]);
            }

#if UNITY_EDITOR
            Profiler.EndSample();
            Profiler.BeginSample("Edge Detection");
#endif

            if (ResolveEdge)
                FindEdges();

#if UNITY_EDITOR
            Profiler.EndSample();
            Profiler.EndSample();
#endif
            ApplyData();
        }

        float2 RotatedNormal = new float2();
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SortData(ref SightIteration iteration, float angleStep, int iterationSteps, int iterationNumber, bool FirstIteration = false)
        {
            //Profiler.BeginSample($"-iteration {iterationNumber}");
            float AngleDelta;
            float newAngleStep = angleStep / (IterationRayCount - 1);
            for (int i = 1; i < iterationSteps; i++)
            {
#if UNITY_EDITOR
                if (DebugMode && DrawExpectedNextPoints && !FirstIteration)
                    Debug.DrawLine(Get3Dfrom2D(iteration.Points[i]), Get3Dfrom2D(iteration.NextPoints[i]) + FogOfWarWorld.UpVector * (.03f / (iterationNumber+1)), UnityEngine.Random.ColorHSV());
#endif

                bool cond;
                if (!FirstIteration)
                {
                    //Profiler.BeginSample("Bool Calcs");
                    AngleDelta = AngleBetweenVector2(iteration.Normals[i], iteration.Normals[i - 1]);
                    bool AngleCondition = math.abs(AngleDelta) > DoubleHitMaxAngleDelta;
                    //if (!AddCorners)
                    //    AngleCondition &= AngleDelta < 0;

                    bool DistanceCondition = !Vector2Aprox(iteration.Points[i], iteration.NextPoints[i - 1]);

                    bool SampleCondition = (iteration.Hits[i - 1] || iteration.Hits[i]) &&
                        (DistanceCondition || AngleCondition)
                        || iteration.Hits[i - 1] != iteration.Hits[i];

                    if ((!AddCorners && AngleCondition && AngleDelta > 0) && !DistanceCondition)
                        SampleCondition = false;

                    //Profiler.EndSample();
                    cond = SampleCondition;
                }
                else
                    cond = FirstIterationConditions[i];


                //if (SampleCondition)  //TODO: MOVE DISTANCE CALC INSIDE JOB
                //if (iteration.IterateConditions[i])
                if (cond)
                {
                    //if (!AddCorners && AngleCondition && AngleDelta > 0)
                    //{
                    //    //AddViewPoint(iteration.Hits[i - 1], iteration.Distances[i - 1], iteration.RayAngles[i - 1], -angleStep, iteration.Normals[i - 1], iteration.Points[i - 1]);
                    //    //AddViewPoint(iteration.Hits[i], iteration.Distances[i], iteration.RayAngles[i], angleStep, iteration.Normals[i], iteration.Points[i]);
                    //    if (!DistanceCondition)
                    //        continue;
                    //}
                    if (iterationNumber == NumExtraIterations)
                    {
                        AddViewPoint(iteration.Hits[i - 1], iteration.Distances[i - 1], iteration.RayAngles[i - 1], -angleStep, iteration.Normals[i - 1], iteration.Points[i - 1], iteration.Directions[i - 1]);
                        AddViewPoint(iteration.Hits[i], iteration.Distances[i], iteration.RayAngles[i], angleStep, iteration.Normals[i], iteration.Points[i], iteration.Directions[i]);
                    }
                    else
                    {
                        float initalAngle = iteration.RayAngles[i - 1];
                        //float newAngleStep = angleStep / (IterationRayCount - 1);
                        //iteration.NextIterations[i] = iterate(iterationNumber+1, 0, FirstIteration.RayAngles[i]);
                        //iteration.NextIterations[i] = iterate(iterationNumber + 1, initalAngle, newAngleStep);

                        //Profiler.BeginSample("gather iteration");
                        SightIteration newIter = Iterate(iterationNumber + 1, initalAngle, newAngleStep, ref iteration, i-1);
                        //Profiler.EndSample();

                        SortData(ref newIter, newAngleStep, IterationRayCount, iterationNumber + 1);
                    }
                }
            }
            //Profiler.EndSample();
        }
        Stack<SightIteration> SubIterations = new Stack<SightIteration>();
        Stack<SightIteration> InUseIterations = new Stack<SightIteration>();
        SightIteration GetSubIteration()
        {
            if (SubIterations.Count > 0)
            {
                return SubIterations.Pop();
            }
            SightIteration newInstance = new SightIteration();
            newInstance.InitializeStruct(IterationRayCount);
            return newInstance;
        }

        bool ProfileExtraIterations = false;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        SightIteration Iterate(int iterNumber, float initialAngle, float AngleStep, ref SightIteration PreviousIteration, int PrevIterStartIndex)   //TODO: JOBIFY
        {
#if UNITY_EDITOR
            if (ProfileExtraIterations)
                Profiler.BeginSample($"Iteration {iterNumber + 1}");
#endif
            SightIteration iter = GetSubIteration();
            InUseIterations.Push(iter);
            //float step = AngleStep / (IterationRayCount + 1);
            //step = AngleStep;
            
            iter.RayAngles[0] = PreviousIteration.RayAngles[PrevIterStartIndex];
            iter.Hits[0] = PreviousIteration.Hits[PrevIterStartIndex];
            iter.Distances[0] = PreviousIteration.Distances[PrevIterStartIndex];
            iter.Points[0] = PreviousIteration.Points[PrevIterStartIndex];
            iter.Directions[0] = PreviousIteration.Directions[PrevIterStartIndex];
            iter.Normals[0] = PreviousIteration.Normals[PrevIterStartIndex];

            float2 RotatedNormal = new float2(-iter.Normals[0].y, iter.Normals[0].x);
            float angleC = 180 - (AngleBetweenVector2(RotatedNormal, -iter.Directions[0]) + AngleStep);
            float nextDist = (iter.Distances[0] * math.sin(math.radians(AngleStep))) / Mathf.Sin(math.radians(angleC));
            iter.NextPoints[0] = iter.Points[0] + (RotatedNormal * nextDist);
            //iter.NextPoints[0] = PreviousIteration.NextPoints[PrevIterStartIndex];

            //for (int i = 1; i <= IterationRayCount; i++)
            for (int i = 1; i < IterationRayCount - 1; i++)
            {
                RayCast(initialAngle + AngleStep * i, ref currentRay);
#if UNITY_EDITOR
                if (DebugMode && DrawIteritiveRays)
                {
                    Debug.DrawRay(EyePosition, DirFromAngle(initialAngle + AngleStep * i, true) * 10, Color.red);
                    //Debug.DrawRay(EyePosition, DirFromAngle(currentRay.angle, true) * 10, Color.red);
                }

#endif
                iter.RayAngles[i] = currentRay.angle;
                iter.Hits[i] = currentRay.hit;
                iter.Distances[i] = currentRay.distance;
                iter.Points[i] = currentRay.point;
                iter.Directions[i] = currentRay.direction;
                iter.Normals[i] = currentRay.normal;

                RotatedNormal = new float2(-iter.Normals[i].y, iter.Normals[i].x);
                angleC = 180 - (AngleBetweenVector2(RotatedNormal, -iter.Directions[i]) + AngleStep);
                nextDist = (iter.Distances[i] * math.sin(math.radians(AngleStep))) / Mathf.Sin(math.radians(angleC));
                iter.NextPoints[i] = iter.Points[i] + (RotatedNormal * nextDist);
            }

            iter.RayAngles[IterationRayCount - 1] = PreviousIteration.RayAngles[PrevIterStartIndex + 1];
            iter.Hits[IterationRayCount - 1] = PreviousIteration.Hits[PrevIterStartIndex + 1];
            iter.Distances[IterationRayCount - 1] = PreviousIteration.Distances[PrevIterStartIndex + 1];
            iter.Points[IterationRayCount - 1] = PreviousIteration.Points[PrevIterStartIndex + 1];
            iter.Directions[IterationRayCount - 1] = PreviousIteration.Directions[PrevIterStartIndex + 1];
            iter.Normals[IterationRayCount - 1] = PreviousIteration.Normals[PrevIterStartIndex + 1];
            iter.NextPoints[IterationRayCount - 1] = PreviousIteration.NextPoints[PrevIterStartIndex + 1];

#if UNITY_EDITOR
            if (DebugMode && DrawIteritiveRays)
            {
                Debug.DrawRay(EyePosition, DirFromAngle(initialAngle + AngleStep * 0, true) * 10, Color.red);
                Debug.DrawRay(EyePosition, DirFromAngle(initialAngle + AngleStep * (IterationRayCount - 1), true) * 10, Color.red);
                //Debug.DrawRay(EyePosition, DirFromAngle(currentRay.angle, true) * 10, Color.red);
            }

            if (ProfileExtraIterations)
                Profiler.EndSample();
#endif
            return iter;
        }

        [BurstCompile]
        public struct ConditionCalculations : IJobParallelFor
        {
            public float DoubleHitMaxAngleDelta;
            public float EdgeDstThreshold;
            public bool AddCorners;

            [ReadOnly][NativeDisableParallelForRestriction] public NativeArray<float2> Points;
            [ReadOnly][NativeDisableParallelForRestriction] public NativeArray<float2> NextPoints;
            [ReadOnly][NativeDisableParallelForRestriction] public NativeArray<float2> Normals;
            [ReadOnly][NativeDisableParallelForRestriction] public NativeArray<bool> Hits;

            [WriteOnly][NativeDisableParallelForRestriction] public NativeArray<bool> IterateConditions;
            public void Execute(int id)
            {
                if (id == 0)
                    return;
                float AngleDelta = AngleBetweenVector2(Normals[id], Normals[id - 1]);
                bool AngleCondition = math.abs(AngleDelta) > DoubleHitMaxAngleDelta;
                bool DistanceCondition = !Vector2Aprox(Points[id], NextPoints[id - 1]);

                bool SampleCondition = (Hits[id - 1] || Hits[id]) &&
                    (DistanceCondition || AngleCondition)
                    || Hits[id - 1] != Hits[id];

                if (!AddCorners && AngleCondition && AngleDelta > 0)
                {
                    if (!DistanceCondition)
                        SampleCondition = false;
                }

                IterateConditions[id] = SampleCondition;
            }

            float AngleBetweenVector2(float2 vec1, float2 vec2)
            {
                float2 vec1Rotated90 = new float2();
                vec1Rotated90.x = -vec1.y;
                vec1Rotated90.y = vec1.x;
                float sign = (math.dot(vec1Rotated90, vec2) < 0) ? -1.0f : 1.0f;
                return Vector2.Angle(vec1, vec2) * sign;
            }

            bool Vector2Aprox(float2 v1, float2 v2)
            {
                return math.distancesq(v1, v2) < EdgeDstThreshold;
                //return math.abs(math.distancesq(v1, v2)) < EdgeDstThreshold;
                //return Mathf.Abs((v1 - v2).sqrMagnitude) < EdgeDstThreshold;
                //return Mathf.Approximately(0, (v1 - v2).sqrMagnitude);
            }
        }

        protected FindEdgeJob EdgeJob;
        protected JobHandle EdgeJobHandle;
        protected abstract void _FindEdge();
        private void FindEdgesJobs()
        {
            _FindEdge();
        }

        public struct EdgeResolveData
        {
            public float CurrentAngle;
            public float AngleAdd;
            public float Sign;
            public bool Break;
        }

        [BurstCompile]
        protected struct FindEdgeJob : IJobParallelFor
        {
            public float MaxAcceptableEdgeAngleDifference;
            public float DoubleHitMaxAngleDelta;
            public float EdgeDstThreshold;
            [ReadOnly] public NativeArray<SightRay> SightRays;
            public NativeArray<SightSegment> SightSegments;
            public NativeArray<float2> EdgeNormals;
            public NativeArray<EdgeResolveData> EdgeData;
            public void Execute(int index)
            {
                EdgeResolveData data = EdgeData[index];

                if (data.Break)
                    return;

                SightSegment segment = SightSegments[index];
                SightRay currentRay = SightRays[index];

                float2 normal = EdgeNormals[index];
                float _angleStep = data.CurrentAngle - segment.Angle;
                float2 RotatedNormal = new float2(-normal.y, normal.x);
                float angleC = 180 - (AngleBetweenVector2(RotatedNormal, -segment.Direction) + _angleStep);
                float nextDist = (segment.Radius * math.sin(math.radians(_angleStep))) / Mathf.Sin(math.radians(angleC));
                float2 nextPoint = segment.Point + (RotatedNormal * nextDist);

                if (segment.DidHit != currentRay.hit ||
                        Vector2.Angle(normal, currentRay.normal) > DoubleHitMaxAngleDelta ||
                        !Vector2Aprox(nextPoint, currentRay.point))
                {
                    data.Sign = -1;
                }
                else
                {
                    data.Sign = 1;
                    segment.Angle = data.CurrentAngle;
                    segment.Radius = currentRay.distance;
                    EdgeNormals[index] = currentRay.normal;
                    segment.Point = currentRay.point;
                }

                SightSegments[index] = segment;

                data.AngleAdd /= 2;
                if (math.abs(data.AngleAdd) < MaxAcceptableEdgeAngleDifference)
                    data.Break = true;
                data.CurrentAngle += data.AngleAdd * data.Sign;
                
                EdgeData[index] = data;
            }

            float AngleBetweenVector2(float2 vec1, float2 vec2)
            {
                float2 vec1Rotated90 = new float2(-vec1.y, vec1.x);
                float sign = (math.dot(vec1Rotated90, vec2) < 0) ? -1.0f : 1.0f;
                return Vector2.Angle(vec1, vec2) * sign;
            }

            bool Vector2Aprox(float2 v1, float2 v2)
            {
                return math.distancesq(v1, v2) < EdgeDstThreshold;
            }
        }

        private void FindEdges()
        {
            //EDGE FIND. TODO: JOBIFY
            for (int i = 0; i < NumberOfPoints; i++)
            {
                float currentAngle = ViewPoints[i].Angle;
                float angleAdd = EdgeAngles[i];
                float sign = 1;

                angleAdd /= 2;
                currentAngle += angleAdd;
                for (int r = 0; r < MaxEdgeResolveIterations; r++)
                {
                    RayCast(currentAngle, ref currentRay);

//#if UNITY_EDITOR
//                    Profiler.BeginSample("math");
//#endif
                    RotatedNormal.x = -EdgeNormals[i].y;
                    RotatedNormal.y = EdgeNormals[i].x;
                    float _angleStep = currentAngle - ViewPoints[i].Angle;

                    float angleC = 180 - (AngleBetweenVector2(RotatedNormal, -ViewPoints[i].Direction) + _angleStep);
                    float nextDist = (ViewPoints[i].Radius * math.sin(math.radians(_angleStep))) / math.sin(math.radians(angleC));
                    float2 nextPoint = ViewPoints[i].Point + (RotatedNormal * nextDist);
//#if UNITY_EDITOR
//                    Profiler.EndSample();
//#endif

#if UNITY_EDITOR
                    //if (DebugMode && i == DEBUGEDGESLICE)
                    if (DebugMode && DrawEdgeResolveRays)
                    {
                        Debug.DrawLine(Get3Dfrom2D(ViewPoints[i].Point), Get3Dfrom2D(nextPoint) + Vector3.up * .03f, UnityEngine.Random.ColorHSV());
                        Debug.DrawRay(EyePosition, DirFromAngle(currentAngle, true) * currentRay.distance, angleAdd >= 0 ? Color.green : Color.cyan);
                    }
                        
#endif
                    if (ViewPoints[i].DidHit != currentRay.hit || 
                        Vector2.Angle(EdgeNormals[i], currentRay.normal) > DoubleHitMaxAngleDelta ||
                        !Vector2Aprox(nextPoint, currentRay.point))
                    {
                        sign = -1;
                    }
                    else
                    {
                        sign = 1;
                        ViewPoints[i].Angle = currentAngle;
                        ViewPoints[i].Radius = currentRay.distance;
                        EdgeNormals[i] = currentRay.normal;
                        ViewPoints[i].Point = currentRay.point;
                    }

                    angleAdd /= 2;
                    if (math.abs(angleAdd) < MaxAcceptableEdgeAngleDifference)
                        break;
                    currentAngle += angleAdd * sign;
                }
            }
        }

        float2 vec1Rotated90 = new float2();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float AngleBetweenVector2(float2 vec1, float2 vec2)
        {
            vec1Rotated90.x = -vec1.y;
            vec1Rotated90.y = vec1.x;
            float sign = (math.dot(vec1Rotated90, vec2) < 0) ? -1.0f : 1.0f;
            return Vector2.Angle(vec1, vec2) * sign;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool Vector2Aprox(float2 v1, float2 v2)
        {
            return math.distancesq(v1, v2) < EdgeDstThreshold;
            //return math.abs(math.distancesq(v1, v2)) < EdgeDstThreshold;
            //return Mathf.Abs((v1 - v2).sqrMagnitude) < EdgeDstThreshold;
            //return Mathf.Approximately(0, (v1 - v2).sqrMagnitude);
        }

        protected abstract Vector3 _Get3Dfrom2D(Vector2 twoD);
        Vector3 Get3Dfrom2D(Vector2 twoD)
        {
            return _Get3Dfrom2D(twoD);
        }

        [BurstCompile]
        public struct CalculateNextPoints : IJobParallelFor
        {
            public Vector3 UpVector;
            public float AngleStep;
            //[ReadOnly] public NativeArray<SightRay> rays;
            [ReadOnly] public NativeArray<float> Distances;
            [ReadOnly] public NativeArray<float2> Points;
            [ReadOnly] public NativeArray<float2> Normals;
            [ReadOnly] public NativeArray<float2> Directions;

            [WriteOnly] public NativeArray<float2> ExpectedNextPoints;
            public void Execute(int id)
            {
                //float angleC = 180 - (AngleBetweenVector2(-Vector3.Cross(Normals[id], UpVector), -Directions[id].normalized) + AngleStep);
                float2 normal = Normals[id];
                float2 RotatedNormal = new float2(-normal.y, normal.x);
                float angleC = 180 - (AngleBetweenVector2(RotatedNormal, -Directions[id]) + AngleStep);
                float nextDist = (Distances[id] * math.sin(math.radians(AngleStep))) / Mathf.Sin(math.radians(angleC));
                ExpectedNextPoints[id] = Points[id] + (RotatedNormal * nextDist);
            }
            float AngleBetweenVector2(float2 vec1, float2 vec2)
            {
                float2 vec1Rotated90 = new float2(-vec1.y, vec1.x);
                float sign = (math.dot(vec1Rotated90, vec2) < 0) ? -1.0f : 1.0f;
                return Vector2.Angle(vec1, vec2) * sign;
            }
        }
    }

    public class SightIteration
    {
        //public float[] RayAngles;
        public NativeArray<float> RayAngles;
        public NativeArray<bool> Hits;
        public NativeArray<float> Distances;
        public NativeArray<float2> Points;
        public NativeArray<float2> Directions;
        public NativeArray<float2> Normals;

        public NativeArray<float2> NextPoints;

        public SightIteration[] NextIterations;

        public void InitializeStruct(int NumSteps)
        {
            //RayAngles = new float[NumSteps];
            RayAngles = new NativeArray<float>(NumSteps, Allocator.Persistent);
            Hits = new NativeArray<bool>(NumSteps, Allocator.Persistent);
            Distances = new NativeArray<float>(NumSteps, Allocator.Persistent);
            Points = new NativeArray<float2>(NumSteps, Allocator.Persistent);
            Directions = new NativeArray<float2>(NumSteps, Allocator.Persistent);
            Normals = new NativeArray<float2>(NumSteps, Allocator.Persistent);
            NextPoints = new NativeArray<float2>(NumSteps, Allocator.Persistent);
        }
        public void DisposeStruct()
        {
            //RayAngles = null;
            RayAngles.Dispose();
            Distances.Dispose();
            Hits.Dispose();
            Points.Dispose();
            Directions.Dispose();
            Normals.Dispose();
            NextPoints.Dispose();
        }
    }
}
