using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using static Unity.Burst.Intrinsics.X86.Avx;
using System;
using UnityEngine.Serialization;



#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Profiling;
#endif

namespace FOW
{
    [DefaultExecutionOrder(-100)]
    public class FogOfWarWorld : MonoBehaviour
    {
        public static FogOfWarWorld instance;

        public FowUpdateMethod UpdateMethod = FowUpdateMethod.LateUpdate;

        public bool UsingSoftening;

        public FogOfWarType FogType = FogOfWarType.Soft;
        public FogOfWarFadeType FogFade = FogOfWarFadeType.Smoothstep;
        public FogOfWarBlendMode BlendType = FogOfWarBlendMode.Max;
        public float EdgeSoftenDistance = .1f;
        public float UnobscuredSoftenDistance = .25f;
        public bool UseInnerSoften = true;
        public float InnerSoftenAngle = 5;
        public bool AllowBleeding = false;
        public float SightExtraAmount = .01f;
        public float MaxFogDistance = 10000f;
        public bool PixelateFog = false;
        public bool WorldSpacePixelate = false;
        public float PixelDensity = 2f;
        public bool RoundRevealerPosition = false;
        public Vector2 PixelGridOffset;
        public bool UseDithering = false;
        public float DitherSize = 20;
        public bool InvertFowEffect;

        public float FogFadePower = 1;

        [SerializeField] private FogOfWarAppearance FogAppearance;

        [Tooltip("The color of the fog")]
        public Color UnknownColor = new Color(.35f, .35f, .35f);

        public float SaturationStrength = 0;

        public float BlurStrength = 1;
        //public float blurPixelOffset = 2.5f;
        [Range(0, 2)]
        public float BlurDistanceScreenPercentMin = .1f;
        [Range(0, 2)]
        public float BlurDistanceScreenPercentMax = 1;
        public int BlurSamples = 6;

        public Texture2D FogTexture;
        public bool UseTriplanar = true;
        public Vector2 FogTextureTiling = Vector2.one;
        public Vector2 FogScrollSpeed = Vector2.one;

        public float OutlineThickness = .1f;

        public FogSampleMode FOWSamplingMode = FogSampleMode.Pixel_Perfect;
        public bool UseRegrow;
        public bool RevealerFadeIn = false;
        public float FogRegrowSpeed = .5f;
        public float InitialFogExplorationValue = 0;
        public float MaxFogRegrowAmount = .3f;
        RenderTexture FOW_RT;
        RenderTexture FOW_TEMP_RT;
        public Material FowTextureMaterial;
        public int FowResX = 256;
        public int FowResY = 256;
        public bool UseConstantBlur = true;
        public int ConstantTextureBlurQuality = 2;
        public float ConstantTextureBlurAmount = 0.75f;

        public bool UseWorldBounds;
        public float WorldBoundsSoftenDistance = 1f;
        public float WorldBoundsInfluence = 1;
        public Bounds WorldBounds = new Bounds(Vector3.zero, Vector3.one);
        
        public bool UseMiniMap;
        public Color MiniMapColor = new Color(.4f, .4f, .4f, .95f);
        public RawImage UIImage;

        //public bool AllowMinimumDistance = false;

        [FormerlySerializedAs("revealerMode")]
        public RevealerUpdateMethod RevealerUpdateMode = RevealerUpdateMethod.Every_Frame;
        [Tooltip("The number of revealers to update each frame. Only used when Revealer Mode is set to N_Per_Frame")]
        public int MaxNumRevealersPerFrame = 3;

        [Tooltip("The Max possible number of revealers. Keep this as low as possible to use less GPU memory")]
        public int MaxPossibleRevealers = 256;
        [Tooltip("The Max possible number of segments per revealer. Keep this as low as possible to use less GPU memory")]
        public int MaxPossibleSegmentsPerRevealer = 128;

        public bool is2D;
        public GamePlane gamePlane = GamePlane.XZ;

        public Material FogOfWarMaterial;

        static int maxCones;
        public static ComputeBuffer IndicesBuffer;
        public static ComputeBuffer CircleBuffer;
        public static ComputeBuffer AnglesBuffer;


        public static FogOfWarRevealer[] Revealers;
        private static int _numRevealers;
        public static int numDynamicRevealers;

        public static List<FogOfWarHider> HidersList = new List<FogOfWarHider>();
        public static List<PartialHider> PartialHiders = new List<PartialHider>();
        public static int NumHiders;
        public static List<FogOfWarRevealer> RevealersToRegister = new List<FogOfWarRevealer>();

        public static List<int> DeregisteredIDs = new List<int>();
        private static int numDeregistered = 0;

        private static int[] indiciesDataToSet = new int[1];

        int numRevealersID = Shader.PropertyToID("_NumRevealers");
        int materialColorID = Shader.PropertyToID("_unKnownColor");
        //int blurRadiusID = Shader.PropertyToID("_fadeOutDistance");
        int unobscuredBlurRadiusID = Shader.PropertyToID("_unboscuredFadeOutDistance");
        int extraRadiusID = Shader.PropertyToID("_extraRadius");
        int maxDistanceID = Shader.PropertyToID("_maxDistance");
        int fadePowerID = Shader.PropertyToID("_fadePower");
        int saturationStrengthID = Shader.PropertyToID("_saturationStrength");
        int blurStrengthID = Shader.PropertyToID("_blurStrength");
        //int blurPixelOffsetID = Shader.PropertyToID("_blurPixelOffset");
        int blurPixelOffsetMinID = Shader.PropertyToID("_blurPixelOffsetMin");
        int blurPixelOffsetMaxID = Shader.PropertyToID("_blurPixelOffsetMax");
        int blurSamplesID = Shader.PropertyToID("_blurSamples");
        int blurPeriodID = Shader.PropertyToID("_samplePeriod");
        int fowTetureID = Shader.PropertyToID("_fowTexture");
        int fowTilingID = Shader.PropertyToID("_fowTiling");
        int fowSpeedID = Shader.PropertyToID("_fowScrollSpeed");

        #region Data Structures

        [StructLayout(LayoutKind.Sequential)]
        public struct RevealerStruct
        {
            public Vector2 CircleOrigin;
            public int StartIndex;
            public int NumSegments;
            public float CircleHeight;
            public float UnobscuredRadius;
            //public int isComplete;
            public float CircleRadius;
            public float CircleFade;
            public float VisionHeight;
            public float HeightFade;
            public float Opacity;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct ConeEdgeStruct
        {
            public float angle;
            public float length;
            public int cutShort;
        };
        public enum FowUpdateMethod
        {
            Update,
            LateUpdate
        };
        public enum RevealerUpdateMethod
        {
            Every_Frame,
            N_Per_Frame,
            Controlled_ElseWhere,
        };
        public enum FogSampleMode
        {
            Pixel_Perfect,
            Texture,
            Both,
        };

        public enum FogOfWarType
        {
            //No_Bleed,
            //No_Bleed_Soft,
            Hard,
            Soft,
        };

        public enum FogOfWarFadeType
        {
            Linear,
            Exponential,
            Smooth,
            Smoother,
            Smoothstep,
        };

        public enum FogOfWarBlendMode
        {
            Max,
            Addative,
        };

        public enum FogOfWarAppearance
        {
            Solid_Color,
            GrayScale,
            Blur,
            Texture_Sample,
            Outline,
            None
        };

        public enum GamePlane
        {
            XZ,
            XY,
            ZY,
        };
        #endregion

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void OnLoad()
        {
            ResetStatics();
        }

        static void ResetStatics()
        {
            instance = null;
            HidersList = new List<FogOfWarHider>();
            PartialHiders = new List<PartialHider>();
            NumHiders = 0;
            RevealersToRegister = new List<FogOfWarRevealer>();
        }

        #region Unity Methods
        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            // see the unity bug workaround section
            UnityBugWorkaround.OnAssetPostProcess += ReInitializeFOW;
#endif
            Initialize();
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            // see the unity bug workaround section
            UnityBugWorkaround.OnAssetPostProcess -= ReInitializeFOW;
#endif
            Cleanup();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        int currentIndex = 0;
        private void Update()
        {
            if (UpdateMethod == FowUpdateMethod.Update)
                CalculateFOW();
        }

        private void LateUpdate()
        {
            if (UpdateMethod == FowUpdateMethod.LateUpdate)
                CalculateFOW();
        }

        #endregion

        void CalculateFOW()
        {
            if (_numRevealers > 0)
            {
                switch (RevealerUpdateMode)
                {
                    case RevealerUpdateMethod.Every_Frame:
                        for (int i = 0; i < _numRevealers; i++)
                        {
                            Revealers[i].RevealHiders();
                            if (!Revealers[i].StaticRevealer)
                                Revealers[i].LineOfSightPhase1();
                        }
                        for (int i = 0; i < _numRevealers; i++)
                        {
                            if (!Revealers[i].StaticRevealer)
                                Revealers[i].LineOfSightPhase2();
                        }
                        break;
                    case RevealerUpdateMethod.N_Per_Frame:
                        int index = currentIndex;
                        for (int i = 0; i < Mathf.Clamp(MaxNumRevealersPerFrame, 0, numDynamicRevealers); i++)
                        {
                            index = (index + 1) % _numRevealers;
                            Revealers[index].RevealHiders();
                            if (!Revealers[index].StaticRevealer)
                                Revealers[index].LineOfSightPhase1();
                            else
                                i--;
                        }
                        for (int i = 0; i < Mathf.Clamp(MaxNumRevealersPerFrame, 0, numDynamicRevealers); i++)
                        {
                            currentIndex = (currentIndex + 1) % _numRevealers;
                            if (!Revealers[currentIndex].StaticRevealer)
                                Revealers[currentIndex].LineOfSightPhase2();
                            else
                                i--;
                        }
                        break;
                    case RevealerUpdateMethod.Controlled_ElseWhere: break;
                }
            }

            if (UseMiniMap || FOWSamplingMode == FogSampleMode.Texture || FOWSamplingMode == FogSampleMode.Both)
            {
                if (UseRegrow)
                {
                    //we dont need the second pass anymore :)
                    //Graphics.Blit(FOW_RT, FOW_REGROW_RT, FowTextureMaterial, 1);
                    //Graphics.Blit(FOW_REGROW_RT, FOW_RT, FowTextureMaterial, 0);

                    Graphics.Blit(FOW_RT, FOW_TEMP_RT);
                    Graphics.Blit(FOW_TEMP_RT, FOW_RT, FowTextureMaterial, 0);

                    //Graphics.Blit(FOW_RT, FOW_RT, FowTextureMaterial, 0);
                }
                else
                    Graphics.Blit(null, FOW_RT, FowTextureMaterial, 0);
            }
        }

        #region Dumb Unity Bug Workaround :)
#if UNITY_EDITOR
        //BASICALLY, every time an asset is updated in the project folder, materials are losing the compute buffer data. 
        //So, im hooking onto asset post processing, and re-initializing the material with the necessary data
        public void ReInitializeFOW()
        {
            StartCoroutine(FixFowDebug());
        }

        IEnumerator FixFowDebug()
        {
            yield return new WaitForEndOfFrame();
            enabled = false;
            enabled = true;
            //FogOfWarMaterial.SetBuffer(Shader.PropertyToID("_ActiveCircleIndices"), IndicesBuffer);
            //FogOfWarMaterial.SetBuffer(Shader.PropertyToID("_CircleBuffer"), CircleBuffer);
            //FogOfWarMaterial.SetBuffer(Shader.PropertyToID("_ConeBuffer"), AnglesBuffer);
            //UpdateMaterialProperties(FogOfWarMaterial);
        }
#endif
        #endregion

        void Cleanup()
        {
            Shader.SetGlobalFloat("FowEffectStrength", 0);
            int n = _numRevealers;
            for (int i = 0; i < n; i++)
            {
                FogOfWarRevealer revealer = Revealers[0];
                revealer.DeregisterRevealer();
                RevealersToRegister.Add(revealer);
            }
            if (CircleBuffer != null)
            {
                //setAnglesBuffersJobHandle.Complete();
                //AnglesNativeArray.Dispose();
                IndicesBuffer.Dispose();
                CircleBuffer.Dispose();
                AnglesBuffer.Dispose();
            }
            instance = null;
        }

        //private JobHandle setAnglesBuffersJobHandle;
        //private SetAnglesBuffersJob setAnglesBuffersJob;
        //private NativeArray<ConeEdgeStruct> AnglesNativeArray;    //was used when using computebuffer.beginwrite. will be used again when unity fixes a bug internally
        //private NativeArray<int> _circleIndicesArray;
        //private NativeArray<CircleStruct> _circleArray;
        //private NativeArray<ConeEdgeStruct> _angleArray;
        private ConeEdgeStruct[] anglesArray;

        public void Initialize()
        {
            if (instance != null)
                return;

            //ResetStatics();
            instance = this;

            Shader.SetGlobalFloat("FowEffectStrength", 1);

            maxCones = MaxPossibleRevealers * MaxPossibleSegmentsPerRevealer;

            Revealers = new FogOfWarRevealer[MaxPossibleRevealers];
            //indicesBuffer = new ComputeBuffer(maxPossibleRevealers, Marshal.SizeOf(typeof(int)), ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
            IndicesBuffer = new ComputeBuffer(MaxPossibleRevealers, Marshal.SizeOf(typeof(int)), ComputeBufferType.Default);

            //circleBuffer = new ComputeBuffer(maxPossibleRevealers, Marshal.SizeOf(typeof(CircleStruct)), ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
            CircleBuffer = new ComputeBuffer(MaxPossibleRevealers, Marshal.SizeOf(typeof(RevealerStruct)), ComputeBufferType.Default);

            anglesArray = new ConeEdgeStruct[MaxPossibleSegmentsPerRevealer];
            //AnglesNativeArray = new NativeArray<ConeEdgeStruct>(maxPossibleSegmentsPerRevealer, Allocator.Persistent);
            //anglesBuffer = new ComputeBuffer(maxCones, Marshal.SizeOf(typeof(ConeEdgeStruct)), ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
            AnglesBuffer = new ComputeBuffer(maxCones, Marshal.SizeOf(typeof(ConeEdgeStruct)), ComputeBufferType.Default);

            FogOfWarMaterial = new Material(Shader.Find("Hidden/FullScreen/FOW/SolidColor"));
            //SetFogShader();

            //UpdateMaterialProperties(FogOfWarMaterial);
            if (UseMiniMap || FOWSamplingMode == FogSampleMode.Texture || FOWSamplingMode == FogSampleMode.Both)
            {
                FowTextureMaterial = new Material(Shader.Find("Hidden/FullScreen/FOW/FOW_RT"));
                InitFOWRT();

                UpdateMaterialProperties(FowTextureMaterial);
                FowTextureMaterial.SetBuffer(Shader.PropertyToID("_ActiveCircleIndices"), IndicesBuffer);
                FowTextureMaterial.SetBuffer(Shader.PropertyToID("_CircleBuffer"), CircleBuffer);
                FowTextureMaterial.SetBuffer(Shader.PropertyToID("_ConeBuffer"), AnglesBuffer);
                FowTextureMaterial.EnableKeyword("IGNORE_HEIGHT");
            }
            SetFogShader();
            UpdateAllMaterialProperties();
            SetAllMaterialBounds();

            //setAnglesBuffersJob = new SetAnglesBuffersJob();

            foreach (FogOfWarRevealer revealer in RevealersToRegister)
            {
                if (revealer != null)
                    revealer.RegisterRevealer();
            }
            RevealersToRegister.Clear();
        }

        public static Vector3 UpVector;
        public static Vector3 ForwardVector;
        public void SetFogShader()
        {
            if (!Application.isPlaying)
                return;

            UsingSoftening = false;
            string shaderName = "Hidden/FullScreen/FOW";
            switch (FogAppearance)
            {
                case FogOfWarAppearance.Solid_Color: shaderName += "/SolidColor"; break;
                case FogOfWarAppearance.GrayScale: shaderName += "/GrayScale"; break;
                case FogOfWarAppearance.Blur: shaderName += "/Blur"; break;
                case FogOfWarAppearance.Texture_Sample: shaderName += "/TextureSample"; break;
                case FogOfWarAppearance.Outline: shaderName += "/Outline"; break;
                case FogOfWarAppearance.None: shaderName = "Hidden/BlitCopy"; break;
            }
            FogOfWarMaterial.shader = Shader.Find(shaderName);
#if UNITY_2021_2_OR_NEWER
#else
            //this was required in unity 2020.3.28. when updating to 2020.3.48, its no longer required. not sure what version fixes it exactly.
            //FogOfWarMaterial.EnableKeyword("_VS_NORMAL");   //this is only for urp/texture sample fog mode
#endif

            InitializeFogProperties(FogOfWarMaterial);
            UpdateMaterialProperties(FogOfWarMaterial);
            //SetMaterialBounds();
        }

        public void InitializeFogProperties(Material material)
        {
            material.DisableKeyword("IS_2D");
            material.DisableKeyword("IS_3D");
            if (!is2D)
            {
                material.EnableKeyword("IS_3D");
                //material.DisableKeyword("PLANE_XZ");
                //material.DisableKeyword("PLANE_XY");
                //material.DisableKeyword("PLANE_ZY");
                switch (gamePlane)
                {
                    case GamePlane.XZ:
                        //material.EnableKeyword("PLANE_XZ");
                        material.SetInt("_fowPlane", 1);
                        UpVector = Vector3.up;
                        break;
                    case GamePlane.XY:
                        //material.EnableKeyword("PLANE_XY");
                        material.SetInt("_fowPlane", 2);
                        UpVector = -Vector3.forward;
                        break;
                    case GamePlane.ZY:
                        //material.EnableKeyword("PLANE_ZY");
                        material.SetInt("_fowPlane", 3);
                        UpVector = Vector3.right;
                        break;
                }
            }
            else
            {
                UpVector = -Vector3.forward;
                material.EnableKeyword("IS_2D");

                material.SetInt("_fowPlane", 0);
            }

            material.SetBuffer(Shader.PropertyToID("_ActiveCircleIndices"), IndicesBuffer);
            material.SetBuffer(Shader.PropertyToID("_CircleBuffer"), CircleBuffer);
            material.SetBuffer(Shader.PropertyToID("_ConeBuffer"), AnglesBuffer);
        }

        public void UpdateAllMaterialProperties()
        {
            if (!Application.isPlaying)
                return;

            UpdateMaterialProperties(FogOfWarMaterial);
            if (FowTextureMaterial != null)
                UpdateMaterialProperties(FowTextureMaterial);

            foreach (PartialHider hider in PartialHiders)
                UpdateMaterialProperties(hider.HiderMaterial);

            //SetMaterialBounds();
        }

        public void UpdateMaterialProperties(Material material)
        {
#if UNITY_EDITOR
            if (material == null)   //fix for "Enter Playmode Options"
                return;
#endif
            material.DisableKeyword("HARD");
            material.DisableKeyword("SOFT");
            UsingSoftening = false;
            switch (FogType)
            {
                case FogOfWarType.Hard: material.EnableKeyword("HARD"); break;
                case FogOfWarType.Soft: material.EnableKeyword("SOFT"); UsingSoftening = true; break;
            }

            //material.DisableKeyword("BLEED");
            //if (AllowBleeding)
            //    material.EnableKeyword("BLEED");
            material.SetInt("BLEED", 0);
            if (AllowBleeding)
                material.SetInt("BLEED", 1);

            material.SetColor(materialColorID, material == FowTextureMaterial ? MiniMapColor : UnknownColor);
            material.SetFloat(unobscuredBlurRadiusID, UnobscuredSoftenDistance);
            material.DisableKeyword("INNER_SOFTEN");
            if (FogType == FogOfWarType.Soft && UseInnerSoften)
            {
                material.EnableKeyword("INNER_SOFTEN");
                material.SetFloat(Shader.PropertyToID("_fadeOutDegrees"), InnerSoftenAngle);
            }
            else
                material.SetFloat(Shader.PropertyToID("_fadeOutDegrees"), 0);

            material.SetFloat(extraRadiusID, SightExtraAmount);

            material.SetFloat(Shader.PropertyToID("_edgeSoftenDistance"), EdgeSoftenDistance);
            material.SetFloat(maxDistanceID, MaxFogDistance);

            #region Pixellation
            material.SetInt("_pixelate", 0);
            if (PixelateFog && !WorldSpacePixelate)
                material.SetInt("_pixelate", 1);

            material.SetInt("_pixelateWS", 0);
            if (PixelateFog && WorldSpacePixelate)
                material.SetInt("_pixelateWS", 1);

            if (PixelateFog)
                material.SetFloat(extraRadiusID, SightExtraAmount + (1f / PixelDensity));
            #endregion

            material.SetFloat("_pixelDensity", PixelDensity);
            material.SetVector("_pixelOffset", PixelGridOffset);

            material.SetInt("_ditherFog", 0);
            if (UseDithering)
                material.SetInt("_ditherFog", 1);
            material.SetFloat("_ditherSize", DitherSize);

            material.SetInt("_invertEffect", 0);
            if (InvertFowEffect)
                material.SetInt("_invertEffect", 1);

            //material.DisableKeyword("FADE_LINEAR");
            //material.DisableKeyword("FADE_SMOOTH");
            //material.DisableKeyword("FADE_SMOOTHER");
            //material.DisableKeyword("FADE_SMOOTHSTEP");
            //material.DisableKeyword("FADE_EXP");
            //switch (FogFade)
            //{
            //    case FogOfWarFadeType.Linear:
            //        material.EnableKeyword("FADE_LINEAR");
            //        break;
            //    case FogOfWarFadeType.Exponential:
            //        material.EnableKeyword("FADE_EXP");
            //        material.SetFloat(fadePowerID, FogFadePower);
            //        break;
            //    case FogOfWarFadeType.Smooth:
            //        material.EnableKeyword("FADE_SMOOTH");
            //        break;
            //    case FogOfWarFadeType.Smoother:
            //        material.EnableKeyword("FADE_SMOOTHER");
            //        break;
            //    case FogOfWarFadeType.Smoothstep:
            //        material.EnableKeyword("FADE_SMOOTHSTEP");
            //        break;
            //}
            switch (FogFade)
            {
                case FogOfWarFadeType.Linear:
                    material.SetInt("_fadeType", 0);
                    break;
                case FogOfWarFadeType.Exponential:
                    material.SetInt("_fadeType", 4);
                    material.SetFloat(fadePowerID, FogFadePower);
                    break;
                case FogOfWarFadeType.Smooth:
                    material.SetInt("_fadeType", 1);
                    break;
                case FogOfWarFadeType.Smoother:
                    material.SetInt("_fadeType", 2);
                    break;
                case FogOfWarFadeType.Smoothstep:
                    material.SetInt("_fadeType", 3);
                    break;
            }
            //material.DisableKeyword("BLEND_MAX");
            //material.DisableKeyword("BLEND_ADDITIVE");
            //switch (BlendType)
            //{
            //    case FogOfWarBlendMode.Max:
            //        material.EnableKeyword("BLEND_MAX");
            //        break;
            //    case FogOfWarBlendMode.Addative:
            //        material.EnableKeyword("BLEND_ADDITIVE");
            //        break;
            //}
            material.SetInt("BLEND_MAX", 1);
            switch (BlendType)
            {
                case FogOfWarBlendMode.Max:
                    material.SetInt("BLEND_MAX", 1);
                    break;
                case FogOfWarBlendMode.Addative:
                    material.SetInt("BLEND_MAX", 0);
                    break;
            }

            switch (FogAppearance)
            {
                case FogOfWarAppearance.Solid_Color:
                    break;
                case FogOfWarAppearance.GrayScale:
                    material.SetFloat(saturationStrengthID, SaturationStrength);
                    break;
                case FogOfWarAppearance.Blur:
                    material.SetFloat(blurStrengthID, BlurStrength);
                    material.SetFloat(blurPixelOffsetMinID, Screen.height * (BlurDistanceScreenPercentMin / 100));
                    material.SetFloat(blurPixelOffsetMaxID, Screen.height * (BlurDistanceScreenPercentMax / 100));
                    material.SetInt(blurSamplesID, BlurSamples);
                    material.SetFloat(blurPeriodID, (2 * Mathf.PI) / BlurSamples);    //TAU = 2 * PI
                    break;
                case FogOfWarAppearance.Texture_Sample:
                    material.SetTexture(fowTetureID, FogTexture);
                    material.SetInt("_skipTriplanar", 0);
                    if (!UseTriplanar)
                    {
                        material.SetInt("_skipTriplanar", 1);
                        material.SetVector("_fowAxis", UpVector);
                    }
                    material.SetVector(fowTilingID, FogTextureTiling);
                    material.SetVector(fowSpeedID, FogScrollSpeed);
                    break;
                case FogOfWarAppearance.Outline:
                    material.SetFloat("lineThickness", OutlineThickness);
                    break;
            }


            material.DisableKeyword("SAMPLE_REALTIME");
            if (FOWSamplingMode == FogSampleMode.Pixel_Perfect || FOWSamplingMode == FogSampleMode.Both)
                material.EnableKeyword("SAMPLE_REALTIME");

            material.DisableKeyword("SAMPLE_TEXTURE");
            material.DisableKeyword("USE_TEXTURE_BLUR");
            if (FOWSamplingMode == FogSampleMode.Texture || FOWSamplingMode == FogSampleMode.Both)
            {
                material.SetTexture("_FowRT", FOW_RT);
                material.EnableKeyword("SAMPLE_TEXTURE");
                
                if (UseConstantBlur)
                {
                    material.EnableKeyword("USE_TEXTURE_BLUR");
                    material.SetFloat("_Sample_Blur_Quality", ConstantTextureBlurQuality);
                    material.SetFloat("_Sample_Blur_Amount", ConstantTextureBlurAmount);
                }
            }

            if (material == FowTextureMaterial)
            {
                //material.SetTexture("_FowRT", FOW_RT);
                //material.SetTexture("_FowRT", FogTexture);
                material.SetFloat("_regrowSpeed", FogRegrowSpeed);
                material.SetFloat("_maxRegrowAmount", MaxFogRegrowAmount);
                material.EnableKeyword("SAMPLE_REALTIME");
                material.DisableKeyword("SAMPLE_TEXTURE");
                material.DisableKeyword("USE_REGROW");
                if (UseRegrow)
                {
                    material.EnableKeyword("USE_REGROW");
                    material.DisableKeyword("USE_FADEIN");
                    if (RevealerFadeIn)
                        material.EnableKeyword("USE_FADEIN");
                }
            }

            material.DisableKeyword("USE_WORLD_BOUNDS");
            if (UseRegrow)
                material.EnableKeyword("USE_WORLD_BOUNDS");

            //material.DisableKeyword("USE_WORLD_BOUNDS");
            //if (UseWorldBounds)
            //    material.EnableKeyword("USE_WORLD_BOUNDS");
            material.SetFloat("_worldBoundsInfluence", 0);
            if (UseWorldBounds)
            {
                material.SetFloat("_worldBoundsSoftenDistance", WorldBoundsSoftenDistance);
                material.SetFloat("_worldBoundsInfluence", WorldBoundsInfluence);
            }

            SetMaterialBounds(material);
        }

        public void UpdateWorldBounds(Vector3 center, Vector3 extent)
        {
            WorldBounds.center = center;
            WorldBounds.extents = extent;
            SetAllMaterialBounds();
        }

        public void UpdateWorldBounds(Bounds newBounds)
        {
            WorldBounds = newBounds;
            SetAllMaterialBounds();
        }

        void SetAllMaterialBounds()
        {
            if (FogOfWarMaterial != null)
                SetMaterialBounds(FogOfWarMaterial);

            if (FowTextureMaterial != null)
                SetMaterialBounds(FowTextureMaterial);
        }

        void SetMaterialBounds(Material mat)
        {
            //if (UseWorldBounds && FogOfWarMaterial != null)
            Vector4 boundsVec = GetBoundsVectorForShader();
            if (mat != null)
                mat.SetVector("_worldBounds", boundsVec);
        }

        public Vector4 GetBoundsVectorForShader()
        {
            if (is2D)
                return new Vector4(WorldBounds.size.x, WorldBounds.center.x, WorldBounds.size.y, WorldBounds.center.y);

            switch(gamePlane)
            {
                case GamePlane.XZ: return new Vector4(WorldBounds.size.x, WorldBounds.center.x, WorldBounds.size.z, WorldBounds.center.z);
                case GamePlane.XY: return new Vector4(WorldBounds.size.x, WorldBounds.center.x, WorldBounds.size.y, WorldBounds.center.y);
                case GamePlane.ZY: return new Vector4(WorldBounds.size.z, WorldBounds.center.z, WorldBounds.size.z, WorldBounds.center.z);
            }

            return new Vector4(WorldBounds.size.x, WorldBounds.center.x, WorldBounds.size.z, WorldBounds.center.z);
        }

        public Vector2 GetFowPositionFromWorldPosition(Vector3 WorldPosition)
        {
            if (is2D)
                return new Vector2(WorldPosition.x, WorldPosition.y);

            switch (gamePlane)
            {
                case GamePlane.XZ: return new Vector2(WorldPosition.x, WorldPosition.z);
                case GamePlane.XY: return new Vector2(WorldPosition.x, WorldPosition.y);
                case GamePlane.ZY: return new Vector2(WorldPosition.z, WorldPosition.y);
            }

            return new Vector2(WorldPosition.x, WorldPosition.z);
        }

        void SetNumRevealers()
        {
            if (FogOfWarMaterial != null)
                SetNumRevealers(FogOfWarMaterial);
            if (FowTextureMaterial != null)
                SetNumRevealers(FowTextureMaterial);
            foreach (PartialHider hider in PartialHiders)
                SetNumRevealers(hider.HiderMaterial);
        }

        public void SetNumRevealers(Material material)
        {
            material.SetInt(numRevealersID, _numRevealers);
        }
        
        public int RegisterRevealer(FogOfWarRevealer newRevealer)
        {
#if UNITY_EDITOR
            Profiler.BeginSample("Register Revealer");
#endif
            _numRevealers++;
            if (!newRevealer.StaticRevealer)
                numDynamicRevealers++;
            SetNumRevealers();

            int newID = _numRevealers - 1;
            Revealers[newID] = newRevealer;
            if (numDeregistered > 0)
            {
                numDeregistered--;
                newID = DeregisteredIDs[0];
                DeregisteredIDs.RemoveAt(0);
            }

            newRevealer.IndexID = _numRevealers - 1;

            indiciesDataToSet[0] = newID;
            IndicesBuffer.SetData(indiciesDataToSet, 0, _numRevealers - 1, 1);

            //_circleIndicesArray = indicesBuffer.BeginWrite<int>(numCircles - 1, 1);
            //_circleIndicesArray[0] = newID;

            //indicesBuffer.EndWrite<int>(1);

#if UNITY_EDITOR
            Profiler.EndSample();
#endif
            return newID;
        }
        public void DeRegisterRevealer(FogOfWarRevealer toRemove)
        {
#if UNITY_EDITOR
            Profiler.BeginSample("De-Register Revealer");
#endif
            int index = toRemove.IndexID;

            DeregisteredIDs.Add(toRemove.FogOfWarID);
            numDeregistered++;

            _numRevealers--;
            if (!toRemove.StaticRevealer)
                numDynamicRevealers--;

            FogOfWarRevealer toSwap = Revealers[_numRevealers];

            if (toRemove != toSwap)
            {
                Revealers[index] = toSwap;

                indiciesDataToSet[0] = toSwap.FogOfWarID;
                IndicesBuffer.SetData(indiciesDataToSet, 0, index, 1);
                //_circleIndicesArray = indicesBuffer.BeginWrite<int>(index, 1);
                //_circleIndicesArray[0] = toSwap.FogOfWarID;
                toSwap.IndexID = index;

                //indicesBuffer.EndWrite<int>(1);
            }

            SetNumRevealers();
#if UNITY_EDITOR
            Profiler.EndSample();
#endif
        }

        private RevealerStruct[] _revealerDataToSet = new RevealerStruct[1];
        public void UpdateRevealerData(int id, RevealerStruct data, int numHits, float[] radii, float[] distances, bool[] hits)
        {
#if UNITY_EDITOR
            Profiler.BeginSample("write to compute buffers");
#endif
            //setAnglesBuffersJobHandle.Complete();
            data.StartIndex = id * MaxPossibleSegmentsPerRevealer;
            _revealerDataToSet[0] = data;
            CircleBuffer.SetData(_revealerDataToSet, 0, id, 1);
            //_circleArray = circleBuffer.BeginWrite<CircleStruct>(id, 1);
            //_circleArray[0] = data;
            //circleBuffer.EndWrite<CircleStruct>(1);

            if (numHits > MaxPossibleSegmentsPerRevealer)
            {
                Debug.LogError($"the revealer is trying to register {numHits} segments. this is more than was set by maxPossibleSegmentsPerRevealer");
                return;
            }
            for (int i = 0; i < numHits; i++)
            {
                anglesArray[i].angle = radii[i];
                anglesArray[i].length = distances[i];
                anglesArray[i].cutShort = hits[i] ? 1 : 0;
                //AnglesNativeArray[i] = anglesArray[i];
            }

            AnglesBuffer.SetData(anglesArray, 0, id * MaxPossibleSegmentsPerRevealer, numHits);
            //the following lines of code should work in theory, however due to a unity bug, are going to be put on hold for a little bit.
            //_angleArray = anglesBuffer.BeginWrite<ConeEdgeStruct>(id * maxPossibleSegmentsPerRevealer, radii.Length);
            //setAnglesBuffersJob.AnglesArray = _angleArray;
            //setAnglesBuffersJob.Angles = AnglesNativeArray;
            //setAnglesBuffersJobHandle = setAnglesBuffersJob.Schedule(radii.Length, 128);
            //setAnglesBuffersJobHandle.Complete();
            //anglesBuffer.EndWrite<ConeEdgeStruct>(radii.Length);

#if UNITY_EDITOR
            Profiler.EndSample();
#endif
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct SetAnglesBuffersJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<ConeEdgeStruct> Angles;
            [WriteOnly]
            public NativeArray<ConeEdgeStruct> AnglesArray;

            public void Execute(int index)
            {
                AnglesArray[index] = Angles[index];
            }
        }

        /// <summary>
        /// Test if provided point is currently visible.
        /// </summary>
        public static bool TestPointVisibility(Vector3 point)
        {
            for (int i = 0; i < _numRevealers; i++)
            {
                if (Revealers[i].TestPoint(point))
                    return true;
            }
            return false;
        }

        public void SetFowAppearance(FogOfWarAppearance AppearanceMode)
        {
            FogAppearance = AppearanceMode;
            if (!Application.isPlaying)
                return;

            enabled = false;
            enabled = true;
        }

        public FogOfWarAppearance GetFowAppearance()
        {
            return FogAppearance;
        }

        public void InitFOWRT()
        {
            var tmp = RenderTexture.active;

            //RenderTextureFormat format = RenderTextureFormat.ARGBFloat;
            //RenderTextureFormat format = RenderTextureFormat.Default;
            RenderTextureFormat format = RenderTextureFormat.ARGBHalf;
            FOW_RT = new RenderTexture(FowResX, FowResY, 0, format, RenderTextureReadWrite.Linear);
            //Debug.Log(FOW_RT.filterMode);
            //Debug.Log(FOW_RT.antiAliasing);
            //Debug.Log(FOW_RT.anisoLevel);
            FOW_RT.antiAliasing = 8;
            FOW_RT.filterMode = FilterMode.Trilinear;
            FOW_RT.anisoLevel = 9;
            FOW_RT.Create();
            RenderTexture.active = FOW_RT;
            GL.Begin(GL.TRIANGLES);
            GL.Clear(true, true, new Color(0, 0, 0, 1 - InitialFogExplorationValue));
            GL.End();
            if (UseMiniMap && UIImage != null)
                UIImage.texture = FOW_RT;
            if (UseRegrow)
            {
                FOW_TEMP_RT = new RenderTexture(FOW_RT);
                FOW_TEMP_RT.Create();
            }

            RenderTexture.active = tmp;
        }

        public RenderTexture GetFOWRT()
        {
            return FOW_RT;
        }

        [Obsolete("Please use ClearFowTexture() instead")]
        public void ClearRegrowTexture()
        {
            ClearFowTexture();
        }

        public void ClearFowTexture()
        {
            var tmp = RenderTexture.active;

            RenderTexture.active = FOW_RT;
            GL.Begin(GL.TRIANGLES);
            GL.Clear(true, true, new Color(0, 0, 0, 1 - InitialFogExplorationValue));
            GL.End();
            RenderTexture.active = FOW_TEMP_RT;
            GL.Begin(GL.TRIANGLES);
            GL.Clear(true, true, new Color(0, 0, 0, 1 - InitialFogExplorationValue));
            GL.End();

            RenderTexture.active = tmp;
        }

        /// <summary>
        /// Retuns a byte array that you can save to a file
        /// </summary>
        public byte[] GetFowTextureSaveData()
        {
            var tex = new Texture2D(FOW_RT.width, FOW_RT.height, TextureFormat.RGBAHalf, mipChain: false, linear: true);

            var tmp = RenderTexture.active;

            RenderTexture.active = FOW_RT;
            tex.ReadPixels(new Rect(0, 0, FOW_RT.width, FOW_RT.height), 0, 0, false);
            tex.Apply(false, false);

            RenderTexture.active = tmp;

            Destroy(tex);

            return ImageConversion.EncodeToPNG(tex);
        }

        /// <summary>
        /// Loads the FOW exploration data from a byte array created with GetFowTextureSaveData
        /// </summary>
        public void LoadFowTextureData(byte[] save)
        {
            ClearFowTexture();

            Texture2D temp = new Texture2D(1, 1, TextureFormat.RGBAHalf, mipChain: false, linear: true);
            temp.LoadImage(save);

            Graphics.Blit(temp, FOW_RT);
        }
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(FogOfWarWorld))]
    public class FogOfWarWorldEditor : Editor
    {
        static class Styles
        {
            public static readonly GUIStyle rightLabel = new GUIStyle("RightLabel");
        }
        string[] FogUpdateMethods = new string[]
        {
            "Update", "Late Update"
        };
        string[] FowSampleOptions = new string[]
        {
            "Pixel-Perfect", "Texture Storage"
            //"Pixel-Perfect", "Texture", "Both"
        };
        string[] FogTypeOptions = new string[]
        {
            //"No Bleed", "No Bleed Soft", "Hard", "Soft"
            "Hard", "Soft"
        };
        string[] FogAppearanceOptions = new string[]
        {
            "Solid Color", "Gray Scale", "Blur", "Texture Color", "Outline (BETA)", "None - MiniMap Only"
        };
        string[] FogFadeOptions = new string[]
        {
            "Linear", "Exponential", "Smooth", "Smoother", "Smooth Step"
        };
        string[] FogBlendOptions = new string[]
        {
            "Maximum", "Additive"
        };
        string[] RevealerModeOptions = new string[]
        {
            "Every Frame", "N Per Frame", "Controlled Elsewhere"
        };
        string[] GamePlaneOptions = new string[]
        {
            "XZ", "XY", "ZY"
        };
        private BoxBoundsHandle m_BoundsHandle = new BoxBoundsHandle();
        void OnSceneGUI()
        {
            FogOfWarWorld fow = (FogOfWarWorld)target;
            if (fow.UseWorldBounds || fow.UseMiniMap || fow.FOWSamplingMode == FogOfWarWorld.FogSampleMode.Texture || fow.FOWSamplingMode == FogOfWarWorld.FogSampleMode.Both)
            {
                m_BoundsHandle.center = fow.WorldBounds.center;
                m_BoundsHandle.size = fow.WorldBounds.size;

                EditorGUI.BeginChangeCheck();
                m_BoundsHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(fow, "Change Bounds");

                    Bounds newBounds = new Bounds();
                    newBounds.center = m_BoundsHandle.center;
                    newBounds.size = m_BoundsHandle.size;
                    fow.UpdateWorldBounds(newBounds);
                    //fow.WorldBounds = newBounds;
                }
            }
        }
        public override void OnInspectorGUI()
        {
            //DrawDefaultInspector();
            FogOfWarWorld fow = (FogOfWarWorld)target;

            FogOfWarWorld.FowUpdateMethod updateMethod = fow.UpdateMethod;
            int selected = (int)updateMethod;
            selected = EditorGUILayout.Popup("Update Method", selected, FogUpdateMethods);
            updateMethod = (FogOfWarWorld.FowUpdateMethod)selected;
            if (fow.UpdateMethod != updateMethod)
            {
                Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                fow.UpdateMethod = updateMethod;
                fow.UpdateAllMaterialProperties();
            }

            EditorGUILayout.Space(20);

            EditorGUILayout.LabelField("Customization Options:");
            FogOfWarWorld.FogOfWarType fogType = fow.FogType;
            selected = (int)fogType;
            selected = EditorGUILayout.Popup("Fog Type", selected, FogTypeOptions);
            fogType = (FogOfWarWorld.FogOfWarType)selected;
            if (fow.FogType != fogType)
            {
                Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                fow.FogType = fogType;
                fow.UpdateAllMaterialProperties();
            }
            //if (fow.FogType == FogOfWarWorld.FogOfWarType.No_Bleed_Soft || fow.FogType == FogOfWarWorld.FogOfWarType.Soft)
            if (fow.FogType == FogOfWarWorld.FogOfWarType.Soft)
            {
                EditorGUILayout.LabelField("---Soft Fog Options---");
                FogOfWarWorld.FogOfWarFadeType fadeType = fow.FogFade;
                selected = (int)fadeType;
                selected = EditorGUILayout.Popup("Soft Fog Fade Mode", selected, FogFadeOptions);
                fadeType = (FogOfWarWorld.FogOfWarFadeType)selected;
                if (fow.FogFade != fadeType)
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.FogFade = fadeType;
                    fow.UpdateAllMaterialProperties();
                }
                if (fadeType == FogOfWarWorld.FogOfWarFadeType.Exponential)
                {
                    float fadeExp = fow.FogFadePower;
                    float newfadeExp = EditorGUILayout.FloatField("Fade Exponent: ", fadeExp);
                    if (fadeExp != newfadeExp)
                    {
                        Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                        fow.FogFadePower = newfadeExp;
                        fow.UpdateAllMaterialProperties();
                    }
                }
                FogOfWarWorld.FogOfWarBlendMode blendType = fow.BlendType;
                selected = (int)blendType;
                //selected = EditorGUILayout.Popup("Blend Type", selected, FogBlendOptions);
                selected = EditorGUILayout.Popup("Revealer Combination Mode", selected, FogBlendOptions);
                blendType = (FogOfWarWorld.FogOfWarBlendMode)selected;
                if (fow.BlendType != blendType)
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.BlendType = blendType;
                    fow.UpdateAllMaterialProperties();
                }

                //float softenDist = fow.SoftenDistance;
                //float newSoftenDist = EditorGUILayout.FloatField("Soften Distance: ", softenDist);
                //if (newSoftenDist != softenDist)
                //{
                //    fow.SoftenDistance = newSoftenDist;
                //    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                //    fow.UpdateFogConfig();
                //}

                float unobscuredsoftenDist = fow.UnobscuredSoftenDistance;
                float newUnobscuredSoftenDist = EditorGUILayout.FloatField("Un-Obscured area Soften Distance: ", unobscuredsoftenDist);
                if (!Mathf.Approximately(newUnobscuredSoftenDist, unobscuredsoftenDist))
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.UnobscuredSoftenDistance = Mathf.Max(0, newUnobscuredSoftenDist);
                    fow.UpdateAllMaterialProperties();
                }

                bool innerSoften = fow.UseInnerSoften;
                bool newinnerSoften = EditorGUILayout.Toggle("Soften Inner Edge? (BETA!)", innerSoften);
                if (newinnerSoften != innerSoften)
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.UseInnerSoften = newinnerSoften;
                    fow.UpdateAllMaterialProperties();
                }
                if (newinnerSoften)
                {
                    float softenAng = fow.InnerSoftenAngle;
                    float newSoftenAng = EditorGUILayout.FloatField("Inner Soften Angle: ", softenAng);
                    if (!Mathf.Approximately(newSoftenAng, softenAng))
                    {
                        Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                        fow.InnerSoftenAngle = Mathf.Max(0, newSoftenAng);
                        fow.UpdateAllMaterialProperties();
                    }
                }

                EditorGUILayout.LabelField("------");
            }

            bool AllowBleeding = fow.AllowBleeding;
            bool newAllowBleeding = EditorGUILayout.Toggle("Allow Bleeding?", AllowBleeding);
            if (newAllowBleeding != AllowBleeding)
            {
                Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                fow.AllowBleeding = newAllowBleeding;
                fow.UpdateAllMaterialProperties();
            }

            float oldExtraSightAmount = fow.SightExtraAmount;
            float newExtraSightAmount = EditorGUILayout.Slider("Revealer Extra Sight Distance: ", oldExtraSightAmount, -.01f, 1);
            if (!Mathf.Approximately(oldExtraSightAmount, newExtraSightAmount))
            {
                Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                fow.SightExtraAmount = newExtraSightAmount;
                fow.UpdateAllMaterialProperties();
            }

            if (fow.FogType == FogOfWarWorld.FogOfWarType.Soft)
            {
                float EdgeSoftenDistance = fow.EdgeSoftenDistance;
                //float newEdgeSoftenDist = EditorGUILayout.FloatField("Edge Softening Distance: ", EdgeSoftenDistance);
                float newEdgeSoftenDist = EditorGUILayout.FloatField("Revealer Extra Sight Distance Softening: ", EdgeSoftenDistance);
                if (!Mathf.Approximately(newEdgeSoftenDist, EdgeSoftenDistance))
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.EdgeSoftenDistance = Mathf.Max(0, newEdgeSoftenDist);
                    fow.UpdateAllMaterialProperties();
                }
            }

            float oldMaxDist = fow.MaxFogDistance;
            float newMaxDist = EditorGUILayout.Slider("Max Fog Distance: ", oldMaxDist, 0, 10000);
            if (!Mathf.Approximately(oldMaxDist, newMaxDist))
            {
                Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                fow.MaxFogDistance = newMaxDist;
                fow.UpdateAllMaterialProperties();
            }

            bool invertEffect = fow.InvertFowEffect;
            bool newInvertEffect = EditorGUILayout.Toggle("Invert Fow Effect?", invertEffect);
            if (newInvertEffect != invertEffect)
            {
                Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                fow.InvertFowEffect = newInvertEffect;
                fow.UpdateAllMaterialProperties();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("----");
            EditorGUILayout.Space(5);

            bool pixelate = fow.PixelateFog;
            bool newpixelate = EditorGUILayout.Toggle("Pixelate Fog?", pixelate);
            if (newpixelate != pixelate)
            {
                Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                fow.PixelateFog = newpixelate;
                fow.UpdateAllMaterialProperties();
            }
            if (newpixelate)
            {
                bool WSpixelate = fow.WorldSpacePixelate;
                bool newWSpixelate = EditorGUILayout.Toggle("- Use World Space?", WSpixelate);
                if (newWSpixelate != WSpixelate)
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.WorldSpacePixelate = newWSpixelate;
                    fow.UpdateAllMaterialProperties();
                }
                float oldPixelateSize = fow.PixelDensity;
                float newPixelateSize = EditorGUILayout.FloatField("- Pixel Density: ", oldPixelateSize);
                if (!Mathf.Approximately(newPixelateSize, oldPixelateSize))
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.PixelDensity = Mathf.Max(0, newPixelateSize);
                    //Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.UpdateAllMaterialProperties();
                }
                bool roundRevPos = fow.RoundRevealerPosition;
                bool newRoundRevPos = EditorGUILayout.Toggle("- Round Revealer Position?", roundRevPos);
                if (newRoundRevPos != roundRevPos)
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.RoundRevealerPosition = newRoundRevPos;
                    fow.UpdateAllMaterialProperties();
                }
                Vector2 oldOffset = fow.PixelGridOffset;
                Vector2 newOffset = EditorGUILayout.Vector2Field("- Pixel Grid Offset: ", oldOffset);
                newOffset = new Vector2(Mathf.Clamp(newOffset.x, -.5f, .5f), Mathf.Clamp(newOffset.y, -.5f, .5f));
                if (oldOffset != newOffset)
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.PixelGridOffset = newOffset;
                    fow.UpdateAllMaterialProperties();
                }
            }

            if (fow.FogType == FogOfWarWorld.FogOfWarType.Soft)
            {
                bool ditherFog = fow.UseDithering;
                bool newDithering = EditorGUILayout.Toggle("Use Dithering?", ditherFog);
                if (newDithering != ditherFog)
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.UseDithering = newDithering;
                    fow.UpdateAllMaterialProperties();
                }
                if (newDithering)
                {
                    float ditherSize = fow.DitherSize;
                    float newDitherSize = EditorGUILayout.FloatField("Dithering Size: ", ditherSize);
                    if (!Mathf.Approximately(newDitherSize, ditherSize))
                    {
                        Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                        fow.DitherSize = Mathf.Max(0, newDitherSize);
                        fow.UpdateAllMaterialProperties();
                    }
                }
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("----");
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("FOW Rendering Options:");

            FogOfWarWorld.FogOfWarAppearance fogAppearance = fow.GetFowAppearance();
            selected = (int)fogAppearance;
            selected = EditorGUILayout.Popup("Fog Appearance", selected, FogAppearanceOptions);
            fogAppearance = (FogOfWarWorld.FogOfWarAppearance)selected;
            if (fow.GetFowAppearance() != fogAppearance)
            {
                //fow.FogAppearance = fogAppearance;
                Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                fow.SetFowAppearance(fogAppearance);
            }

            if (fow.GetFowAppearance() != FogOfWarWorld.FogOfWarAppearance.None)
            {
                Color unknownColor = fow.UnknownColor;
                Color newColor = EditorGUILayout.ColorField("Unknown Area Color: ", unknownColor);
                if (unknownColor != newColor)
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.UnknownColor = newColor;
                    fow.UpdateAllMaterialProperties();
                }
            }
            if (fow.GetFowAppearance() == FogOfWarWorld.FogOfWarAppearance.Solid_Color)
            {

            }
            else if (fow.GetFowAppearance() == FogOfWarWorld.FogOfWarAppearance.GrayScale)
            {

                float oldStrength = fow.SaturationStrength;
                float newStrength = EditorGUILayout.Slider("Unknown Area Saturation Strength: ", oldStrength, 0, 1);
                if (!Mathf.Approximately(oldStrength, newStrength))
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.SaturationStrength = newStrength;
                    fow.UpdateAllMaterialProperties();
                }
            }
            else if (fow.GetFowAppearance() == FogOfWarWorld.FogOfWarAppearance.Blur)
            {
                float oldBlur = fow.BlurStrength;
                float newBlur = EditorGUILayout.Slider("Unknown Area Blur Strength: ", oldBlur, -1, 1);
                if (!Mathf.Approximately(oldBlur, newBlur))
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.BlurStrength = newBlur;
                    fow.UpdateAllMaterialProperties();
                }

                //float oldBlurOffset = fow.blurPixelOffset;
                //float newBlurOffset = EditorGUILayout.Slider("Unknown Area Blur Pixel Offset: ", oldBlurOffset, 1.5f, 10);
                //if (oldBlurOffset != newBlurOffset)
                //{
                //    fow.blurPixelOffset = newBlurOffset;
                //    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                //    fow.updateFogConfiguration();
                //}
                float oldBlurOffset = fow.BlurDistanceScreenPercentMin;
                float newBlurOffset = EditorGUILayout.Slider("Min Screen Percent: ", oldBlurOffset, 0, 2);
                if (!Mathf.Approximately(oldBlurOffset, newBlurOffset))
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.BlurDistanceScreenPercentMin = newBlurOffset;
                    fow.UpdateAllMaterialProperties();
                }

                oldBlurOffset = fow.BlurDistanceScreenPercentMax;
                newBlurOffset = EditorGUILayout.Slider("Max Screen Percent: ", oldBlurOffset, 0, 2);
                if (oldBlurOffset != newBlurOffset)
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.BlurDistanceScreenPercentMax = newBlurOffset;
                    fow.UpdateAllMaterialProperties();
                }

                int oldBlurSamples = fow.BlurSamples;
                int newBlurSamples = EditorGUILayout.IntSlider("Num Blur Samples: ", oldBlurSamples, 6, 18);
                if (oldBlurSamples != newBlurSamples)
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.BlurSamples = newBlurSamples;
                    fow.UpdateAllMaterialProperties();
                }
            }
            else if (fow.GetFowAppearance() == FogOfWarWorld.FogOfWarAppearance.Texture_Sample)
            {
                Texture2D oldTexture = fow.FogTexture;
                Texture2D newTexture = (Texture2D)EditorGUILayout.ObjectField("Fog Of War Texture: ", oldTexture, typeof(Texture2D), false);
                if (newTexture != oldTexture)
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.FogTexture = newTexture;
                    fow.UpdateAllMaterialProperties();
                }

                bool useTriplanar = fow.UseTriplanar;
                bool newUseTriplanar = EditorGUILayout.Toggle("Use Triplanar Sampling?", useTriplanar);
                //bool newUseBounds = false;
                if (useTriplanar != newUseTriplanar)
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.UseTriplanar = newUseTriplanar;
                    fow.UpdateAllMaterialProperties();
                }

                Vector2 oldTiling = fow.FogTextureTiling;
                Vector2 newTiling = EditorGUILayout.Vector2Field("Texture Tiling: ", oldTiling);
                if (oldTiling != newTiling)
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.FogTextureTiling = newTiling;
                    fow.UpdateAllMaterialProperties();
                }

                Vector2 oldSpeed = fow.FogScrollSpeed;
                Vector2 newSpeed = EditorGUILayout.Vector2Field("Texture Scroll Speed: ", oldSpeed);
                if (oldSpeed != newSpeed)
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.FogScrollSpeed = newSpeed;
                    fow.UpdateAllMaterialProperties();
                }
            }
            else if (fow.GetFowAppearance() == FogOfWarWorld.FogOfWarAppearance.Outline)
            {
                float oldThickness = fow.OutlineThickness;
                float newThickness = EditorGUILayout.FloatField("Outline Thickness: ", oldThickness);
                if (!Mathf.Approximately(oldThickness, newThickness))
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.OutlineThickness = newThickness;
                    fow.UpdateAllMaterialProperties();
                }
            }

            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("------------------");
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("FOW Technical Options:");

            FogOfWarWorld.FogSampleMode sampleMode = fow.FOWSamplingMode;
            selected = (int)sampleMode;
            selected = EditorGUILayout.Popup("Fog Sample Mode", selected, FowSampleOptions);
            sampleMode = (FogOfWarWorld.FogSampleMode)selected;
            if (fow.FOWSamplingMode != sampleMode)
            {
                Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                fow.FOWSamplingMode = sampleMode;
                fow.UpdateAllMaterialProperties();
            }
            if (fow.FOWSamplingMode == FogOfWarWorld.FogSampleMode.Texture || fow.FOWSamplingMode == FogOfWarWorld.FogSampleMode.Both)
            {
                EditorGUILayout.LabelField("Texture Sampling Mode Options:");
                bool useConstantBlur = fow.UseConstantBlur;
                bool newUseConstantBlur = EditorGUILayout.Toggle("--Use Blur?", useConstantBlur);
                //bool newUseBounds = false;
                if (useConstantBlur != newUseConstantBlur)
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.UseConstantBlur = newUseConstantBlur;
                    fow.UpdateAllMaterialProperties();
                }
                if (newUseConstantBlur)
                {
                    int oldCBlurQual = fow.ConstantTextureBlurQuality;
                    int newCBlurQual = EditorGUILayout.IntSlider("--Texture Blur Quality: ", oldCBlurQual, 1, 6);
                    if (oldCBlurQual != newCBlurQual)
                    {
                        Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                        fow.ConstantTextureBlurQuality = newCBlurQual;
                        fow.UpdateAllMaterialProperties();
                    }
                    float oldCBlurAmmount = fow.ConstantTextureBlurAmount;
                    float newCBlurAmount = EditorGUILayout.Slider("--Texture Blur Amount: ", oldCBlurAmmount, 0, 5);
                    if (!Mathf.Approximately(oldCBlurAmmount, newCBlurAmount))
                    {
                        Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                        fow.ConstantTextureBlurAmount = newCBlurAmount;
                        fow.UpdateAllMaterialProperties();
                    }
                }

                EditorGUILayout.Space(20);
            }

            bool useWorldBounds = fow.UseWorldBounds;
            bool newUseBounds = EditorGUILayout.Toggle("Use World Bounds?", useWorldBounds);
            //bool newUseBounds = false;
            if (useWorldBounds != newUseBounds)
            {
                Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                fow.UseWorldBounds = newUseBounds;
                fow.UpdateAllMaterialProperties();
            }
            if (newUseBounds)
            {
                float boundSoftendistance = fow.WorldBoundsSoftenDistance;
                float newboundSoftendistance = EditorGUILayout.Slider("--World Bounds Soften Distance:", boundSoftendistance, 0, 5);
                if (!Mathf.Approximately(boundSoftendistance, newboundSoftendistance))
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.WorldBoundsSoftenDistance = newboundSoftendistance;
                    fow.UpdateAllMaterialProperties();
                }

                float boundsInfluence = fow.WorldBoundsInfluence;
                float newboundsInfluence = EditorGUILayout.Slider("--World Bounds Influence:", boundsInfluence, 0, 1);
                if (!Mathf.Approximately(boundsInfluence, newboundsInfluence))
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.WorldBoundsInfluence = newboundsInfluence;
                    fow.UpdateAllMaterialProperties();
                }
            }
            EditorGUILayout.Space(10);
            bool UseMiniMap = fow.UseMiniMap;
            bool newUseMiniMap = EditorGUILayout.Toggle("Enable Mini-Map?", UseMiniMap);
            //bool newUseBounds = false;
            if (UseMiniMap != newUseMiniMap)
            {
                Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                fow.UseMiniMap = newUseMiniMap;
                fow.UpdateAllMaterialProperties();
            }
            if (newUseMiniMap)
            {
                Color mapColor = fow.MiniMapColor;
                Color newMapColor = EditorGUILayout.ColorField("--MiniMap Color: ", mapColor);
                if (mapColor != newMapColor)
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.MiniMapColor = newMapColor;
                    fow.UpdateAllMaterialProperties();
                }

                RawImage oldReference = fow.UIImage;
                RawImage newReference = (RawImage)EditorGUILayout.ObjectField("--Minimap UI Raw Image Reference: ", oldReference, typeof(RawImage), true);
                if (newReference != oldReference)
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.UIImage = newReference;
                    fow.UpdateAllMaterialProperties();
                }
            }

            if (newUseBounds || newUseMiniMap || fow.FOWSamplingMode == FogOfWarWorld.FogSampleMode.Texture || fow.FOWSamplingMode == FogOfWarWorld.FogSampleMode.Both)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Bounds:");
                Vector3 WorldBoundsCenter = fow.WorldBounds.center;
                Vector3 newWorldBoundsCenter = EditorGUILayout.Vector3Field("--Center: ", WorldBoundsCenter);
                if (WorldBoundsCenter != newWorldBoundsCenter)
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.WorldBounds.center = newWorldBoundsCenter;
                    fow.InitFOWRT();
                    fow.UpdateAllMaterialProperties();
                }
                Vector3 WorldBoundsExtents = fow.WorldBounds.extents;
                Vector3 newWorldBoundsExtents = EditorGUILayout.Vector3Field("--Extents: ", WorldBoundsExtents);
                if (WorldBoundsExtents != newWorldBoundsExtents)
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.WorldBounds.extents = newWorldBoundsExtents;
                    fow.InitFOWRT();
                    fow.UpdateAllMaterialProperties();
                }
            }
            if (newUseMiniMap || fow.FOWSamplingMode == FogOfWarWorld.FogSampleMode.Texture || fow.FOWSamplingMode == FogOfWarWorld.FogSampleMode.Both)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("FOW Texture Options (either cause you are using a minimap, or because your sampling mode uses a texture, or both)");

                int MiniMapResX = fow.FowResX;
                int newMiniMapResX = EditorGUILayout.IntSlider("--FOW Res X: ", MiniMapResX, 128, 2048);
                if (MiniMapResX != newMiniMapResX)
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.FowResX = newMiniMapResX;
                    fow.InitFOWRT();
                    fow.UpdateAllMaterialProperties();
                }
                int MiniMapResY = fow.FowResY;
                int newMiniMapResY = EditorGUILayout.IntSlider("--FOW Res Y: ", MiniMapResY, 128, 2048);
                if (MiniMapResY != newMiniMapResY)
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.FowResY = newMiniMapResY;
                    fow.InitFOWRT();
                    fow.UpdateAllMaterialProperties();
                }

                EditorGUILayout.Space(10);

                bool useRegrow = fow.UseRegrow;
                bool newUseRegrow = EditorGUILayout.Toggle("Use Regrow?", useRegrow);
                //bool newUseBounds = false;
                if (useRegrow != newUseRegrow)
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.UseRegrow = newUseRegrow;
                    fow.InitFOWRT();
                    fow.UpdateAllMaterialProperties();
                }
                if (newUseRegrow)
                {
                    bool useFadeIn = fow.RevealerFadeIn;
                    bool newUseFadeIn = EditorGUILayout.Toggle("--Use Revealer Fade-In?", useFadeIn);
                    if (useFadeIn != newUseFadeIn)
                    {
                        Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                        fow.RevealerFadeIn = newUseFadeIn;
                        fow.UpdateAllMaterialProperties();
                    }

                    float oldRegrowSpeed = fow.FogRegrowSpeed;
                    float newRegrowSpeed = EditorGUILayout.Slider("--Fog Fade Speed: ", oldRegrowSpeed, 0, 10);
                    if (!Mathf.Approximately(oldRegrowSpeed, newRegrowSpeed))
                    {
                        Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                        fow.FogRegrowSpeed = newRegrowSpeed;
                        fow.UpdateAllMaterialProperties();
                    }
                    float oldInitVal = fow.InitialFogExplorationValue;
                    float newInitVal = EditorGUILayout.Slider("--Initial Fog Exploration: ", oldInitVal, 0, 1);
                    if (!Mathf.Approximately(oldInitVal, newInitVal))
                    {
                        Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                        fow.InitialFogExplorationValue = newInitVal;
                        fow.UpdateAllMaterialProperties();
                    }
                    float oldRegrowMax = fow.MaxFogRegrowAmount;
                    float newRegrowMax = EditorGUILayout.Slider("--Max Fog Regrow Amount: ", oldRegrowMax, 0, 1);
                    if (!Mathf.Approximately(oldRegrowMax, newRegrowMax))
                    {
                        Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                        fow.MaxFogRegrowAmount = newRegrowMax;
                        fow.UpdateAllMaterialProperties();
                    }
                }
            }

            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("------------------");
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Utility Options (cant be changed at runtime)");

            FogOfWarWorld.RevealerUpdateMethod revealerMode = fow.RevealerUpdateMode;
            selected = (int)revealerMode;
            selected = EditorGUILayout.Popup("Revealer Update Mode", selected, RevealerModeOptions);
            revealerMode = (FogOfWarWorld.RevealerUpdateMethod)selected;
            if (fow.RevealerUpdateMode != revealerMode)
            {
                Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                fow.RevealerUpdateMode = revealerMode;
            }

            if (fow.RevealerUpdateMode == FogOfWarWorld.RevealerUpdateMethod.N_Per_Frame)
            {
                int _numRevealersPerFrame = fow.MaxNumRevealersPerFrame;
                int new_numRevealersPerFrame = EditorGUILayout.IntField("Num Revealers Per Frame: ", _numRevealersPerFrame);
                if (_numRevealersPerFrame != new_numRevealersPerFrame)
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.MaxNumRevealersPerFrame = new_numRevealersPerFrame;
                }
            }

            int max_numRevealers = fow.MaxPossibleRevealers;
            int newmax_numRevealers = EditorGUILayout.IntField("Max Num Revealers: ", max_numRevealers);
            if (max_numRevealers != newmax_numRevealers)
            {
                Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                fow.MaxPossibleRevealers = newmax_numRevealers;
            }

            int maxNumSegments = fow.MaxPossibleSegmentsPerRevealer;
            int newmaxNumSegments = EditorGUILayout.IntField("Max Num Segments Per Revealer: ", maxNumSegments);
            if (newmaxNumSegments != maxNumSegments)
            {
                Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                fow.MaxPossibleSegmentsPerRevealer = newmaxNumSegments;
            }

            //bool oldAllowMinDist = fow.AllowMinimumDistance;
            //bool newAllowMinDist = EditorGUILayout.Toggle("Enable Minimum Distance To Revealers? ", oldAllowMinDist);
            //if (oldAllowMinDist != newAllowMinDist)
            //{
            //    fow.AllowMinimumDistance = newAllowMinDist;
            //    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
            //    fow.UpdateFogConfig();
            //}

            bool is2d = fow.is2D;
            bool new2d = EditorGUILayout.Toggle("Is 2D?", is2d);
            if (is2d != new2d)
            {
                Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                fow.is2D = new2d;
                fow.UpdateAllMaterialProperties();
            }

            if (!new2d)
            {
                FogOfWarWorld.GamePlane plane = fow.gamePlane;
                selected = (int)plane;
                selected = EditorGUILayout.Popup("Game Plane", selected, GamePlaneOptions);
                plane = (FogOfWarWorld.GamePlane)selected;
                if (fow.gamePlane != plane)
                {
                    Undo.RegisterCompleteObjectUndo(fow, "Change FOW parameters");
                    fow.gamePlane = plane;
                }
            }
        }
    }
#endif
}