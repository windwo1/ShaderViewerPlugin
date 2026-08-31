using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D11;
using Frosty.Core.Viewport;
using FrostySdk;
using System.IO;
using FrostySdk.Managers;
using FrostySdk.IO;
using FrostySdk.Resources;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Frosty.Hash;
using DXUT = Frosty.Core.Viewport.DXUT;
using Frosty.Core.Screens;
using Frosty.Core;
using MeshSetPlugin.Render;
using ShaderLibrary = MeshSetPlugin.Render.ShaderLibrary;
using MeshRenderShape = MeshSetPlugin.Render.MeshRenderShape;
using Shader = MeshSetPlugin.Render.Shader;
using MeshRenderBase = MeshSetPlugin.Render.MeshRenderBase;
using MeshRenderPath = MeshSetPlugin.Render.MeshRenderPath;
using System.Diagnostics;

namespace MeshSetPlugin.Screens
{
    internal static class Kernel32
    {
        [DllImport("kernel32.dll", EntryPoint = "LoadLibraryEx", SetLastError = true)]
        public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, uint dwFlags);

        [DllImport("kernel32", EntryPoint = "GetProcAddress", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", EntryPoint = "FreeLibrary", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeLibrary(IntPtr hModule);
    }

    internal class LoadLibraryHandle
    {
        IntPtr handle;
        public LoadLibraryHandle(string lib)
        {
            handle = Kernel32.LoadLibraryEx(lib, IntPtr.Zero, 0);
        }
        public static implicit operator IntPtr(LoadLibraryHandle value) { return value.handle; }
        ~LoadLibraryHandle()
        {
            Kernel32.FreeLibrary(handle);
        }
    }

    internal enum RENDERDOC_CaptureOption
    {
        // Allow the application to enable vsync
        //
        // Default - enabled
        //
        // 1 - The application can enable or disable vsync at will
        // 0 - vsync is force disabled
        eRENDERDOC_Option_AllowVSync = 0,

        // Allow the application to enable fullscreen
        //
        // Default - enabled
        //
        // 1 - The application can enable or disable fullscreen at will
        // 0 - fullscreen is force disabled
        eRENDERDOC_Option_AllowFullscreen = 1,

        // Record API debugging events and messages
        //
        // Default - disabled
        //
        // 1 - Enable built-in API debugging features and records the results into
        //     the capture logfile, which is matched up with events on replay
        // 0 - no API debugging is forcibly enabled
        eRENDERDOC_Option_APIValidation = 2,
        eRENDERDOC_Option_DebugDeviceMode = 2,    // deprecated name of this enum

        // Capture CPU callstacks for API events
        //
        // Default - disabled
        //
        // 1 - Enables capturing of callstacks
        // 0 - no callstacks are captured
        eRENDERDOC_Option_CaptureCallstacks = 3,

        // When capturing CPU callstacks, only capture them from drawcalls.
        // This option does nothing without the above option being enabled
        //
        // Default - disabled
        //
        // 1 - Only captures callstacks for drawcall type API events.
        //     Ignored if CaptureCallstacks is disabled
        // 0 - Callstacks, if enabled, are captured for every event.
        eRENDERDOC_Option_CaptureCallstacksOnlyDraws = 4,

        // Specify a delay in seconds to wait for a debugger to attach, after
        // creating or injecting into a process, before continuing to allow it to run.
        //
        // 0 indicates no delay, and the process will run immediately after injection
        //
        // Default - 0 seconds
        //
        eRENDERDOC_Option_DelayForDebugger = 5,

        // Verify any writes to mapped buffers, by checking the memory after the
        // bounds of the returned pointer to detect any modification.
        //
        // Default - disabled
        //
        // 1 - Verify any writes to mapped buffers
        // 0 - No verification is performed, and overwriting bounds may cause
        //     crashes or corruption in RenderDoc
        eRENDERDOC_Option_VerifyMapWrites = 6,

        // Hooks any system API calls that create child processes, and injects
        // RenderDoc into them recursively with the same options.
        //
        // Default - disabled
        //
        // 1 - Hooks into spawned child processes
        // 0 - Child processes are not hooked by RenderDoc
        eRENDERDOC_Option_HookIntoChildren = 7,

        // By default RenderDoc only includes resources in the final logfile necessary
        // for that frame, this allows you to override that behaviour.
        //
        // Default - disabled
        //
        // 1 - all live resources at the time of capture are included in the log
        //     and available for inspection
        // 0 - only the resources referenced by the captured frame are included
        eRENDERDOC_Option_RefAllResources = 8,

        // By default RenderDoc skips saving initial states for resources where the
        // previous contents don't appear to be used, assuming that writes before
        // reads indicate previous contents aren't used.
        //
        // Default - disabled
        //
        // 1 - initial contents at the start of each captured frame are saved, even if
        //     they are later overwritten or cleared before being used.
        // 0 - unless a read is detected, initial contents will not be saved and will
        //     appear as black or empty data.
        eRENDERDOC_Option_SaveAllInitials = 9,

        // In APIs that allow for the recording of command lists to be replayed later,
        // RenderDoc may choose to not capture command lists before a frame capture is
        // triggered, to reduce overheads. This means any command lists recorded once
        // and replayed many times will not be available and may cause a failure to
        // capture.
        //
        // Note this is only true for APIs where multithreading is difficult or
        // discouraged. Newer APIs like Vulkan and D3D12 will ignore this option
        // and always capture all command lists since the API is heavily oriented
        // around it and the overheads have been reduced by API design.
        //
        // 1 - All command lists are captured from the start of the application
        // 0 - Command lists are only captured if their recording begins during
        //     the period when a frame capture is in progress.
        eRENDERDOC_Option_CaptureAllCmdLists = 10,

        // Mute API debugging output when the API validation mode option is enabled
        //
        // Default - enabled
        //
        // 1 - Mute any API debug messages from being displayed or passed through
        // 0 - API debugging is displayed as normal
        eRENDERDOC_Option_DebugOutputMute = 11,

    }

    internal enum RENDERDOC_InputButton
    {
        // '0' - '9' matches ASCII values
        eRENDERDOC_Key_0 = 0x30,
        eRENDERDOC_Key_1 = 0x31,
        eRENDERDOC_Key_2 = 0x32,
        eRENDERDOC_Key_3 = 0x33,
        eRENDERDOC_Key_4 = 0x34,
        eRENDERDOC_Key_5 = 0x35,
        eRENDERDOC_Key_6 = 0x36,
        eRENDERDOC_Key_7 = 0x37,
        eRENDERDOC_Key_8 = 0x38,
        eRENDERDOC_Key_9 = 0x39,

        // 'A' - 'Z' matches ASCII values
        eRENDERDOC_Key_A = 0x41,
        eRENDERDOC_Key_B = 0x42,
        eRENDERDOC_Key_C = 0x43,
        eRENDERDOC_Key_D = 0x44,
        eRENDERDOC_Key_E = 0x45,
        eRENDERDOC_Key_F = 0x46,
        eRENDERDOC_Key_G = 0x47,
        eRENDERDOC_Key_H = 0x48,
        eRENDERDOC_Key_I = 0x49,
        eRENDERDOC_Key_J = 0x4A,
        eRENDERDOC_Key_K = 0x4B,
        eRENDERDOC_Key_L = 0x4C,
        eRENDERDOC_Key_M = 0x4D,
        eRENDERDOC_Key_N = 0x4E,
        eRENDERDOC_Key_O = 0x4F,
        eRENDERDOC_Key_P = 0x50,
        eRENDERDOC_Key_Q = 0x51,
        eRENDERDOC_Key_R = 0x52,
        eRENDERDOC_Key_S = 0x53,
        eRENDERDOC_Key_T = 0x54,
        eRENDERDOC_Key_U = 0x55,
        eRENDERDOC_Key_V = 0x56,
        eRENDERDOC_Key_W = 0x57,
        eRENDERDOC_Key_X = 0x58,
        eRENDERDOC_Key_Y = 0x59,
        eRENDERDOC_Key_Z = 0x5A,

        // leave the rest of the ASCII range free
        // in case we want to use it later
        eRENDERDOC_Key_NonPrintable = 0x100,

        eRENDERDOC_Key_Divide,
        eRENDERDOC_Key_Multiply,
        eRENDERDOC_Key_Subtract,
        eRENDERDOC_Key_Plus,

        eRENDERDOC_Key_F1,
        eRENDERDOC_Key_F2,
        eRENDERDOC_Key_F3,
        eRENDERDOC_Key_F4,
        eRENDERDOC_Key_F5,
        eRENDERDOC_Key_F6,
        eRENDERDOC_Key_F7,
        eRENDERDOC_Key_F8,
        eRENDERDOC_Key_F9,
        eRENDERDOC_Key_F10,
        eRENDERDOC_Key_F11,
        eRENDERDOC_Key_F12,

        eRENDERDOC_Key_Home,
        eRENDERDOC_Key_End,
        eRENDERDOC_Key_Insert,
        eRENDERDOC_Key_Delete,
        eRENDERDOC_Key_PageUp,
        eRENDERDOC_Key_PageDn,

        eRENDERDOC_Key_Backspace,
        eRENDERDOC_Key_Tab,
        eRENDERDOC_Key_PrtScrn,
        eRENDERDOC_Key_Pause,

        eRENDERDOC_Key_Max,
    }

    internal enum RENDERDOC_OverlayBits : uint
    {
        // This single bit controls whether the overlay is enabled or disabled globally
        eRENDERDOC_Overlay_Enabled = 0x1,

        // Show the average framerate over several seconds as well as min/max
        eRENDERDOC_Overlay_FrameRate = 0x2,

        // Show the current frame number
        eRENDERDOC_Overlay_FrameNumber = 0x4,

        // Show a list of recent captures, and how many captures have been made
        eRENDERDOC_Overlay_CaptureList = 0x8,

        // Default values for the overlay mask
        eRENDERDOC_Overlay_Default = (eRENDERDOC_Overlay_Enabled | eRENDERDOC_Overlay_FrameRate |
                                      eRENDERDOC_Overlay_FrameNumber | eRENDERDOC_Overlay_CaptureList),

        // Enable all bits
        eRENDERDOC_Overlay_All = 0xFFFFFFFF,

        // Disable all bits
        eRENDERDOC_Overlay_None = 0,
    }

    internal static class RenderDoc
    {
        private static LoadLibraryHandle handle;
        private delegate int GetApiFuncPtr(int version, out IntPtr ptrs);

        public static Api GetAPI(int version)
        {
            handle = new LoadLibraryHandle("thirdparty/renderdoc.dll");
            if (handle == IntPtr.Zero)
                return null;

            GetApiFuncPtr func = Marshal.GetDelegateForFunctionPointer<GetApiFuncPtr>(Kernel32.GetProcAddress(handle, "RENDERDOC_GetAPI"));

            func(version, out IntPtr ptrs);

            return new Api(ptrs);
        }

        public class Api
        {
            private IntPtr ptr;
            public Api(IntPtr inPtr)
            {
                ptr = inPtr;
            }

            /*
              pRENDERDOC_GetAPIVersion GetAPIVersion; = 0x00

              pRENDERDOC_SetCaptureOptionU32 SetCaptureOptionU32; = 0x08
              pRENDERDOC_SetCaptureOptionF32 SetCaptureOptionF32; = 0x10

              pRENDERDOC_GetCaptureOptionU32 GetCaptureOptionU32; = 0x18
              pRENDERDOC_GetCaptureOptionF32 GetCaptureOptionF32; = 0x20

              pRENDERDOC_SetFocusToggleKeys SetFocusToggleKeys; = 0x28
              pRENDERDOC_SetCaptureKeys SetCaptureKeys; = 0x30

              pRENDERDOC_GetOverlayBits GetOverlayBits; = 0x38
              pRENDERDOC_MaskOverlayBits MaskOverlayBits; = 0x40

              pRENDERDOC_Shutdown Shutdown; = 0x48
              pRENDERDOC_UnloadCrashHandler UnloadCrashHandler; = 0x50

              pRENDERDOC_SetLogFilePathTemplate SetLogFilePathTemplate; = 0x58 
              pRENDERDOC_GetLogFilePathTemplate GetLogFilePathTemplate; = 0x60

              pRENDERDOC_GetNumCaptures GetNumCaptures; = 0x68
              pRENDERDOC_GetCapture GetCapture; = 0x70

              pRENDERDOC_TriggerCapture TriggerCapture; = 0x78

              pRENDERDOC_IsTargetControlConnected IsTargetControlConnected; = 0x80
              pRENDERDOC_LaunchReplayUI LaunchReplayUI; = 0x88

              pRENDERDOC_SetActiveWindow SetActiveWindow; = 0x90

              pRENDERDOC_StartFrameCapture StartFrameCapture; = 0x98
              pRENDERDOC_IsFrameCapturing IsFrameCapturing; = 0xA0
              pRENDERDOC_EndFrameCapture EndFrameCapture; = 0xA8
            */

            private delegate void GetAPIVersionFuncPtr(out int major, out int minor, out int rev);
            public void GetAPIVersion(out int major, out int minor, out int rev)
            {
                GetAPIVersionFuncPtr func = Marshal.GetDelegateForFunctionPointer<GetAPIVersionFuncPtr>(Marshal.ReadIntPtr(ptr, 0));
                func(out major, out minor, out rev);
            }

            private delegate void SetCaptureOptionU32FuncPtr(RENDERDOC_CaptureOption option, uint value);
            public void SetCaptureOptionU32(RENDERDOC_CaptureOption option, uint value)
            {
                SetCaptureOptionU32FuncPtr func = Marshal.GetDelegateForFunctionPointer<SetCaptureOptionU32FuncPtr>(Marshal.ReadIntPtr(ptr, 0x08));
                func(option, value);
            }

            private delegate uint GetOverlayBitsFuncPtr();
            public RENDERDOC_OverlayBits GetOverlayBits()
            {
                GetOverlayBitsFuncPtr func = Marshal.GetDelegateForFunctionPointer<GetOverlayBitsFuncPtr>(Marshal.ReadIntPtr(ptr, 0x38));
                return (RENDERDOC_OverlayBits)func();
            }

            private delegate void SetLogFilePathTemplateFuncPtr([MarshalAs(UnmanagedType.LPStr)] string path);
            public void SetLogFilePathTemplate(string path)
            {
                SetLogFilePathTemplateFuncPtr func = Marshal.GetDelegateForFunctionPointer<SetLogFilePathTemplateFuncPtr>(Marshal.ReadIntPtr(ptr, 0x58));
                func(path);
            }

            [return: MarshalAs(UnmanagedType.LPStr)]
            private delegate string GetLogFilePathTemplateFuncPtr();
            public string GetLogFilePathTemplate()
            {
                GetLogFilePathTemplateFuncPtr func = Marshal.GetDelegateForFunctionPointer<GetLogFilePathTemplateFuncPtr>(Marshal.ReadIntPtr(ptr, 0x60));
                return func();
            }

            private delegate void LaunchReplayUIFuncPtr(int connectTargetControl, [MarshalAs(UnmanagedType.LPStr)] string cmdLine);
            public void LaunchReplayUI(bool connectTC, string cmdLine)
            {
                LaunchReplayUIFuncPtr func = Marshal.GetDelegateForFunctionPointer<LaunchReplayUIFuncPtr>(Marshal.ReadIntPtr(ptr, 0x88));
                func((connectTC) ? 1 : 0, cmdLine);
            }

            private delegate void SetActiveWindowFuncPtr(IntPtr device, IntPtr wndHandle);
            public void SetActiveWindow(SharpDX.Direct3D11.Device device, IntPtr hWnd)
            {
                SetActiveWindowFuncPtr func = Marshal.GetDelegateForFunctionPointer<SetActiveWindowFuncPtr>(Marshal.ReadIntPtr(ptr, 0x90));
                func((device != null) ? device.NativePointer : IntPtr.Zero, hWnd);
            }

            private delegate void StartFrameCaptureFuncPtr(IntPtr device, IntPtr handle);
            public void StartFrameCapture(SharpDX.Direct3D11.Device device, IntPtr handle)
            {
                StartFrameCaptureFuncPtr func = Marshal.GetDelegateForFunctionPointer<StartFrameCaptureFuncPtr>(Marshal.ReadIntPtr(ptr, 0x98));
                func((device != null) ? device.NativePointer : IntPtr.Zero, handle);
            }

            private delegate void EndFrameCaptureFuncPtr(IntPtr device, IntPtr handle);
            public void EndFrameCapture(SharpDX.Direct3D11.Device device, IntPtr handle)
            {
                EndFrameCaptureFuncPtr func = Marshal.GetDelegateForFunctionPointer<EndFrameCaptureFuncPtr>(Marshal.ReadIntPtr(ptr, 0xa8));
                func((device != null) ? device.NativePointer : IntPtr.Zero, handle);
            }

            private delegate void TriggerCaptureFuncPtr();
            public void TriggerCapture()
            {
                TriggerCaptureFuncPtr func = Marshal.GetDelegateForFunctionPointer<TriggerCaptureFuncPtr>(Marshal.ReadIntPtr(ptr, 0x78));
                func();
            }
        }
    }

    public struct MeshRenderInstance
    {
        public MeshRenderBase RenderMesh;
        public Matrix Transform;
    }

    public struct FrameData
    {
        public Vector4 Time;
        public Vector4 ScreenSize;
        public Matrix ViewMatrix;
        public Matrix ProjMatrix;
        public Matrix ViewProjMatrix;
        public Matrix CrViewProjMatrix;
        public Matrix4x3[] NormalBasisTransforms;
        public Vector4 ProjectionKxKyKzKw;
        public Vector4 ExposureMultipliers;
        public Vector3 CameraPos;
    }

    public struct GlobalsData
    {
        public Vector2 VignetteScale;
        public float VignetteExponent;
        public Vector3 VignetteColor;
        public float VignetteOpacity;
    }

    public class ShaderRenderScreen : Screen
    {
        #region -- Shader Constants --
        protected struct ViewConstants
        {
            public Vector4 Time;
            public Vector4 ScreenSize;
            public Matrix ViewMatrix;
            public Matrix ProjMatrix;
            public Matrix ViewProjMatrix;
            public Matrix CrViewProjMatrix;
            public Matrix PrevViewProjMatrix;
            public Matrix CrPrevViewProjMatrix;
            public Matrix4x3 NormalBasisTransforms1;
            public Matrix4x3 NormalBasisTransforms2;
            public Matrix4x3 NormalBasisTransforms3;
            public Matrix4x3 NormalBasisTransforms4;
            public Matrix4x3 NormalBasisTransforms5;
            public Matrix4x3 NormalBasisTransforms6;
            public Vector4 ExposureMultipliers;
            public Vector4 CameraPos;
        }

        protected struct CommonConstants
        {
            public Matrix InvViewProjMatrix;
            public Matrix InvProjMatrix;
            public Vector4 CameraPos;
            public Vector4 InvScreenSize;
            public Vector4 ExposureMultipliers;
            public Matrix4x3 NormalBasisTransforms1;
            public Matrix4x3 NormalBasisTransforms2;
            public Matrix4x3 NormalBasisTransforms3;
            public Matrix4x3 NormalBasisTransforms4;
            public Matrix4x3 NormalBasisTransforms5;
            public Matrix4x3 NormalBasisTransforms6;
            public Vector4 LightProbeIntensity;

            public static float ComputeEV100(float aperture, float shutterTime, float ISO)
            {
                return (float)Math.Log((aperture * aperture) / shutterTime * 100 / ISO, 2);
            }

            public static float ConvertEV100ToExposure(float EV100)
            {
                float maxLuminance = 1.2f * (float)Math.Pow(2.0f, EV100);
                return 1.0f / maxLuminance;
            }

            public static float ComputeEV100FromAvgLuminance(float avgLuminance)
            {
                return (float)Math.Log(avgLuminance * 100.0f / 12.5f, 2);
            }

            //public static Vector2 ComputeExposure(float inAperture, float inShutterSpeed, float inISO)
            //{
            //    float exposure = ConvertEV100ToExposure(ComputeEV100(inAperture, inShutterSpeed, inISO));
            //    Vector2 outExposure = new Vector2();

            //    outExposure.X = exposure;
            //    outExposure.Y = 1.0f / exposure;

            //    return outExposure;
            //}

            public static Vector2 ComputeExposure(float avgLuminance, float min, float max)
            {
                float minEV100 = min;
                float maxEV100 = max;

                float EV100 = ComputeEV100FromAvgLuminance(avgLuminance);

                if (EV100 < minEV100) EV100 = minEV100;
                if (EV100 > maxEV100) EV100 = maxEV100;

                float exposure = ConvertEV100ToExposure(EV100);
                Vector2 outExposure = new Vector2
                {
                    X = avgLuminance,
                    Y = 1.0f / avgLuminance
                };


                return outExposure;
            }

            public static Vector4 ComputeExposureVec4(float avgLuminance, float min, float max)
            {
                float minEV100 = min;
                float maxEV100 = max;

                float EV100 = ComputeEV100FromAvgLuminance(avgLuminance);

                if (EV100 < minEV100) EV100 = minEV100;
                if (EV100 > maxEV100) EV100 = maxEV100;

                float exposure = ConvertEV100ToExposure(EV100);
                Vector4 outExposure = new Vector4
                {
                    X = exposure,
                    Y = 1.0f / exposure,
                    Z = exposure,
                    W = 1.0f / exposure
                };

                return outExposure;
            }
        }

        protected struct LightConstants
        {
            public Vector4 LightPosAndInvSqrRadius;
            public Vector4 LightColorAndIntensity;
        }

        protected struct FunctionConstants
        {
            public Matrix WorldMatrix;
            public Vector4 LightProbe1;
            public Vector4 LightProbe2;
            public Vector4 LightProbe3;
            public Vector4 LightProbe4;
            public Vector4 LightProbe5;
            public Vector4 LightProbe6;
            public Vector4 LightProbe7;
            public Vector4 LightProbe8;
            public Vector4 LightProbe9;
        }

        protected struct CubeMapConstants
        {
            public int CubeFace;
            public uint MipIndex;
            public uint NumMips;
            public uint Pad;
        }

        protected struct TableLookupConstants
        {
            public float LutSize;
            public float FlipY;
            public Vector2 Pad;
        }

        protected struct GlobalConstants
        {
            public Vector4 VignetteColor;
            public Vector3 VignetteParams;
            public uint Padding;
        }
        #endregion

        /// <summary>
        /// The list of meshes to be rendered in the next frame
        /// </summary>
        protected List<MeshRenderInstance> meshes;
        protected List<MeshRenderInstance> editorMeshes;
        protected List<LightRenderInstance> lights;

        /// <summary>
        /// The collection of GBuffers
        /// </summary>
        protected GBufferCollection gBufferCollection;

        // various libraries
        protected TextureLibrary textureLibrary;
        protected ShaderLibrary shaderLibrary;

        // constant buffers
        protected ConstantBuffer<ViewConstants> viewConstants;
        protected ConstantBuffer<FunctionConstants> functionConstants;
        protected ConstantBuffer<CommonConstants> commonConstants;
        protected ConstantBuffer<LightConstants> lightConstants;
        protected ConstantBuffer<CubeMapConstants> cubeMapConstants;
        protected ConstantBuffer<TableLookupConstants> lookupTableConstants;
        protected ConstantBuffer<GlobalConstants> globalConstants;
        protected SharpDX.Direct3D11.Buffer postProcessConstants;

        // resources
        protected BindableTexture normalBasisCubemapTexture;
        protected BindableTexture lightAccumulationTexture;
        protected BindableTexture preintegratedDFGTexture;
        protected BindableTexture blankTexture;
        protected BindableTexture whiteTexture;
        protected BindableCubeTexture preintegratedDLDTexture;
        protected BindableCubeTexture preintegratedSLDTexture;
        protected BindableTexture scaledSceneTexture;
        protected BindableTexture[] toneMapTextures = new BindableTexture[7];
        protected BindableTexture postProcessTexture;
        protected BindableTexture editorCompositeTexture;
        protected BindableDepthTexture editorCompositeDepthTexture;
        protected BindableTexture finalColorTexture;
        protected BindableDepthTexture selectionDepthTexture;
        protected BindableTexture selectionOutlineTexture;
        protected BindableTexture worldNormalsForHBAOTexture;
        protected BindableTexture brightPassTexture;
        protected BindableTexture blurTexture;
        protected BindableTexture bloomSourceTexture;
        protected BindableTexture[] bloomTextures = new BindableTexture[3];

        // light shaders
        protected PixelShader psSunLight;
        protected PixelShader psPointLight;
        protected PixelShader psSphereLight;

        // IBL shaders
        protected PixelShader psIntegrateDFG;
        protected PixelShader psIntegrateDiffuseLD;
        protected PixelShader psIntegrateSpecularLD;
        protected PixelShader psIBLRender;

        // utility shaders
        protected VertexShader vsFullscreenQuad;
        protected PixelShader psResolve;
        protected PixelShader psResolveDepthToMsaa;
        protected PixelShader psResolveWorldNormals;

        // post processing shaders
        protected PixelShader psDownscale4x4;
        protected PixelShader psSampleLumInitial;
        protected PixelShader psSampleLumIterative;
        protected PixelShader psSampleLumFinal;
        protected PixelShader psCalcAdaptedLum;
        protected PixelShader psLookupTable;
        protected PixelShader psEditorComposite;
        protected PixelShader psSelectionOutline;
        protected PixelShader psDebugRenderMode;
        protected PixelShader psBrightPass;
        protected PixelShader psGaussianBlur5x5;
        protected PixelShader psDownSample2x2;
        protected PixelShader psBloomBlur;
        protected PixelShader psRenderBloom;
        protected PixelShader psVignette;

        // txaa
        protected IntPtr txaaContext;
        protected IntPtr txaaMotionVectorGenerator;

        protected BindableTexture txaaMotionVectorsTexture;
        protected BindableTexture txaaFeedbackTeture;

        // shadows
        protected GFSDK_ShadowLib.Context shadowContext;
        protected GFSDK_ShadowLib.Map shadowMapHandle;
        protected GFSDK_ShadowLib.Buffer shadowBufferHandle;
        protected ShaderResourceView shadowSRV;

        // hbao
        protected GFSDK_SSAO.Context hbaoContext;

        public ShaderResourceView PreintegratedDFGTextureSRV => preintegratedDFGTexture.SRV;
        public ShaderResourceView NormalBasisCubemapTextureSRV => normalBasisCubemapTexture.SRV;
        public ShaderResourceView BlankTextureSRV => blankTexture.SRV;
        public ShaderResourceView WhiteTextureSRV => whiteTexture.SRV;

        #region -- Temporary Stuff --

        // everything here is mainly here for testing purposes and may be completely removed

        protected RenderCreateState2 RenderCreateState2 => new RenderCreateState2(Viewport.Device, textureLibrary, shaderLibrary, this);

        public DXUT.BaseCamera camera;

        public float CameraAperture { get; set; } = 16.0f;
        public float CameraShutterSpeed { get; set; } = 1 / 100.0f;
        public float CameraISO { get; set; } = 100.0f;

        public Vector3 SunPosition { get; set; } = new Vector3(10, 20, 20);
        public Vector3 SunColor { get; set; } = new Vector3(1, 1, 1);
        public float SunIntensity { get; set; } = 1000.0f;
        public float SunAngularRadius { get; set; } = 0.029f;

        public ShaderResourceView DistantLightProbe
        {
            get => distantLightProbe;
            set
            {
                distantLightProbe = value;
                if (value == null)
                    distantLightProbe = defaultDistantLightProbe;
                bRecalculateLightProbe = true;
            }
        }
        public GlobalsData Globals { get; set; }
        public float LightProbeIntensity { get; set; } = 1.0f;
        public ShaderResourceView LookupTable { get; set; }
        public ShaderResourceView Skybox { get; set; }
        public Vector4[] SHLightProbe { get; set; } = new Vector4[9];
        public DebugRenderMode RenderMode { get; set; }
        public bool GroundVisible { get; set; } = true;
        public bool GridVisible { get; set; } = true;
        public float TimeScale { get; set; } = 1.0f;
        public float MinEV100 { get; set; } = 8.0f;
        public float MaxEV100 { get; set; } = 20.0f;

        private ShaderResourceView distantLightProbe;
        private ShaderResourceView defaultDistantLightProbe;
        private bool bRecalculateLightProbe;

        private MeshRenderShape skySphere;
        private MeshRenderShape groundBox;
        private MeshRenderShape gridPlane;

        private Histogram luminanceHistogram = new Histogram(1);
        private double totalTime = 0.0;
        private double lastDeltaTime = 0.0;

        private const float NearPlane = 0.1f;
        private const float FarPlane = 1000000.0f;

        public int iDepthBias { get; set; } = 100;
        public float fSlopeScaledDepthBias { get; set; } = 5;
        public float fDistanceBiasMin { get; set; } = 0.00000001f;
        public float fDistanceBiasFactor { get; set; } = 0.00000001f;
        public float fDistanceBiasThreshold { get; set; } = 700.0f;
        public float fDistanceBiasPower { get; set; } = 0.3f;
        public float BloomStrength { get; set; } = 0.1f;

        public bool ShadowsEnabled;
        public bool HBAOEnabled;
        public bool TXAAEnabled;

        #endregion

#if FROSTY_DEVELOPER
        protected enum RenderDocCaptureState
        {
            NotStarted,
            BeginCapture,
            CaptureInProgress
        }

        /// <summary>
        /// Pointer to renderdoc api (can be null if dll not present)
        /// </summary>
        private RenderDoc.Api renderDocApi;
        protected RenderDocCaptureState renderDocCaptureState;
#endif

        // default costructor
        public ShaderRenderScreen()
        {
#if FROSTY_DEVELOPER
            InitializeRenderDoc();
#endif

            GroundVisible = Config.Get<bool>("MeshSetViewerShowFloor", true);
            GridVisible = Config.Get<bool>("MeshSetViewerShowGrid", true);
            //GroundVisible = Config.Get<bool>("MeshViewer", "ShowFloor", true);
            //GridVisible = Config.Get<bool>("MeshViewer", "ShowGrid", true);
        }

        #region -- Creation --
        /// <summary>
        /// Creates all buffers that are dependent on viewport size
        /// </summary>
        public override void CreateSizeDependentBuffers()
        {
            // initialize the gbuffers
            gBufferCollection = new GBufferCollection(Viewport.Device, Viewport.ViewportWidth, Viewport.ViewportHeight, new GBufferDescription[]
            {
                new GBufferDescription() { Format = SharpDX.DXGI.Format.R10G10B10A2_UNorm, ClearColor = new Color4(0,0,0,0), DebugName = "GBufferA" },
                new GBufferDescription() { Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm_SRgb, ClearColor = new Color4(0,0,0,0), DebugName = "GBufferB" },
                new GBufferDescription() { Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm, ClearColor = new Color4(0,0,0,0), DebugName = "GBufferC" },
                new GBufferDescription() { Format = SharpDX.DXGI.Format.R16G16B16A16_Float, ClearColor = new Color4(0,0,0,0), DebugName = "GBufferD" },
            });
            finalColorTexture = new BindableTexture(Viewport.Device, new Texture2DDescription()
            {
                ArraySize = 1,
                Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                Width = Viewport.ViewportWidth,
                Height = Viewport.ViewportHeight,
                MipLevels = 1,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            }, true, true);
            lightAccumulationTexture = new BindableTexture(Viewport.Device, new Texture2DDescription()
            {
                ArraySize = 1,
                Format = SharpDX.DXGI.Format.R16G16B16A16_Float,
                Width = Viewport.ViewportWidth,
                Height = Viewport.ViewportHeight,
                MipLevels = 1,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            }, true, true);

            int scaledWidth = (Viewport.ViewportWidth - (Viewport.ViewportWidth % 8)) / 4;
            if (scaledWidth < 1)
                scaledWidth = 1;
            int scaledHeight = (Viewport.ViewportHeight - (Viewport.ViewportHeight % 8)) / 4;
            if (scaledHeight < 1)
                scaledHeight = 1;

            scaledSceneTexture = new BindableTexture(Viewport.Device, new Texture2DDescription()
            {
                ArraySize = 1,
                Width = scaledWidth,
                Height = scaledHeight,
                Format = SharpDX.DXGI.Format.R16G16B16A16_Float,
                MipLevels = 1,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            }, true, true);

            // bloom
            brightPassTexture = new BindableTexture(Viewport.Device, new Texture2DDescription()
            {
                ArraySize = 1,
                Width = scaledWidth,
                Height = scaledHeight,
                Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                MipLevels = 1,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            }, true, true);
            blurTexture = new BindableTexture(Viewport.Device, new Texture2DDescription()
            {
                ArraySize = 1,
                Width = scaledWidth,
                Height = scaledHeight,
                Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                MipLevels = 1,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default

            }, true, true);

            scaledWidth = (Viewport.ViewportWidth - (Viewport.ViewportWidth % 8)) / 8;
            if (scaledWidth < 1)
                scaledWidth = 1;
            scaledHeight = (Viewport.ViewportHeight - (Viewport.ViewportHeight % 8)) / 8;
            if (scaledHeight < 1)
                scaledHeight = 1;

            bloomSourceTexture = new BindableTexture(Viewport.Device, new Texture2DDescription()
            {
                ArraySize = 1,
                Width = scaledWidth,
                Height = scaledHeight,
                Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                MipLevels = 1,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            }, true, true);

            for (int i = 0; i < 3; i++)
            {
                bloomTextures[i] = new BindableTexture(Viewport.Device, new Texture2DDescription()
                {
                    ArraySize = 1,
                    Width = scaledWidth,
                    Height = scaledHeight,
                    Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                    MipLevels = 1,
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                    Usage = ResourceUsage.Default
                }, true, true);
            }

            postProcessTexture = new BindableTexture(Viewport.Device, new Texture2DDescription()
            {
                ArraySize = 1,
                Format = SharpDX.DXGI.Format.R16G16B16A16_Float,
                Width = Viewport.ViewportWidth,
                Height = Viewport.ViewportHeight,
                MipLevels = 1,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            }, true, true);

            // txaa
            txaaMotionVectorsTexture = new BindableTexture(Viewport.Device, new Texture2DDescription()
            {
                ArraySize = 1,
                Format = SharpDX.DXGI.Format.R16G16_Float,
                Width = Viewport.ViewportWidth,
                Height = Viewport.ViewportHeight,
                MipLevels = 1,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            }, true, true);
            txaaFeedbackTeture = new BindableTexture(Viewport.Device, new Texture2DDescription()
            {
                ArraySize = 1,
                Format = SharpDX.DXGI.Format.R16G16B16A16_Float,
                Width = Viewport.ViewportWidth,
                Height = Viewport.ViewportHeight,
                MipLevels = 1,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            }, true, false);

            // editor composite
            editorCompositeTexture = new BindableTexture(Viewport.Device, new Texture2DDescription()
            {
                ArraySize = 1,
                Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                Width = Viewport.ViewportWidth,
                Height = Viewport.ViewportHeight,
                MipLevels = 1,
                SampleDescription = new SharpDX.DXGI.SampleDescription(4, 0),
                Usage = ResourceUsage.Default
            }, true, true);

            // editor MSAA depth buffer
            editorCompositeDepthTexture = new BindableDepthTexture(Viewport.Device, new Texture2DDescription()
            {
                ArraySize = 1,
                Format = SharpDX.DXGI.Format.R24G8_Typeless,
                Width = Viewport.ViewportWidth,
                Height = Viewport.ViewportHeight,
                MipLevels = 1,
                SampleDescription = new SharpDX.DXGI.SampleDescription(4, 0),
                Usage = ResourceUsage.Default
            }, true,
            new DepthStencilViewDescription()
            {
                Dimension = DepthStencilViewDimension.Texture2DMultisampled,
                Format = SharpDX.DXGI.Format.D24_UNorm_S8_UInt,
                Texture2DMS = new DepthStencilViewDescription.Texture2DMultisampledResource()
            },
            new ShaderResourceViewDescription()
            {
                Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Texture2DMultisampled,
                Format = SharpDX.DXGI.Format.R24_UNorm_X8_Typeless,
                Texture2DMS = new ShaderResourceViewDescription.Texture2DMultisampledResource()
            });

            // for drawing selection outlines
            selectionOutlineTexture = new BindableTexture(Viewport.Device, new Texture2DDescription()
            {
                ArraySize = 1,
                Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                Width = Viewport.ViewportWidth,
                Height = Viewport.ViewportHeight,
                MipLevels = 1,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            }, true, true);
            selectionDepthTexture = new BindableDepthTexture(Viewport.Device, new Texture2DDescription()
            {
                ArraySize = 1,
                Format = SharpDX.DXGI.Format.R24G8_Typeless,
                Width = Viewport.ViewportWidth,
                Height = Viewport.ViewportHeight,
                MipLevels = 1,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            }, true,
            new DepthStencilViewDescription()
            {
                Dimension = DepthStencilViewDimension.Texture2D,
                Format = SharpDX.DXGI.Format.D24_UNorm_S8_UInt,
                Texture2D = new DepthStencilViewDescription.Texture2DResource()
                {
                    MipSlice = 0
                }
            },
            new ShaderResourceViewDescription()
            {
                Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Texture2DMultisampled,
                Format = SharpDX.DXGI.Format.R24_UNorm_X8_Typeless,
                Texture2D = new ShaderResourceViewDescription.Texture2DResource()
                {
                    MipLevels = 1,
                    MostDetailedMip = 0
                }
            });

            // world normals for HBAO
            worldNormalsForHBAOTexture = new BindableTexture(Viewport.Device, new Texture2DDescription()
            {
                ArraySize = 1,
                Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                Width = Viewport.ViewportWidth,
                Height = Viewport.ViewportHeight,
                MipLevels = 1,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            }, true, true);

            if (ShadowsEnabled)
            {
                // resize shadow screen dependent buffers
                GFSDK_ShadowLib.InitSizeDependent(shadowContext, Viewport.ViewportWidth, Viewport.ViewportHeight, ref shadowMapHandle, ref shadowBufferHandle);
            }

            // update camera (for when returning from other tabs)
            camera?.SetProjParams(90.0f * ((float)Math.PI / 360.0f), Viewport.ViewportWidth / (float)Viewport.ViewportHeight, NearPlane, FarPlane);

            // clear the average luminance on return (this ensures that the luminance is not
            // skewed to a really dark value when switching between tabs)
            toneMapTextures[5]?.Clear(Viewport.Context, new Color4(0.00177f, 0, 0, 0));
        }

        /// <summary>
        /// Creates all other buffers
        /// </summary>
        public override void CreateBuffers()
        {
            ShadowsEnabled = Config.Get<bool>("RenderShadowsEnabled", true);
            HBAOEnabled = !Config.Get<bool>("RenderShadersEnabled", true) && Config.Get<bool>("RenderHBAOEnabled", true);
            TXAAEnabled = Config.Get<bool>("RenderTXAAEnabled", true);
            //ShadowsEnabled = Config.Get<bool>("Render", "ShadowsEnabled", true);
            //HBAOEnabled = Config.Get<bool>("Render", "HBAOEnabled", true);
            //TXAAEnabled = Config.Get<bool>("Render", "TXAAEnabled", true);


            // initialize the libraries
            textureLibrary = new TextureLibrary(Viewport.Device);
            shaderLibrary = new ShaderLibrary(Shader.CreateFallback(Viewport.Device));

            // constant buffers
            viewConstants = new ConstantBuffer<ViewConstants>(Viewport.Device, new ViewConstants());
            functionConstants = new ConstantBuffer<FunctionConstants>(Viewport.Device, new FunctionConstants());
            commonConstants = new ConstantBuffer<CommonConstants>(Viewport.Device, new CommonConstants());
            lightConstants = new ConstantBuffer<LightConstants>(Viewport.Device, new LightConstants());
            cubeMapConstants = new ConstantBuffer<CubeMapConstants>(Viewport.Device, new CubeMapConstants());
            lookupTableConstants = new ConstantBuffer<TableLookupConstants>(Viewport.Device, new TableLookupConstants());
            globalConstants = new ConstantBuffer<GlobalConstants>(Viewport.Device, new GlobalConstants());
            postProcessConstants = new SharpDX.Direct3D11.Buffer(Viewport.Device, new BufferDescription()
            {
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = 32 * 4 * 4,
                StructureByteStride = 0,
                Usage = ResourceUsage.Dynamic
            });

            // shaders
            psSunLight = FrostyShaderDb.GetShader<PixelShader>(Viewport.Device, "SunLight");
            psPointLight = FrostyShaderDb.GetShader<PixelShader>(Viewport.Device, "PointLight");
            psSphereLight = FrostyShaderDb.GetShader<PixelShader>(Viewport.Device, "SphereLight");

            vsFullscreenQuad = FrostyShaderDb.GetShader<VertexShader>(Viewport.Device, "FullscreenQuad");
            psResolve = FrostyShaderDb.GetShader<PixelShader>(Viewport.Device, "Resolve");
            psResolveDepthToMsaa = FrostyShaderDb.GetShader<PixelShader>(Viewport.Device, "ResolveDepthToMsaa");
            psResolveWorldNormals = FrostyShaderDb.GetShader<PixelShader>(Viewport.Device, "ResolveWorldNormals");

            psIntegrateDFG = FrostyShaderDb.GetShader<PixelShader>(Viewport.Device, "IBL_IntegrateDFG");
            psIntegrateDiffuseLD = FrostyShaderDb.GetShader<PixelShader>(Viewport.Device, "IBL_IntegrateDiffuseLD");
            psIntegrateSpecularLD = FrostyShaderDb.GetShader<PixelShader>(Viewport.Device, "IBL_IntegrateSpecularLD");
            psIBLRender = FrostyShaderDb.GetShader<PixelShader>(Viewport.Device, "IBL_Main");

            psDownscale4x4 = FrostyShaderDb.GetShader<PixelShader>(Viewport.Device, "DownScale4x4");
            psSampleLumInitial = FrostyShaderDb.GetShader<PixelShader>(Viewport.Device, "SampleLumInitial");
            psSampleLumIterative = FrostyShaderDb.GetShader<PixelShader>(Viewport.Device, "SampleLumIterative");
            psSampleLumFinal = FrostyShaderDb.GetShader<PixelShader>(Viewport.Device, "SampleLumFinal");
            psCalcAdaptedLum = FrostyShaderDb.GetShader<PixelShader>(Viewport.Device, "CalculateAdaptedLum");
            psLookupTable = FrostyShaderDb.GetShader<PixelShader>(Viewport.Device, "LookupTable");
            psEditorComposite = FrostyShaderDb.GetShader<PixelShader>(Viewport.Device, "EditorComposite");
            psSelectionOutline = FrostyShaderDb.GetShader<PixelShader>(Viewport.Device, "SelectionOutline");
            psDebugRenderMode = FrostyShaderDb.GetShader<PixelShader>(Viewport.Device, "DebugRenderMode");
            psBrightPass = FrostyShaderDb.GetShader<PixelShader>(Viewport.Device, "BrightPass");
            psGaussianBlur5x5 = FrostyShaderDb.GetShader<PixelShader>(Viewport.Device, "GaussianBlur5x5");
            psDownSample2x2 = FrostyShaderDb.GetShader<PixelShader>(Viewport.Device, "DownSample2x2");
            psBloomBlur = FrostyShaderDb.GetShader<PixelShader>(Viewport.Device, "BloomBlur");
            psRenderBloom = FrostyShaderDb.GetShader<PixelShader>(Viewport.Device, "RenderBloom");
            psVignette = FrostyShaderDb.GetShader<PixelShader>(Viewport.Device, "Vignette");

            // resources
            preintegratedDFGTexture = new BindableTexture(Viewport.Device, new Texture2DDescription()
            {
                ArraySize = 1,
                Format = SharpDX.DXGI.Format.R16G16B16A16_Float,
                Width = 128,
                Height = 128,
                MipLevels = 1,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            }, true, true);
            blankTexture = new BindableTexture(Viewport.Device, new Texture2DDescription()
            {
                ArraySize = 1,
                Format = SharpDX.DXGI.Format.R16G16B16A16_Float,
                Height = 1,
                Width = 1,
                MipLevels = 1,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
            }, true, true);
            whiteTexture = new BindableTexture(Viewport.Device, new Texture2DDescription()
            {
                ArraySize = 1,
                Format = SharpDX.DXGI.Format.R16G16B16A16_Float,
                Height = 1,
                Width = 1,
                MipLevels = 1,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
            }, true, true);
            preintegratedDLDTexture = new BindableCubeTexture(Viewport.Device, new Texture2DDescription()
            {
                ArraySize = 6,
                Format = SharpDX.DXGI.Format.R16G16B16A16_Float,
                Height = 32,
                Width = 32,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.TextureCube,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            }, true, true);
            preintegratedSLDTexture = new BindableCubeTexture(Viewport.Device, new Texture2DDescription()
            {
                ArraySize = 6,
                Format = SharpDX.DXGI.Format.R16G16B16A16_Float,
                Height = 256,
                Width = 256,
                MipLevels = 9,
                OptionFlags = ResourceOptionFlags.TextureCube,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            }, true, true);

            blankTexture.Clear(Viewport.Device.ImmediateContext, new Color4(0, 0, 0, 1));
            whiteTexture.Clear(Viewport.Device.ImmediateContext, new Color4(1, 1, 1, 1));

            // tonemaps
            int sampleLen = 0;
            for (int i = 0; i < 6; i++)
            {
                sampleLen = 1 << (2 * i);
                if (i >= 4)
                    sampleLen = 1;

                toneMapTextures[i] = new BindableTexture(Viewport.Device, new Texture2DDescription()
                {
                    ArraySize = 1,
                    Format = SharpDX.DXGI.Format.R32_Float,
                    Height = sampleLen,
                    MipLevels = 1,
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    Width = sampleLen
                }, true, true);
            }
            toneMapTextures[5].Clear(Viewport.Context, new Color4(0.00177f, 0, 0, 0));

            // staging texture for luminance gathering
            toneMapTextures[6] = new BindableTexture(Viewport.Device, new Texture2DDescription()
            {
                ArraySize = 1,
                Format = SharpDX.DXGI.Format.R32_Float,
                Height = sampleLen,
                MipLevels = 1,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Staging,
                Width = sampleLen,
                CpuAccessFlags = CpuAccessFlags.Read,
            }, false, false);

            if (TXAAEnabled)
            {
                // initialise TXAA
                GFSDK_TXAA.Init(Viewport.Device, ref txaaContext, ref txaaMotionVectorGenerator);
            }

            if (ShadowsEnabled)
            {
                // initialize ShadowLib
                GFSDK_ShadowLib.Init(Viewport.Device, Viewport.Context, Viewport.ViewportWidth, Viewport.ViewportHeight, ref shadowContext, ref shadowMapHandle, ref shadowBufferHandle);
            }

            if (HBAOEnabled)
            {
                // initialize HBAO
                GFSDK_SSAO.Init(Viewport.Device, ref hbaoContext);
            }

            normalBasisCubemapTexture = new BindableTexture(Viewport.Device, new Texture2DDescription()
            {
                ArraySize = 6,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                Height = 1,
                MipLevels = 1,
                Width = 1,
                OptionFlags = ResourceOptionFlags.TextureCube,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            }, true, false);

            uint[] values = new uint[]
            {
                0x00000000,
                0x01010101,
                0x02020202,
                0x03030303,
                0x04040404,
                0x05050505
            };
            GCHandle handle = GCHandle.Alloc(values, GCHandleType.Pinned);

            for (int i = 0; i < 6; i++)
            {
                int subResourceId = normalBasisCubemapTexture.Texture.CalculateSubResourceIndex(0, i, out int rowPitch);

                IntPtr bufferPtr = handle.AddrOfPinnedObject();
                bufferPtr += (i * 4);

                DataBox box = new DataBox(bufferPtr, rowPitch, 0);
                Viewport.Device.ImmediateContext.UpdateSubresource(box, normalBasisCubemapTexture.Texture, subResourceId);
            }

            handle.Free();

            skySphere = MeshRenderShape.CreateSphere(RenderCreateState2, "SkySphere", "Skybox", true, 200000.0f, 32);
            groundBox = MeshRenderShape.CreateCube(RenderCreateState2, "GroundBox", "GroundPlane", false, 1, 1, 1);
            gridPlane = MeshRenderShape.CreatePlane(RenderCreateState2, "Grid", "Grid", false, 1, 1);

            // load in a default light probe cubemap
            defaultDistantLightProbe = Skybox ?? textureLibrary.LoadTextureAsset("Resources/Textures/DefaultLightProbe.dds", true);
            DistantLightProbe = defaultDistantLightProbe;

            camera?.SetProjParams(90.0f * ((float)Math.PI / 360.0f), Viewport.ViewportWidth / (float)Viewport.ViewportHeight, NearPlane, FarPlane);
        }
        #endregion

        #region -- Update/Render --
        /// <summary>
        /// Called once a frame to perform any update steps like animation, etc.
        /// </summary>
        public override void Update(double timestep)
        {
            GFSDK_TXAA.Update(Viewport.ViewportWidth, Viewport.ViewportHeight);
            camera.FrameMove((float)(timestep));
            totalTime += timestep * TimeScale;
            lastDeltaTime = timestep;
        }

        /// <summary>
        /// Performs the actual render to screen
        /// </summary>
        public override void Render()
        {
            GFSDK_TXAA.TxaaEnabled = RenderMode == DebugRenderMode.Default && TXAAEnabled;

            BeginFrameActions();
            {
                // collect the meshes and lights to be rendered this frame
                meshes = CollectMeshInstances();
                lights = CollectLightInstances();

                // add in sky sphere and ground plane
                //meshes.Add(new MeshRenderInstance() { RenderMesh = skySphere, Transform = Matrix.Identity });
                if (GroundVisible)
                    meshes.Insert(0, new MeshRenderInstance() { RenderMesh = groundBox, Transform = Matrix.Scaling(8, 0.25f, 8) * Matrix.Translation(0, -0.125f, 0) });

                // add grid to editor meshes
                editorMeshes = new List<MeshRenderInstance>();
                if (GridVisible)
                    editorMeshes.Add(new MeshRenderInstance() { RenderMesh = gridPlane, Transform = Matrix.Translation(0, (GroundVisible) ? -0.125f : 0.0f, 0) });

                {
                    GFSDK_TXAA.GetJitter(out float[] jitter);

                    // update the view constants
                    UpdateViewConstants(true);

                    // update the common constants
                    Matrix invProjMatrix = camera.GetProjMatrix();
                    Matrix invViewProjMatrix = camera.GetViewProjMatrix();

                    invProjMatrix.Invert();
                    invProjMatrix.Transpose();
                    invViewProjMatrix.Invert();
                    invViewProjMatrix.Transpose();

                    Matrix4x3[] normalBasisTransforms = new Matrix4x3[6]
                    {
                        new Matrix4x3(new float[] { 0, 0, -1, 0, 0, -1, 0, 0, -1, 0, 0, 0 }),
                        new Matrix4x3(new float[] { 0, 0, 1, 0, 0, -1, 0, 0, 1, 0, 0, 0 }),
                        new Matrix4x3(new float[] { -1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0 }),
                        new Matrix4x3(new float[] { -1, 0, 0, 0, 0, 0, -1, 0, 0, -1, 0, 0 }),
                        new Matrix4x3(new float[] { -1, 0, 0, 0, 0, -1, 0, 0, 0, 0, 1, 0 }),
                        new Matrix4x3(new float[] { 1, 0, 0, 0, 0, -1, 0, 0, 0, 0, -1, 0 })
                    };

                    commonConstants.UpdateData(Viewport.Context, new CommonConstants()
                    {
                        InvViewProjMatrix = invViewProjMatrix,
                        InvProjMatrix = invProjMatrix,
                        CameraPos = new Vector4(camera.GetEyePt() * new Vector3(-1, 1, 1), (float)RenderMode),
                        InvScreenSize = new Vector4(1.0f / Viewport.ViewportWidth, 1.0f / Viewport.ViewportHeight, Viewport.ViewportWidth, Viewport.ViewportHeight),
                        ExposureMultipliers = new Vector4(CommonConstants.ComputeExposure(luminanceHistogram.GetAverage(), MinEV100, MaxEV100), MinEV100, MaxEV100),

                        NormalBasisTransforms1 = normalBasisTransforms[0],
                        NormalBasisTransforms2 = normalBasisTransforms[1],
                        NormalBasisTransforms3 = normalBasisTransforms[2],
                        NormalBasisTransforms4 = normalBasisTransforms[3],
                        NormalBasisTransforms5 = normalBasisTransforms[4],
                        NormalBasisTransforms6 = normalBasisTransforms[5],

                        LightProbeIntensity = new Vector4(LightProbeIntensity, 0, 0, 0)
                    });
                }

                ClearRenderTargets();
                if (bRecalculateLightProbe)
                {
                    PreintegrateIBL();
                    CalculateSphericalHarmonics();
                    bRecalculateLightProbe = false;
                }
                RenderBasePass();
                RenderShadows();
                RenderLights();
                RenderIBL();
                ResolveNormalsForHBAO();
                RenderEmissive();
                RenderForward();
                PostProcess();
                Resolve();
            }
            EndFrameActions();
        }

        public FrameData GetFrameData()
        {
            Matrix4x3[] normalBasisTransforms = new Matrix4x3[6]
            {
                new Matrix4x3(new float[] { 0, 0, 1, 0, 0, -1, 0, 0, -1, 0, 0, 0 }),
                new Matrix4x3(new float[] { 0, 0, 1, 0, 0, -1, 0, 0, 1, 0, 0, 0 }),
                new Matrix4x3(new float[] { 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0 }),
                new Matrix4x3(new float[] { 1, 0, 0, 0, 0, 0, 1, 0, 0, -1, 0, 0 }),
                new Matrix4x3(new float[] { 1, 0, 0, 0, 0, -1, 0, 0, 0, 0, 1, 0 }),
                new Matrix4x3(new float[] { -1, 0, 0, 0, 0, -1, 0, 0, 0, 0, 1, 0 })
            };

            Matrix viewMatrix = camera.GetViewMatrix();
            viewMatrix.Transpose();
            Matrix projMatrix = camera.GetProjMatrix();
            projMatrix.Transpose();
            Matrix viewProjMatrix = camera.GetViewProjMatrix();
            viewProjMatrix.Transpose();
            Matrix crViewProjMatrix = camera.GetCrViewProjMatrix();
            crViewProjMatrix.Transpose();

            float time = (float)totalTime;

            Vector4 exposure = CommonConstants.ComputeExposureVec4(luminanceHistogram.GetAverage(), MinEV100, MaxEV100);
            exposure.X *= 512;
            exposure.Y /= 512;
            exposure.Z *= 512;
            exposure.W /= 512;

            return new FrameData
            {
                Time = new Vector4(time, time, time, time),
                ScreenSize = new Vector4(Viewport.ViewportWidth, Viewport.ViewportHeight, 1.0f / Viewport.ViewportWidth, 1.0f / Viewport.ViewportHeight),
                ViewMatrix = viewMatrix,
                ProjMatrix = projMatrix,
                ViewProjMatrix = viewProjMatrix,
                CrViewProjMatrix = crViewProjMatrix,
                NormalBasisTransforms = normalBasisTransforms,
                ProjectionKxKyKzKw = new Vector4(1.0f / projMatrix.M11, 1.0f / projMatrix.M22, projMatrix.M43, projMatrix.M33),
                ExposureMultipliers = exposure,
                CameraPos = camera.GetEyePt()
            };
        }

        public virtual List<MeshRenderInstance> CollectMeshInstances()
        {
            return new List<MeshRenderInstance>();
        }

        public virtual List<LightRenderInstance> CollectLightInstances()
        {
            return new List<LightRenderInstance>();
        }

        protected virtual void UpdateViewConstants(bool bJitter)
        {
            Matrix4x3[] normalBasisTransforms = new Matrix4x3[6]
            {
                new Matrix4x3(new float[] { 0, 0, 1, 0, 0, -1, 0, 0, -1, 0, 0, 0 }),
                new Matrix4x3(new float[] { 0, 0, 1, 0, 0, -1, 0, 0, 1, 0, 0, 0 }),
                new Matrix4x3(new float[] { 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0 }),
                new Matrix4x3(new float[] { 1, 0, 0, 0, 0, 0, 1, 0, 0, -1, 0, 0 }),
                new Matrix4x3(new float[] { 1, 0, 0, 0, 0, -1, 0, 0, 0, 0, 1, 0 }),
                new Matrix4x3(new float[] { -1, 0, 0, 0, 0, -1, 0, 0, 0, 0, 1, 0 })
            };

            Matrix viewMatrix = camera.GetViewMatrix();
            viewMatrix.Transpose();
            Matrix projMatrix = camera.GetProjMatrix();
            projMatrix.Transpose();
            Matrix viewProjMatrix = camera.GetViewProjMatrix();
            viewProjMatrix.Transpose();
            Matrix crViewProjMatrix = camera.GetCrViewProjMatrix();
            if (bJitter)
            {
                GFSDK_TXAA.GetJitter(out float[] jitter);

                crViewProjMatrix = camera.GetCrViewProjMatrix(jitter);
            }
            crViewProjMatrix.Transpose();

            viewConstants.UpdateData(Viewport.Context, new ViewConstants()
            {
                Time = new Vector4((float)totalTime, 0, 0, 0),
                ScreenSize = new Vector4(Viewport.ViewportWidth, Viewport.ViewportHeight, 1.0f / Viewport.ViewportWidth, 1.0f / Viewport.ViewportHeight),

                ViewMatrix = viewMatrix,
                ProjMatrix = projMatrix,
                ViewProjMatrix = viewProjMatrix,
                CrViewProjMatrix = crViewProjMatrix,
                NormalBasisTransforms1 = normalBasisTransforms[0],
                NormalBasisTransforms2 = normalBasisTransforms[1],
                NormalBasisTransforms3 = normalBasisTransforms[2],
                NormalBasisTransforms4 = normalBasisTransforms[3],
                NormalBasisTransforms5 = normalBasisTransforms[4],
                NormalBasisTransforms6 = normalBasisTransforms[5],
                ExposureMultipliers = new Vector4(CommonConstants.ComputeExposure(luminanceHistogram.GetAverage(), MinEV100, MaxEV100), MinEV100, MaxEV100),
                CameraPos = new Vector4(camera.GetEyePt(), 1.0f),
            });
        }
        #endregion

        #region -- Input --
        public override void MouseMove(int x, int y)
        {
            camera?.MouseMove(x, y);
        }

        public override void MouseDown(int x, int y, Frosty.Core.Viewport.MouseButton button)
        {
            camera?.MouseButtonDown(x, y, button);
        }

        public override void MouseUp(int x, int y, Frosty.Core.Viewport.MouseButton button)
        {
            camera?.MouseButtonUp(button);
        }

        public override void MouseScroll(int delta)
        {
            camera?.MouseWheel(delta);
        }

        public override void KeyDown(int key)
        {
            camera?.KeyDown((Key)key);
        }

        public override void KeyUp(int key)
        {
            camera?.KeyUp((Key)key);

#if FROSTY_DEVELOPER
            if ((Key)key == Key.F12)
            {
                CaptureNextFrame();
            }
#endif
        }
        #endregion

        #region -- Dispose --
        /// <summary>
        /// Dispose of any buffers dependent on viewport size, this is called when the viewport
        /// changes sizes or is closed
        /// </summary>
        public override void DisposeSizeDependentBuffers()
        {
            gBufferCollection.Dispose();
            lightAccumulationTexture.Dispose();
            scaledSceneTexture.Dispose();
            postProcessTexture.Dispose();
            txaaFeedbackTeture.Dispose();
            txaaMotionVectorsTexture.Dispose();
            editorCompositeDepthTexture.Dispose();
            editorCompositeTexture.Dispose();
            finalColorTexture.Dispose();
            selectionDepthTexture.Dispose();
            worldNormalsForHBAOTexture.Dispose();
            brightPassTexture.Dispose();
            blurTexture.Dispose();
            bloomSourceTexture.Dispose();

            foreach (BindableTexture texture in bloomTextures)
                texture.Dispose();

            if (ShadowsEnabled)
            {
                shadowContext.RemoveMap(ref shadowMapHandle);
                shadowContext.RemoveBuffer(ref shadowBufferHandle);
            }
        }

        /// <summary>
        /// Dispose of all buffers not viewport dependent
        /// </summary>
        public override void DisposeBuffers()
        {
            textureLibrary.Dispose();
            shaderLibrary.Dispose();

            viewConstants.Dispose();
            functionConstants.Dispose();
            commonConstants.Dispose();
            lightConstants.Dispose();
            postProcessConstants.Dispose();
            cubeMapConstants.Dispose();
            lookupTableConstants.Dispose();
            globalConstants.Dispose();

            psPointLight.Dispose();
            psSunLight.Dispose();
            psSphereLight.Dispose();

            vsFullscreenQuad.Dispose();
            psResolve.Dispose();
            psResolveDepthToMsaa.Dispose();
            psResolveWorldNormals.Dispose();

            psIntegrateDFG.Dispose();
            psIntegrateDiffuseLD.Dispose();
            psIntegrateSpecularLD.Dispose();
            psIBLRender.Dispose();

            psDownscale4x4.Dispose();
            psSampleLumInitial.Dispose();
            psSampleLumIterative.Dispose();
            psSampleLumFinal.Dispose();
            psCalcAdaptedLum.Dispose();
            psLookupTable.Dispose();
            psEditorComposite.Dispose();
            psSelectionOutline.Dispose();
            psDebugRenderMode.Dispose();
            psBrightPass.Dispose();
            psGaussianBlur5x5.Dispose();
            psDownSample2x2.Dispose();
            psBloomBlur.Dispose();
            psRenderBloom.Dispose();
            psVignette.Dispose();

            normalBasisCubemapTexture.Dispose();
            preintegratedDFGTexture.Dispose();
            preintegratedDLDTexture.Dispose();
            preintegratedSLDTexture.Dispose();

            for (int i = 0; i < 7; i++)
                toneMapTextures[i].Dispose();

            if (TXAAEnabled)
                GFSDK_TXAA.Destroy(ref txaaContext, ref txaaMotionVectorGenerator);
            if (HBAOEnabled)
                hbaoContext.Release();
            if (ShadowsEnabled)
                shadowContext.Destroy();

            skySphere.Dispose();
            groundBox.Dispose();
            gridPlane.Dispose();
        }
        #endregion

        #region -- Render Stages --
        /// <summary>
        /// 
        /// </summary>
        protected virtual void CalculateSphericalHarmonics()
        {
            if (DistantLightProbe == null)
                return;

            SharpDX.Mathematics.Interop.RawViewportF[] origViewports = Viewport.Context.Rasterizer.GetViewports<SharpDX.Mathematics.Interop.RawViewportF>();

            D3DUtils.BeginPerfEvent(Viewport.Context, "Spherical Harmonics");
            {
                PixelShader ps = FrostyShaderDb.GetShader<PixelShader>(Viewport.Device, "ResolveCubeMapFace");

                Texture2DDescription desc = new Texture2DDescription()
                {
                    ArraySize = 1,
                    BindFlags = BindFlags.RenderTarget,
                    CpuAccessFlags = CpuAccessFlags.None,
                    Format = SharpDX.DXGI.Format.R16G16B16A16_Float,
                    Height = preintegratedSLDTexture.Texture.Description.Height,
                    MipLevels = 1,
                    OptionFlags = ResourceOptionFlags.None,
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    Width = preintegratedSLDTexture.Texture.Description.Width
                };

                Texture2D tmpTexture = new Texture2D(Viewport.Device, desc);
                desc.CpuAccessFlags = CpuAccessFlags.Read;
                desc.BindFlags = BindFlags.None;
                desc.Usage = ResourceUsage.Staging;
                Texture2D resolveTexture = new Texture2D(Viewport.Device, desc);
                RenderTargetView tmpRtv = new RenderTargetView(Viewport.Device, tmpTexture);

                float[] resultR = new float[9];
                float[] resultG = new float[9];
                float[] resultB = new float[9];
                float[] shBuffB = new float[9];
                float weight = 0.0f;

                for (int i = 0; i < 6; i++)
                {
                    cubeMapConstants.UpdateData(Viewport.Context, new CubeMapConstants() { CubeFace = i });

                    // render out cubemap face
                    Viewport.Context.OutputMerger.SetRenderTargets(null, tmpRtv);
                    Viewport.Context.Rasterizer.SetViewport(new SharpDX.Viewport(0, 0, desc.Width, desc.Height));
                    Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(false);
                    Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget());
                    Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.None);

                    Viewport.Context.InputAssembler.SetIndexBuffer(null, SharpDX.DXGI.Format.Unknown, 0);
                    Viewport.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
                    Viewport.Context.InputAssembler.InputLayout = null;

                    Viewport.Context.VertexShader.Set(vsFullscreenQuad);
                    Viewport.Context.PixelShader.Set(ps);
                    Viewport.Context.PixelShader.SetConstantBuffer(0, cubeMapConstants.Buffer);
                    Viewport.Context.PixelShader.SetShaderResource(0, preintegratedSLDTexture.SRV);
                    Viewport.Context.PixelShader.SetSampler(0, D3DUtils.CreateSamplerState(TextureAddressMode.Clamp, filter: Filter.MinMagMipPoint));

                    Viewport.Context.Draw(6, 0);

                    // resolve to staging
                    Viewport.Context.OutputMerger.SetRenderTargets(null, new RenderTargetView[] { });
                    Viewport.Context.CopyResource(tmpTexture, resolveTexture);

                    // read staging texture
                    Viewport.Context.MapSubresource(resolveTexture, 0, MapMode.Read, MapFlags.None, out DataStream stream);
                    {
                        float invWidth = 1.0f / preintegratedSLDTexture.Texture.Description.Width;
                        float negativeBound = -1.0f + invWidth;
                        float invWidthBy2 = 2.0f / preintegratedSLDTexture.Texture.Description.Width;

                        for (int y = 0; y < preintegratedSLDTexture.Texture.Description.Height; y++)
                        {
                            float fV = negativeBound + y * invWidthBy2;
                            for (int x = 0; x < preintegratedSLDTexture.Texture.Description.Width; x++)
                            {
                                float fU = negativeBound + x * invWidthBy2;
                                Vector3 dir = Vector3.Zero;

                                switch (i)
                                {
                                    case 0: /* X+ */
                                        dir.X = 1.0f;
                                        dir.Y = 1.0f - (invWidthBy2 * y + invWidth);
                                        dir.Z = 1.0f - (invWidthBy2 * x + invWidth);
                                        dir = -dir;
                                        break;
                                    case 1: /* X- */
                                        dir.X = -1.0f;
                                        dir.Y = 1.0f - (invWidthBy2 * y + invWidth);
                                        dir.Z = -1.0f + (invWidthBy2 * x + invWidth);
                                        dir = -dir;
                                        break;
                                    case 2: /* Y+ */
                                        dir.X = -1.0f + (invWidthBy2 * x + invWidth);
                                        dir.Y = 1.0f;
                                        dir.Z = -1.0f + (invWidthBy2 * y + invWidth);
                                        dir = -dir;
                                        break;
                                    case 3: /* Y- */
                                        dir.X = -1.0f + (invWidthBy2 * x + invWidth);
                                        dir.Y = -1.0f;
                                        dir.Z = 1.0f - (invWidthBy2 * y + invWidth);
                                        dir = -dir;
                                        break;
                                    case 4: /* Z+ */
                                        dir.X = -1.0f + (invWidthBy2 * x + invWidth);
                                        dir.Y = 1.0f - (invWidthBy2 * y + invWidth);
                                        dir.Z = 1.0f;
                                        break;
                                    case 5: /* Z- */
                                        dir.X = 1.0f - (invWidthBy2 * x + invWidth);
                                        dir.Y = 1.0f - (invWidthBy2 * y + invWidth);
                                        dir.Z = -1.0f;
                                        break;
                                }

                                dir.Normalize();
                                float diffSolid = 4.0f / ((1.0f + fU * fU + fV * fV) * (float)Math.Sqrt(1.0f + fU * fU + fV * fV));
                                float[] sh = SphericalHarmonicsHelper.shEvaluateDir(dir);

                                weight += diffSolid;

                                float R = HalfUtils.Unpack(stream.Read<ushort>());
                                float G = HalfUtils.Unpack(stream.Read<ushort>());
                                float B = HalfUtils.Unpack(stream.Read<ushort>());
                                float A = HalfUtils.Unpack(stream.Read<ushort>());

                                Vector3 color = new Vector3(R, G, B);
                                if (color.X > 3.0f || color.Y > 3.0f || color.Z > 3.0f)
                                    continue;

                                shBuffB = SphericalHarmonicsHelper.shScale(sh, color.X * diffSolid);
                                resultR = SphericalHarmonicsHelper.shAdd(resultR, shBuffB);
                                shBuffB = SphericalHarmonicsHelper.shScale(sh, color.Y * diffSolid);
                                resultG = SphericalHarmonicsHelper.shAdd(resultG, shBuffB);
                                shBuffB = SphericalHarmonicsHelper.shScale(sh, color.Z * diffSolid);
                                resultB = SphericalHarmonicsHelper.shAdd(resultB, shBuffB);
                            }
                        }
                    }
                    Viewport.Context.UnmapSubresource(resolveTexture, 0);
                }

                float normProj = (4.0f * (float)Math.PI) / weight;
                resultR = SphericalHarmonicsHelper.shScale(resultR, normProj);
                resultG = SphericalHarmonicsHelper.shScale(resultG, normProj);
                resultB = SphericalHarmonicsHelper.shScale(resultB, normProj);

                for (int i = 0; i < 9; i++)
                    SHLightProbe[i] = new Vector4(resultR[i], resultG[i], resultB[i], 1.0f);

                ps.Dispose();
                tmpRtv.Dispose();
                tmpTexture.Dispose();
                resolveTexture.Dispose();
            }
            D3DUtils.EndPerfEvent(Viewport.Context);

            Viewport.Context.Rasterizer.SetViewports(origViewports);
        }

        /// <summary>
        /// Generates the preintegrated DFG, Diffuse LD, and Specular LD textures required for IBL
        /// </summary>
        protected virtual void PreintegrateIBL()
        {
            SharpDX.Mathematics.Interop.RawViewportF[] origViewports = Viewport.Context.Rasterizer.GetViewports<SharpDX.Mathematics.Interop.RawViewportF>();

            D3DUtils.BeginPerfEvent(Viewport.Context, "Preintegrate DFG");
            {
                preintegratedDFGTexture.Clear(Viewport.Context, new Color4(0, 0, 0, 0));

                Viewport.Context.OutputMerger.SetRenderTargets(null, preintegratedDFGTexture.RTV);
                Viewport.Context.Rasterizer.SetViewport(new SharpDX.Viewport(0, 0, preintegratedDFGTexture.Texture.Description.Width, preintegratedDFGTexture.Texture.Description.Height));
                Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(false);
                Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget());
                Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.None);

                Viewport.Context.InputAssembler.SetIndexBuffer(null, SharpDX.DXGI.Format.Unknown, 0);
                Viewport.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
                Viewport.Context.InputAssembler.InputLayout = null;

                Viewport.Context.VertexShader.Set(vsFullscreenQuad);
                Viewport.Context.PixelShader.Set(psIntegrateDFG);

                Viewport.Context.Draw(6, 0);
            }
            D3DUtils.EndPerfEvent(Viewport.Context);

            if (DistantLightProbe != null)
            {
                D3DUtils.BeginPerfEvent(Viewport.Context, "Preintegrate Diffuse LD");
                {
                    for (int i = 0; i < 6; i++)
                    {
                        cubeMapConstants.UpdateData(Viewport.Context, new CubeMapConstants() { CubeFace = i });
                        preintegratedDLDTexture.Clear(Viewport.Context, i, 0, new Color4(0, 0, 0, 0));

                        Viewport.Context.OutputMerger.SetRenderTargets(null, preintegratedDLDTexture.GetRTV(i, 0));
                        Viewport.Context.Rasterizer.SetViewport(new SharpDX.Viewport(0, 0, preintegratedDLDTexture.Texture.Description.Width, preintegratedDLDTexture.Texture.Description.Height));
                        Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(false);
                        Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget());
                        Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.None);

                        Viewport.Context.InputAssembler.SetIndexBuffer(null, SharpDX.DXGI.Format.Unknown, 0);
                        Viewport.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
                        Viewport.Context.InputAssembler.InputLayout = null;

                        Viewport.Context.VertexShader.Set(vsFullscreenQuad);
                        Viewport.Context.PixelShader.Set(psIntegrateDiffuseLD);
                        Viewport.Context.PixelShader.SetConstantBuffers(0, cubeMapConstants.Buffer);
                        Viewport.Context.PixelShader.SetShaderResources(0, DistantLightProbe);
                        Viewport.Context.PixelShader.SetSamplers(0, D3DUtils.CreateSamplerState(address: TextureAddressMode.Clamp, filter: Filter.MinMagMipPoint));

                        Viewport.Context.Draw(6, 0);
                    }
                }
                D3DUtils.EndPerfEvent(Viewport.Context);

                D3DUtils.BeginPerfEvent(Viewport.Context, "Preintegrate Specular LD");
                {
                    // generate lower level mips for specular LD
                    Viewport.Context.GenerateMips(DistantLightProbe);

                    for (int mipIdx = 0; mipIdx < 9; mipIdx++)
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            cubeMapConstants.UpdateData(Viewport.Context, new CubeMapConstants() { CubeFace = i, MipIndex = (uint)mipIdx, NumMips = 9 });
                            preintegratedSLDTexture.Clear(Viewport.Context, i, mipIdx, new Color4(0, 0, 0, 0));

                            Viewport.Context.OutputMerger.SetRenderTargets(null, preintegratedSLDTexture.GetRTV(i, mipIdx));
                            Viewport.Context.Rasterizer.SetViewport(new SharpDX.Viewport(0, 0, preintegratedSLDTexture.Texture.Description.Width >> mipIdx, preintegratedSLDTexture.Texture.Description.Height >> mipIdx));
                            Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(false);
                            Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget());
                            Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.None);

                            Viewport.Context.InputAssembler.SetIndexBuffer(null, SharpDX.DXGI.Format.Unknown, 0);
                            Viewport.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
                            Viewport.Context.InputAssembler.InputLayout = null;

                            Viewport.Context.VertexShader.Set(vsFullscreenQuad);
                            Viewport.Context.PixelShader.Set(psIntegrateSpecularLD);
                            Viewport.Context.PixelShader.SetConstantBuffers(0, cubeMapConstants.Buffer);
                            Viewport.Context.PixelShader.SetShaderResources(0, DistantLightProbe);
                            Viewport.Context.PixelShader.SetSamplers(0, D3DUtils.CreateSamplerState(address: TextureAddressMode.Clamp, filter: Filter.MinMagMipPoint));

                            Viewport.Context.Draw(6, 0);
                        }
                    }
                }
                D3DUtils.EndPerfEvent(Viewport.Context);
            }

            Viewport.Context.Rasterizer.SetViewports(origViewports);
        }

        /// <summary>
        /// Clears all render targets associated with this screen
        /// </summary>
        protected virtual void ClearRenderTargets()
        {
            D3DUtils.BeginPerfEvent(Viewport.Context, "ClearTargets");
            {
                Viewport.Context.ClearRenderTargetView(Viewport.ColorBufferRTV, Color4.Black);
                Viewport.Context.ClearDepthStencilView(Viewport.DepthBufferDSV, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

                editorCompositeDepthTexture.Clear(Viewport.Context, true, true, 1.0f, 0);
                selectionDepthTexture.Clear(Viewport.Context, true, true, 1.0f, 0);

                gBufferCollection.Clear(Viewport.Context);
                lightAccumulationTexture.Clear(Viewport.Context, Color4.Black);
                finalColorTexture.Clear(Viewport.Context, Color4.Black);
                editorCompositeTexture.Clear(Viewport.Context, Color4.Black);
                scaledSceneTexture.Clear(Viewport.Context, Color4.Black);
                worldNormalsForHBAOTexture.Clear(Viewport.Context, Color4.Black);
            }
            D3DUtils.EndPerfEvent(Viewport.Context);
        }

        /// <summary>
        /// Renders the geometry into gbuffers
        /// </summary>
        protected virtual void RenderBasePass()
        {
            D3DUtils.BeginPerfEvent(Viewport.Context, "BasePass");
            {
                Viewport.Context.OutputMerger.SetRenderTargets(Viewport.DepthBufferDSV, gBufferCollection.GBufferRTVs);
                Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(depthComparison: Comparison.LessEqual);
                Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget());
                Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.Front, depthClip: true, fillMode: (RenderMode == DebugRenderMode.Wireframe) ? FillMode.Wireframe : FillMode.Solid);

                Viewport.Context.VertexShader.SetConstantBuffer(0, viewConstants.Buffer);

                Viewport.Context.PixelShader.SetConstantBuffer(0, viewConstants.Buffer);
                Viewport.Context.PixelShader.SetShaderResource(0, normalBasisCubemapTexture.SRV);
                Viewport.Context.PixelShader.SetSampler(0, D3DUtils.CreateSamplerState(TextureAddressMode.Clamp, filter: Filter.MinMagMipPoint));

                RenderMeshes(MeshRenderPath.Deferred, meshes);
            }
            D3DUtils.EndPerfEvent(Viewport.Context);
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void RenderShadows()
        {
            if (!ShadowsEnabled)
                return;

            D3DUtils.BeginPerfEvent(Viewport.Context, "Shadows");
            {
                BoundingBox aabb = CalcWorldBoundingBox();

                // account for the floor mesh
                aabb = BoundingBox.Merge(aabb, new BoundingBox(new Vector3(-4, -1, -4), new Vector3(4, 1, 4)));

                // shadow pass
                GFSDK_ShadowLib.MapRenderParams renderParams = new GFSDK_ShadowLib.MapRenderParams(true)
                {
                    LightDesc =
                    {
                        eLightType = GFSDK_ShadowLib.LightType.Directional,
                        fLightSize = 1.0f,
                        v3LightPos_1 = SunPosition,
                        v3LightPos_2 = SunPosition,
                        v3LightPos_3 = SunPosition,
                        v3LightPos_4 = SunPosition,
                        v3LightLookAt_1 = Vector3.Zero,
                        v3LightLookAt_2 = Vector3.Zero,
                        v3LightLookAt_3 = Vector3.Zero,
                        v3LightLookAt_4 = Vector3.Zero
                    },

                    m4x4EyeViewMatrix = GFSDK_ShadowLib.Matrix.FromSharpDX(camera.GetViewMatrix()),
                    m4x4EyeProjectionMatrix = GFSDK_ShadowLib.Matrix.FromSharpDX(camera.GetProjMatrix()),
                    v3WorldSpaceBBox_1 = aabb.Minimum * 1.05f,
                    v3WorldSpaceBBox_2 = aabb.Maximum * 1.05f,
                    eCullModeType = GFSDK_ShadowLib.CullModeType.Front,
                    eTechniqueType = GFSDK_ShadowLib.TechniqueType.PCF,
                    eCascadedShadowMapType = GFSDK_ShadowLib.CascadedShadowMapType.SampleDistribution,
                    fCascadeMaxDistancePercent = 50.0f,
                    fCascadeZLinearScale_1 = 0.00001f,
                    fCascadeZLinearScale_2 = 0.00002f,
                    fCascadeZLinearScale_3 = 0.00005f,
                    fCascadeZLinearScale_4 = 1.0f,

                    ZBiasParams =
                    {
                        iDepthBias = iDepthBias,
                        fSlopeScaledDepthBias = fSlopeScaledDepthBias,
                        bUseReceiverPlaneBias = 0,
                        fDistanceBiasMin = fDistanceBiasMin,
                        fDistanceBiasFactor = fDistanceBiasFactor,
                        fDistanceBiasThreshold = fDistanceBiasThreshold,
                        fDistanceBiasPower = fDistanceBiasPower
                    },

                    PCSSPenumbraParams =
                    {
                        fMaxThreshold = 247.0f,
                        fMinSizePercent_1 = 1.8f,
                        fMinSizePercent_2 = 1.8f,
                        fMinSizePercent_3 = 1.8f,
                        fMinSizePercent_4 = 1.8f,
                        fMinWeightThresholdPercent = 3.0f
                    },

                    FrustumTraceMapRenderParams =
                    {
                        eConservativeRasterType = GFSDK_ShadowLib.ConservativeRasterType.HW,
                        eCullModeType = GFSDK_ShadowLib.CullModeType.None,
                        fHitEpsilon = 0.009f
                    },

                    RayTraceMapRenderParams =
                    {
                        fHitEpsilon = 0.02f,
                        eCullModeType = GFSDK_ShadowLib.CullModeType.None,
                        eConservativeRasterType = GFSDK_ShadowLib.ConservativeRasterType.HW
                    },

                    DepthBufferDesc =
                    {
                        eDepthType = GFSDK_ShadowLib.DepthType.DepthBuffer,
                        DepthSRV = Viewport.DepthBufferSRV.NativePointer
                    }
                };

                //renderParams.DepthBufferDesc.ReadOnlyDSV = todo

                int retVal = shadowContext.SetMapRenderParams(shadowMapHandle, renderParams);
                retVal = shadowContext.UpdateMapBounds(shadowMapHandle, out GFSDK_ShadowLib.Matrix[] lightViewMatrices, out GFSDK_ShadowLib.Matrix[] lightProjMatrices, out GFSDK_ShadowLib.Frustum[] renderFrustums);

                shadowContext.InitializeMapRendering(shadowMapHandle, GFSDK_ShadowLib.MapRenderType.Depth);

                for (uint uView = 0; uView < GFSDK_ShadowLib.NumCSMLevels; uView++)
                {
                    Matrix viewMatrix = lightViewMatrices[uView].ToSharpDX();
                    Matrix projMatrix = lightProjMatrices[uView].ToSharpDX();
                    Matrix viewProjMatrix = viewMatrix * projMatrix;
                    viewProjMatrix.Transpose();

                    viewConstants.UpdateData(Viewport.Context, new ViewConstants()
                    {
                        CrViewProjMatrix = viewProjMatrix,
                    });

                    shadowContext.BeginMapRendering(shadowMapHandle, GFSDK_ShadowLib.MapRenderType.Depth, uView);
                    RenderMeshes(MeshRenderPath.Shadows, meshes);
                    shadowContext.EndMapRendering(shadowMapHandle, GFSDK_ShadowLib.MapRenderType.Depth, uView);
                }
                shadowContext.ClearBuffer(shadowBufferHandle);
                shadowContext.RenderBuffer(shadowMapHandle, shadowBufferHandle, new GFSDK_ShadowLib.BufferRenderParams());
                retVal = shadowContext.FinalizeBuffer(shadowBufferHandle, ref shadowSRV);
            }
            D3DUtils.EndPerfEvent(Viewport.Context);
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void RenderMeshes(MeshRenderPath renderPath, List<MeshRenderInstance> meshList)
        {
            D3DUtils.BeginPerfEvent(Viewport.Context, "RenderMeshes");
            {
                RasterizerStateDescription desc = Viewport.Context.Rasterizer.State.Description;
                foreach (MeshRenderInstance mesh in meshList)
                {
                    D3DUtils.BeginPerfEvent(Viewport.Context, mesh.RenderMesh.DebugName);
                    {
                        Matrix transform = mesh.Transform;
                        transform.Transpose();

                        functionConstants.UpdateData(Viewport.Context, new FunctionConstants()
                        {
                            WorldMatrix = Matrix.Scaling(-1, 1, 1) * transform,
                            LightProbe1 = SHLightProbe[0],
                            LightProbe2 = SHLightProbe[1],
                            LightProbe3 = SHLightProbe[2],
                            LightProbe4 = SHLightProbe[3],
                            LightProbe5 = SHLightProbe[4],
                            LightProbe6 = SHLightProbe[5],
                            LightProbe7 = SHLightProbe[6],
                            LightProbe8 = SHLightProbe[7],
                            LightProbe9 = SHLightProbe[8],
                        });

                        Viewport.Context.VertexShader.SetConstantBuffer(1, functionConstants.Buffer);
                        Viewport.Context.PixelShader.SetConstantBuffer(1, functionConstants.Buffer);

                        mesh.RenderMesh.Render(Viewport.Context, renderPath, transform);

                        //if (renderPath == MeshRenderPath.Shadows)
                        //{
                        //    foreach (MeshRenderSection section in mesh.Lod.Sections)
                        //        shadowContext.IncrementMapPrimitiveCounter(shadowMapHandle, GFSDK_ShadowLib.MapRenderType.Depth, (uint)section.PrimitiveCount);
                        //}
                    }
                    D3DUtils.EndPerfEvent(Viewport.Context);
                }
            }
            D3DUtils.EndPerfEvent(Viewport.Context);
        }

        /// <summary>
        /// 
        /// </summary>
        private void RenderLights()
        {
            D3DUtils.BeginPerfEvent(Viewport.Context, "Lights");
            {
                RenderTargetBlendDescription rtDesc = new RenderTargetBlendDescription()
                {
                    IsBlendEnabled = true,
                    SourceBlend = BlendOption.One,
                    DestinationBlend = BlendOption.One,
                    BlendOperation = BlendOperation.Add,
                    SourceAlphaBlend = BlendOption.One,
                    DestinationAlphaBlend = BlendOption.One,
                    AlphaBlendOperation = BlendOperation.Add,
                    RenderTargetWriteMask = ColorWriteMaskFlags.All
                };

                Viewport.Context.OutputMerger.SetRenderTargets(null, lightAccumulationTexture.RTV);
                Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(false);
                Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(rtDesc);
                Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.None);

                Viewport.Context.InputAssembler.SetIndexBuffer(null, SharpDX.DXGI.Format.Unknown, 0);
                Viewport.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
                Viewport.Context.InputAssembler.InputLayout = null;

                Viewport.Context.VertexShader.Set(vsFullscreenQuad);
                Viewport.Context.VertexShader.SetConstantBuffer(0, commonConstants.Buffer);

                Viewport.Context.PixelShader.SetConstantBuffers(0, commonConstants.Buffer, lightConstants.Buffer);
                Viewport.Context.PixelShader.SetShaderResources(0, gBufferCollection.GBufferSRVs);
                Viewport.Context.PixelShader.SetShaderResources(4, Viewport.DepthBufferSRV);

                if (SunIntensity > 0)
                {
                    lightConstants.UpdateData(Viewport.Context, new LightConstants()
                    {
                        LightColorAndIntensity = new Vector4(SunColor.X, SunColor.Y, SunColor.Z, SunIntensity),
                        LightPosAndInvSqrRadius = new Vector4(SunPosition * new Vector3(-1, 1, 1), SunAngularRadius)
                    });

                    // directional sunlight first
                    Viewport.Context.PixelShader.SetShaderResources(5, shadowSRV);
                    Viewport.Context.PixelShader.Set(psSunLight);
                    Viewport.Context.Draw(6, 0);
                }

                // then all other lights
                foreach (LightRenderInstance light in lights)
                {
                    if (light.Intensity > 0)
                    {
                        lightConstants.UpdateData(Viewport.Context, new LightConstants()
                        {
                            LightColorAndIntensity = new Vector4(light.Color, light.Intensity),
                            LightPosAndInvSqrRadius = new Vector4(light.Transform.TranslationVector * new Vector3(-1, 1, 1), (light.SphereRadius > 0) ? light.SphereRadius : (1.0f / (float)(light.AttenuationRadius * light.AttenuationRadius)))
                        });

                        Viewport.Context.PixelShader.Set((light.SphereRadius > 0) ? psSphereLight : psPointLight);
                        Viewport.Context.Draw(6, 0);
                    }
                }
            }
            D3DUtils.EndPerfEvent(Viewport.Context);
        }

        /// <summary>
        /// 
        /// </summary>
        private void RenderIBL()
        {
            if (DistantLightProbe == null)
                return;

            D3DUtils.BeginPerfEvent(Viewport.Context, "IBL");
            {
                Viewport.Context.OutputMerger.SetRenderTargets(null, lightAccumulationTexture.RTV);
                Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(false);
                Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(new RenderTargetBlendDescription() { IsBlendEnabled = true, SourceBlend = BlendOption.One, DestinationBlend = BlendOption.One, BlendOperation = BlendOperation.Add, SourceAlphaBlend = BlendOption.One, DestinationAlphaBlend = BlendOption.One, AlphaBlendOperation = BlendOperation.Add, RenderTargetWriteMask = ColorWriteMaskFlags.All });
                Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.None);

                Viewport.Context.InputAssembler.SetIndexBuffer(null, SharpDX.DXGI.Format.Unknown, 0);
                Viewport.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
                Viewport.Context.InputAssembler.InputLayout = null;

                Viewport.Context.VertexShader.Set(vsFullscreenQuad);
                Viewport.Context.VertexShader.SetConstantBuffer(0, commonConstants.Buffer);

                Viewport.Context.PixelShader.Set(psIBLRender);
                Viewport.Context.PixelShader.SetConstantBuffers(0, commonConstants.Buffer);
                Viewport.Context.PixelShader.SetShaderResources(0, gBufferCollection.GBufferSRVs);
                Viewport.Context.PixelShader.SetShaderResources(4, Viewport.DepthBufferSRV, preintegratedDFGTexture.SRV, preintegratedDLDTexture.SRV, preintegratedSLDTexture.SRV, Skybox ?? DistantLightProbe);
                Viewport.Context.PixelShader.SetSampler(0, D3DUtils.CreateSamplerState(address: TextureAddressMode.Clamp, filter: Filter.MinMagMipLinear));
                Viewport.Context.PixelShader.SetSampler(1, D3DUtils.CreateSamplerState(address: TextureAddressMode.Wrap, filter: Filter.MinMagMipLinear));

                Viewport.Context.Draw(6, 0);
            }
            D3DUtils.EndPerfEvent(Viewport.Context);
        }

        /// <summary>
        /// 
        /// </summary>
        private void ResolveNormalsForHBAO()
        {
            D3DUtils.BeginPerfEvent(Viewport.Context, "ResolveNormalsForHBAO");
            {
                Viewport.Context.OutputMerger.SetRenderTargets(null, worldNormalsForHBAOTexture.RTV);
                Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(false);
                Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget());
                Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.None);

                Viewport.Context.InputAssembler.SetIndexBuffer(null, SharpDX.DXGI.Format.Unknown, 0);
                Viewport.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
                Viewport.Context.InputAssembler.InputLayout = null;

                Viewport.Context.VertexShader.Set(vsFullscreenQuad);
                Viewport.Context.VertexShader.SetConstantBuffer(0, commonConstants.Buffer);

                Viewport.Context.PixelShader.Set(psResolveWorldNormals);
                Viewport.Context.PixelShader.SetConstantBuffers(0, commonConstants.Buffer);
                Viewport.Context.PixelShader.SetShaderResources(0, gBufferCollection.GBufferSRVs);
                Viewport.Context.PixelShader.SetShaderResources(4, Viewport.DepthBufferSRV);
                Viewport.Context.PixelShader.SetSampler(0, D3DUtils.CreateSamplerState(address: TextureAddressMode.Clamp, filter: Filter.MinMagMipPoint));

                Viewport.Context.Draw(6, 0);
            }
            D3DUtils.EndPerfEvent(Viewport.Context);
        }

        /// <summary>
        /// 
        /// </summary>
        private void RenderEmissive()
        {
            D3DUtils.BeginPerfEvent(Viewport.Context, "Emissive");
            {
                UpdateViewConstants(true);

                Viewport.Context.OutputMerger.SetRenderTargets(Viewport.DepthBufferDSV, lightAccumulationTexture.RTV);
                Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(depthComparison: Comparison.LessEqual);
                Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget());
                Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.Front, depthClip: true);

                Viewport.Context.VertexShader.SetConstantBuffer(0, viewConstants.Buffer);

                Viewport.Context.PixelShader.SetConstantBuffer(0, viewConstants.Buffer);
                Viewport.Context.PixelShader.SetShaderResource(0, normalBasisCubemapTexture.SRV);
                Viewport.Context.PixelShader.SetShaderResource(1, Skybox ?? blankTexture.SRV);
                Viewport.Context.PixelShader.SetSampler(0, D3DUtils.CreateSamplerState(TextureAddressMode.Clamp, filter: Filter.MinMagMipPoint));

                RenderMeshes(MeshRenderPath.Deferred, new List<MeshRenderInstance>() { new MeshRenderInstance() { RenderMesh = skySphere, Transform = Matrix.Identity } });
            }
            D3DUtils.EndPerfEvent(Viewport.Context);
        }

        private void RenderForward()
        {
            meshes = CollectMeshInstances();

            D3DUtils.BeginPerfEvent(Viewport.Context, "Forward");
            {
                UpdateViewConstants(true);

                Viewport.Context.OutputMerger.SetRenderTargets(Viewport.DepthBufferDSV, lightAccumulationTexture.RTV);
                Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(depthComparison: Comparison.LessEqual, depthWriteMask: DepthWriteMask.Zero);
                Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.None, fillMode: FillMode.Solid, frontCounterClockwise: true, depthClip: true, multisampled: true);

                Viewport.Context.VertexShader.SetConstantBuffer(0, viewConstants.Buffer);

                Viewport.Context.PixelShader.SetConstantBuffer(0, viewConstants.Buffer);
                Viewport.Context.PixelShader.SetShaderResource(0, normalBasisCubemapTexture.SRV);

                RenderMeshes(MeshRenderPath.Forward, meshes);
            }
            D3DUtils.EndPerfEvent(Viewport.Context);
        }

        /// <summary>
        /// 
        /// </summary>
        private void PostProcess()
        {
            SharpDX.Mathematics.Interop.RawViewportF[] origViewports = Viewport.Context.Rasterizer.GetViewports<SharpDX.Mathematics.Interop.RawViewportF>();

            D3DUtils.BeginPerfEvent(Viewport.Context, "PostProcess");
            {
                PostProcessCollectSelections();
                PostProcessEditorPrimitives();
                PostProcessHBAO();
                PostProcessTAA();
                PostProcessDownScaleScene();
                PostProcessMeasureLuminance();
                PostProcessBloom();
                PostProcessColorLookupTable();
                PostProcessVignette();
                PostProcessSelectionOutline();
                PostProcessEditorComposite();
            }
            D3DUtils.EndPerfEvent(Viewport.Context);

            Viewport.Context.Rasterizer.SetViewports(origViewports);
        }

        /// <summary>
        /// Resolve from light accumulation to final render target
        /// </summary>
        private void Resolve()
        {
            if (RenderMode == DebugRenderMode.Default || RenderMode == DebugRenderMode.HBAO)
                return;

            D3DUtils.BeginPerfEvent(Viewport.Context, "DebugRenderMode");
            {
                Viewport.Context.OutputMerger.SetRenderTargets(null, Viewport.ColorBufferRTV);
                Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(false);
                Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget());
                Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.None);

                Viewport.Context.InputAssembler.SetIndexBuffer(null, SharpDX.DXGI.Format.Unknown, 0);
                Viewport.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
                Viewport.Context.InputAssembler.InputLayout = null;

                Viewport.Context.VertexShader.Set(vsFullscreenQuad);
                Viewport.Context.VertexShader.SetConstantBuffer(0, commonConstants.Buffer);

                Viewport.Context.PixelShader.Set(psDebugRenderMode);
                Viewport.Context.PixelShader.SetShaderResources(0, gBufferCollection.GBufferSRVs);
                Viewport.Context.PixelShader.SetShaderResources(4, Viewport.DepthBufferSRV);
                Viewport.Context.PixelShader.SetConstantBuffer(0, commonConstants.Buffer);
                Viewport.Context.PixelShader.SetSampler(0, D3DUtils.CreateSamplerState(address: TextureAddressMode.Clamp, filter: Filter.MinMagMipPoint));

                Viewport.Context.Draw(6, 0);
            }
            D3DUtils.EndPerfEvent(Viewport.Context);

            Viewport.Context.OutputMerger.SetRenderTargets(null, new RenderTargetView[5]);
        }

        #region -- Post Processing --
        /// <summary>
        /// 
        /// </summary>
        private void PostProcessTAA()
        {
            if (GFSDK_TXAA.TxaaEnabled)
            {
                D3DUtils.BeginPerfEvent(Viewport.Context, "TXAA");
                {
                    D3DUtils.BeginPerfEvent(Viewport.Context, "CameraMotionVectors");
                    {
                        // TXAA Camera motion vectors
                        Viewport.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(null, 0, 0));
                        Viewport.Context.InputAssembler.SetIndexBuffer(null, SharpDX.DXGI.Format.Unknown, 0);
                        txaaMotionVectorsTexture.Clear(Viewport.Context, new Color4(0, 0, 0, 0));

                        Matrix viewProjMatrix = camera.GetViewProjMatrix();
                        viewProjMatrix.Transpose();
                        Matrix prevViewProjMatrix = camera.GetPrevViewProjMatrix();
                        prevViewProjMatrix.Transpose();

                        IntPtr ptr1 = Marshal.AllocHGlobal(64);
                        IntPtr ptr2 = Marshal.AllocHGlobal(64);

                        Marshal.Copy(viewProjMatrix.ToArray(), 0, ptr1, 4 * 4);
                        Marshal.Copy(prevViewProjMatrix.ToArray(), 0, ptr2, 4 * 4);

                        GFSDK_TXAA.MotionVectorParameters mvParams = new GFSDK_TXAA.MotionVectorParameters
                        {
                            viewProj = ptr1,
                            prevViewProj = ptr2,
                            samples = 1
                        };

                        IntPtr motionGeneratorVtbl = Marshal.ReadIntPtr(Marshal.ReadIntPtr(txaaMotionVectorGenerator, 0), 0);
                        GFSDK_TXAA.GenerateMotionVectorFunc generateMotionVector = Marshal.GetDelegateForFunctionPointer<GFSDK_TXAA.GenerateMotionVectorFunc>(Marshal.ReadIntPtr(motionGeneratorVtbl, 1 * 8));
                        int retVal = generateMotionVector(Marshal.ReadIntPtr(txaaMotionVectorGenerator), Viewport.Context.NativePointer, txaaMotionVectorsTexture.RTV.NativePointer, Viewport.DepthBufferSRV.NativePointer, mvParams);

                        Marshal.FreeHGlobal(ptr1);
                        Marshal.FreeHGlobal(ptr2);
                    }
                    D3DUtils.EndPerfEvent(Viewport.Context);

                    D3DUtils.BeginPerfEvent(Viewport.Context, "Resolve");
                    {
                        // TXAA Resolve
                        Viewport.Context.OutputMerger.SetRenderTargets(null, null, null, null, null);

                        GFSDK_TXAA.NvTxaaFeedbackParameters feedbackParams = GFSDK_TXAA.NvTxaaFeedbackParameters.NvTxaaDefaultFeedback;
                        IntPtr feedbackParamsPtr = Marshal.AllocHGlobal(Marshal.SizeOf<GFSDK_TXAA.NvTxaaFeedbackParameters>());
                        Marshal.StructureToPtr<GFSDK_TXAA.NvTxaaFeedbackParameters>(feedbackParams, feedbackParamsPtr, true);

                        GFSDK_TXAA.GetJitter(out float[] jitter);

                        GFSDK_TXAA.NvTxaaPerFrameConstants constants = new GFSDK_TXAA.NvTxaaPerFrameConstants
                        {
                            xJitter = jitter[0],
                            yJitter = jitter[1],
                            mvScale = 1024.0f,
                            motionVecSelection = 3,
                            useRGB = 0,
                            frameBlendFactor = 0.04f,
                            dbg1 = 0,
                            bbScale = 1.0f,
                            enableClipping = 1,
                            useBHFilters = 1
                        };
                        //constants.isZFlipped = 1;
                        IntPtr constantsPtr = Marshal.AllocHGlobal(Marshal.SizeOf<GFSDK_TXAA.NvTxaaPerFrameConstants>());
                        Marshal.StructureToPtr<GFSDK_TXAA.NvTxaaPerFrameConstants>(constants, constantsPtr, true);

                        GFSDK_TXAA.NvTxaaResolveParametersDX11 resolveParams = new GFSDK_TXAA.NvTxaaResolveParametersDX11
                        {
                            txaaContext = txaaContext,
                            deviceContext = Viewport.Context.NativePointer,
                            resolveTarget = postProcessTexture.RTV.NativePointer,
                            msaaSource = lightAccumulationTexture.SRV.NativePointer,
                            msaaDepth = Viewport.DepthBufferSRV.NativePointer,
                            feedbackSource = txaaFeedbackTeture.SRV.NativePointer,
                            alphaResolveMode = 1,
                            feedback = feedbackParamsPtr,
                            perFrameConstants = constantsPtr
                        };

                        GFSDK_TXAA.NvTxaaMotionDX11 mParams = new GFSDK_TXAA.NvTxaaMotionDX11
                        {
                            motionVectors = txaaMotionVectorsTexture.SRV.NativePointer,
                            motionVectorsMS = txaaMotionVectorsTexture.SRV.NativePointer
                        };

                        IntPtr resolveParamsPtr = Marshal.AllocHGlobal(Marshal.SizeOf<GFSDK_TXAA.NvTxaaResolveParametersDX11>());
                        Marshal.StructureToPtr<GFSDK_TXAA.NvTxaaResolveParametersDX11>(resolveParams, resolveParamsPtr, true);

                        IntPtr mParamsPtr = Marshal.AllocHGlobal(Marshal.SizeOf<GFSDK_TXAA.NvTxaaMotionDX11>());
                        Marshal.StructureToPtr<GFSDK_TXAA.NvTxaaMotionDX11>(mParams, mParamsPtr, true);

                        int retCode = GFSDK_TXAA.ResolveFromMotionVectors(resolveParamsPtr, mParamsPtr);
                        Viewport.Context.CopyResource(postProcessTexture.Texture, txaaFeedbackTeture.Texture);

                        Marshal.FreeHGlobal(mParamsPtr);
                        Marshal.FreeHGlobal(resolveParamsPtr);
                        Marshal.FreeHGlobal(constantsPtr);
                        Marshal.FreeHGlobal(feedbackParamsPtr);
                    }
                    D3DUtils.EndPerfEvent(Viewport.Context);
                }
                D3DUtils.EndPerfEvent(Viewport.Context);
            }
            else
            {
                D3DUtils.BeginPerfEvent(Viewport.Context, "Resolve");
                {
                    Viewport.Context.CopyResource(lightAccumulationTexture.Texture, postProcessTexture.Texture);
                }
                D3DUtils.EndPerfEvent(Viewport.Context);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void PostProcessDownScaleScene()
        {
            D3DUtils.BeginPerfEvent(Viewport.Context, "Downscale4x4");
            {
                Viewport.Context.MapSubresource(postProcessConstants, 0, MapMode.WriteDiscard, MapFlags.None, out DataStream stream);
                {
                    float tU = 1.0f / (postProcessTexture.Texture.Description.Width);
                    float tV = 1.0f / (postProcessTexture.Texture.Description.Height);

                    for (int y = 0; y < 4; y++)
                    {
                        for (int x = 0; x < 4; x++)
                        {
                            stream.Write((x - 1.5f) * tU);
                            stream.Write((y - 1.5f) * tV);
                            stream.Write(Vector2.Zero);
                        }
                    }
                }
                Viewport.Context.UnmapSubresource(postProcessConstants, 0);

                Viewport.Context.OutputMerger.SetRenderTargets(null, scaledSceneTexture.RTV);
                Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(false);
                Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget());
                Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.None);
                Viewport.Context.Rasterizer.SetViewport(new SharpDX.Viewport(0, 0, scaledSceneTexture.Texture.Description.Width, scaledSceneTexture.Texture.Description.Height));

                Viewport.Context.InputAssembler.SetIndexBuffer(null, SharpDX.DXGI.Format.Unknown, 0);
                Viewport.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
                Viewport.Context.InputAssembler.InputLayout = null;

                Viewport.Context.VertexShader.Set(vsFullscreenQuad);
                Viewport.Context.VertexShader.SetConstantBuffer(0, commonConstants.Buffer);

                Viewport.Context.PixelShader.Set(psDownscale4x4);
                Viewport.Context.PixelShader.SetShaderResources(0, postProcessTexture.SRV);
                Viewport.Context.PixelShader.SetSampler(0, D3DUtils.CreateSamplerState(address: TextureAddressMode.Clamp, filter: Filter.MinMagMipPoint));
                Viewport.Context.PixelShader.SetConstantBuffer(1, postProcessConstants);

                Viewport.Context.Draw(6, 0);
            }
            D3DUtils.EndPerfEvent(Viewport.Context);
        }

        /// <summary>
        /// 
        /// </summary>
        private void PostProcessMeasureLuminance()
        {
            int curTexture = 3;

            D3DUtils.BeginPerfEvent(Viewport.Context, "SampleLuminanceInitial");
            {
                // first pass
                Viewport.Context.MapSubresource(postProcessConstants, 0, MapMode.WriteDiscard, MapFlags.None, out DataStream stream);
                {
                    float tU = 1.0f / (3.0f * toneMapTextures[curTexture].Texture.Description.Width);
                    float tV = 1.0f / (3.0f * toneMapTextures[curTexture].Texture.Description.Height);

                    for (int x = -1; x <= 1; x++)
                    {
                        for (int y = -1; y <= 1; y++)
                        {
                            stream.Write(x * tU);
                            stream.Write(y * tV);
                            stream.Write(Vector2.Zero);
                        }
                    }
                }
                Viewport.Context.UnmapSubresource(postProcessConstants, 0);

                toneMapTextures[curTexture].Clear(Viewport.Context, new Color4(0, 0, 0, 0));
                Viewport.Context.OutputMerger.SetRenderTargets(null, toneMapTextures[curTexture].RTV);
                Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(false);
                Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget());
                Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.None);
                Viewport.Context.Rasterizer.SetViewport(new SharpDX.Viewport(0, 0, toneMapTextures[curTexture].Texture.Description.Width, toneMapTextures[curTexture].Texture.Description.Height));

                Viewport.Context.InputAssembler.SetIndexBuffer(null, SharpDX.DXGI.Format.Unknown, 0);
                Viewport.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
                Viewport.Context.InputAssembler.InputLayout = null;

                Viewport.Context.VertexShader.Set(vsFullscreenQuad);
                Viewport.Context.VertexShader.SetConstantBuffer(0, commonConstants.Buffer);

                Viewport.Context.PixelShader.Set(psSampleLumInitial);
                Viewport.Context.PixelShader.SetShaderResources(0, scaledSceneTexture.SRV, toneMapTextures[5].SRV);
                Viewport.Context.PixelShader.SetSampler(0, D3DUtils.CreateSamplerState(address: TextureAddressMode.Clamp, filter: Filter.MinMagMipPoint));
                Viewport.Context.PixelShader.SetConstantBuffers(0, commonConstants.Buffer, postProcessConstants);

                Viewport.Context.Draw(6, 0);
                curTexture--;
            }
            D3DUtils.EndPerfEvent(Viewport.Context);

            D3DUtils.BeginPerfEvent(Viewport.Context, "SampleLuminanceIterative");
            {
                // iterative downscale
                while (curTexture > 0)
                {
                    Viewport.Context.MapSubresource(postProcessConstants, 0, MapMode.WriteDiscard, MapFlags.None, out DataStream stream);
                    {
                        float tU = 1.0f / (toneMapTextures[curTexture + 1].Texture.Description.Width);
                        float tV = 1.0f / (toneMapTextures[curTexture + 1].Texture.Description.Height);

                        for (int y = 0; y < 4; y++)
                        {
                            for (int x = 0; x < 4; x++)
                            {
                                stream.Write((x - 1.5f) * tU);
                                stream.Write((y - 1.5f) * tV);
                                stream.Write(Vector2.Zero);
                            }
                        }
                    }
                    Viewport.Context.UnmapSubresource(postProcessConstants, 0);

                    toneMapTextures[curTexture].Clear(Viewport.Context, new Color4(0, 0, 0, 0));
                    Viewport.Context.OutputMerger.SetRenderTargets(null, toneMapTextures[curTexture].RTV);
                    Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(false);
                    Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget());
                    Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.None);
                    Viewport.Context.Rasterizer.SetViewport(new SharpDX.Viewport(0, 0, toneMapTextures[curTexture].Texture.Description.Width, toneMapTextures[curTexture].Texture.Description.Height));

                    Viewport.Context.InputAssembler.SetIndexBuffer(null, SharpDX.DXGI.Format.Unknown, 0);
                    Viewport.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
                    Viewport.Context.InputAssembler.InputLayout = null;

                    Viewport.Context.VertexShader.Set(vsFullscreenQuad);
                    Viewport.Context.VertexShader.SetConstantBuffer(0, commonConstants.Buffer);

                    Viewport.Context.PixelShader.Set(psSampleLumIterative);
                    Viewport.Context.PixelShader.SetShaderResources(1, toneMapTextures[curTexture + 1].SRV);
                    Viewport.Context.PixelShader.SetSampler(0, D3DUtils.CreateSamplerState(address: TextureAddressMode.Clamp, filter: Filter.MinMagMipPoint));
                    Viewport.Context.PixelShader.SetConstantBuffer(1, postProcessConstants);

                    Viewport.Context.Draw(6, 0);
                    curTexture--;
                }
            }
            D3DUtils.EndPerfEvent(Viewport.Context);

            D3DUtils.BeginPerfEvent(Viewport.Context, "SampleLuminanceFinal");
            {
                // downscale 1x1
                Viewport.Context.MapSubresource(postProcessConstants, 0, MapMode.WriteDiscard, MapFlags.None, out DataStream stream);
                {
                    float tU = 1.0f / (toneMapTextures[1].Texture.Description.Width);
                    float tV = 1.0f / (toneMapTextures[1].Texture.Description.Height);

                    for (int y = 0; y < 4; y++)
                    {
                        for (int x = 0; x < 4; x++)
                        {
                            stream.Write((x - 1.5f) * tU);
                            stream.Write((y - 1.5f) * tV);
                            stream.Write(Vector2.Zero);
                        }
                    }
                }
                Viewport.Context.UnmapSubresource(postProcessConstants, 0);

                toneMapTextures[0].Clear(Viewport.Context, new Color4(0, 0, 0, 0));
                Viewport.Context.OutputMerger.SetRenderTargets(null, toneMapTextures[0].RTV);
                Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(false);
                Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget());
                Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.None);
                Viewport.Context.Rasterizer.SetViewport(new SharpDX.Viewport(0, 0, toneMapTextures[0].Texture.Description.Width, toneMapTextures[0].Texture.Description.Height));

                Viewport.Context.InputAssembler.SetIndexBuffer(null, SharpDX.DXGI.Format.Unknown, 0);
                Viewport.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
                Viewport.Context.InputAssembler.InputLayout = null;

                Viewport.Context.VertexShader.Set(vsFullscreenQuad);
                Viewport.Context.VertexShader.SetConstantBuffer(0, commonConstants.Buffer);

                Viewport.Context.PixelShader.Set(psSampleLumFinal);
                Viewport.Context.PixelShader.SetShaderResources(1, toneMapTextures[1].SRV);
                Viewport.Context.PixelShader.SetSampler(0, D3DUtils.CreateSamplerState(address: TextureAddressMode.Clamp, filter: Filter.MinMagMipPoint));
                Viewport.Context.PixelShader.SetConstantBuffer(1, postProcessConstants);

                Viewport.Context.Draw(6, 0);
            }
            D3DUtils.EndPerfEvent(Viewport.Context);

            D3DUtils.BeginPerfEvent(Viewport.Context, "CalculateAdaptedLuminance");
            {
                // calculate adapted luminance
                Viewport.Context.MapSubresource(postProcessConstants, 0, MapMode.WriteDiscard, MapFlags.None, out DataStream stream);
                {
                    stream.Write((float)lastDeltaTime);
                }
                Viewport.Context.UnmapSubresource(postProcessConstants, 0);

                toneMapTextures[4].Clear(Viewport.Context, new Color4(0, 0, 0, 0));
                Viewport.Context.OutputMerger.SetRenderTargets(null, toneMapTextures[4].RTV);
                Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(false);
                Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget());
                Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.None);
                Viewport.Context.Rasterizer.SetViewport(new SharpDX.Viewport(0, 0, toneMapTextures[4].Texture.Description.Width, toneMapTextures[4].Texture.Description.Height));

                Viewport.Context.InputAssembler.SetIndexBuffer(null, SharpDX.DXGI.Format.Unknown, 0);
                Viewport.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
                Viewport.Context.InputAssembler.InputLayout = null;

                Viewport.Context.VertexShader.Set(vsFullscreenQuad);
                Viewport.Context.VertexShader.SetConstantBuffer(0, commonConstants.Buffer);

                Viewport.Context.PixelShader.Set(psCalcAdaptedLum);
                Viewport.Context.PixelShader.SetShaderResources(0, toneMapTextures[5].SRV, toneMapTextures[0].SRV);
                Viewport.Context.PixelShader.SetSampler(0, D3DUtils.CreateSamplerState(address: TextureAddressMode.Clamp, filter: Filter.MinMagMipPoint));
                Viewport.Context.PixelShader.SetConstantBuffer(1, postProcessConstants);

                Viewport.Context.Draw(6, 0);

                // copy current luminance into previous
                Viewport.Context.ResolveSubresource(toneMapTextures[4].Texture, 0, toneMapTextures[5].Texture, 0, SharpDX.DXGI.Format.R32_Float);
                Viewport.Context.CopyResource(toneMapTextures[4].Texture, toneMapTextures[6].Texture);

                // read out average luminance
                Viewport.Context.MapSubresource(toneMapTextures[6].Texture, 0, MapMode.Read, MapFlags.None, out stream);
                {
                    // store into a histogram
                    float avgLuminance = stream.Read<float>();
                    luminanceHistogram.Add(avgLuminance);
                }
                Viewport.Context.UnmapSubresource(toneMapTextures[6].Texture, 0);
            }
            D3DUtils.EndPerfEvent(Viewport.Context);
        }

        /// <summary>
        /// 
        /// </summary>
        private void PostProcessBloom()
        {
            D3DUtils.BeginPerfEvent(Viewport.Context, "Bloom");
            {
                brightPassTexture.Clear(Viewport.Context, Color4.Black);
                blurTexture.Clear(Viewport.Context, Color4.Black);
                bloomSourceTexture.Clear(Viewport.Context, Color4.Black);
                bloomTextures[0].Clear(Viewport.Context, Color4.Black);
                bloomTextures[1].Clear(Viewport.Context, Color4.Black);
                bloomTextures[2].Clear(Viewport.Context, Color4.Black);

                D3DUtils.BeginPerfEvent(Viewport.Context, "BrightPass");
                {
                    Viewport.Context.OutputMerger.SetRenderTargets(null, brightPassTexture.RTV);
                    Viewport.Context.Rasterizer.SetViewport(new SharpDX.Viewport(0, 0, brightPassTexture.Texture.Description.Width, brightPassTexture.Texture.Description.Height));
                    Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(false);
                    Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget());
                    Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.None);

                    Viewport.Context.InputAssembler.SetIndexBuffer(null, SharpDX.DXGI.Format.Unknown, 0);
                    Viewport.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
                    Viewport.Context.InputAssembler.InputLayout = null;

                    Viewport.Context.VertexShader.Set(vsFullscreenQuad);
                    Viewport.Context.VertexShader.SetConstantBuffer(0, commonConstants.Buffer);

                    Viewport.Context.PixelShader.Set(psBrightPass);
                    Viewport.Context.PixelShader.SetShaderResources(0, scaledSceneTexture.SRV, toneMapTextures[4].SRV);
                    Viewport.Context.PixelShader.SetSampler(0, D3DUtils.CreateSamplerState(address: TextureAddressMode.Clamp, filter: Filter.MinMagMipPoint));

                    Viewport.Context.Draw(6, 0);
                }
                D3DUtils.EndPerfEvent(Viewport.Context);

                D3DUtils.BeginPerfEvent(Viewport.Context, "Blur");
                {
                    Viewport.Context.MapSubresource(postProcessConstants, 0, MapMode.WriteDiscard, MapFlags.None, out DataStream stream);
                    {
                        float tu = 1.0f / (float)blurTexture.Texture.Description.Width;
                        float tv = 1.0f / (float)blurTexture.Texture.Description.Height;

                        Vector4 vWhite = new Vector4(1, 1, 1, 1);
                        Vector4[] avSampleWeight = new Vector4[16];
                        Vector2[] avTexOffsets = new Vector2[16];

                        float totalWeight = 0.0f;
                        int index = 0;
                        for (int x = -2; x <= 2; x++)
                        {
                            for (int y = -2; y <= 2; y++)
                            {
                                if (Math.Abs(x) + Math.Abs(y) > 2)
                                    continue;

                                avTexOffsets[index] = new Vector2(x * tu, y * tv);
                                avSampleWeight[index] = (vWhite * GaussianDistribution((float)x, (float)y, 1.0f));
                                totalWeight += avSampleWeight[index].X;

                                index++;
                            }
                        }

                        for (int i = 0; i < index; i++)
                            avSampleWeight[i] /= totalWeight;

                        for (int i = 0; i < 16; i++)
                        {
                            stream.Write(avTexOffsets[i]);
                            stream.Write(Vector2.Zero);
                        }
                        for (int i = 0; i < 16; i++)
                            stream.Write(avSampleWeight[i] * BloomStrength);
                    }
                    Viewport.Context.UnmapSubresource(postProcessConstants, 0);

                    Viewport.Context.OutputMerger.SetRenderTargets(null, blurTexture.RTV);
                    Viewport.Context.Rasterizer.SetViewport(new SharpDX.Viewport(0, 0, blurTexture.Texture.Description.Width, blurTexture.Texture.Description.Height));
                    Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(false);
                    Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget());
                    Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.None);

                    Viewport.Context.InputAssembler.SetIndexBuffer(null, SharpDX.DXGI.Format.Unknown, 0);
                    Viewport.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
                    Viewport.Context.InputAssembler.InputLayout = null;

                    Viewport.Context.VertexShader.Set(vsFullscreenQuad);
                    Viewport.Context.VertexShader.SetConstantBuffer(0, commonConstants.Buffer);

                    Viewport.Context.PixelShader.Set(psGaussianBlur5x5);
                    Viewport.Context.PixelShader.SetShaderResources(0, brightPassTexture.SRV);
                    Viewport.Context.PixelShader.SetConstantBuffers(1, postProcessConstants);
                    Viewport.Context.PixelShader.SetSamplers(0, D3DUtils.CreateSamplerState(address: TextureAddressMode.Clamp, filter: Filter.MinMagMipLinear));

                    Viewport.Context.Draw(6, 0);
                }
                D3DUtils.EndPerfEvent(Viewport.Context);

                D3DUtils.BeginPerfEvent(Viewport.Context, "BloomSource");
                {
                    Viewport.Context.MapSubresource(postProcessConstants, 0, MapMode.WriteDiscard, MapFlags.None, out DataStream stream);
                    {
                        float tU = 1.0f / brightPassTexture.Texture.Description.Width;
                        float tV = 1.0f / brightPassTexture.Texture.Description.Height;

                        for (int y = 0; y < 2; y++)
                        {
                            for (int x = 0; x < 2; x++)
                            {
                                stream.Write((x - 0.5f) * tU);
                                stream.Write((y - 0.5f) * tV);
                                stream.Write(Vector2.Zero);
                            }
                        }
                    }
                    Viewport.Context.UnmapSubresource(postProcessConstants, 0);

                    Viewport.Context.OutputMerger.SetRenderTargets(null, bloomSourceTexture.RTV);
                    Viewport.Context.Rasterizer.SetViewport(new SharpDX.Viewport(0, 0, bloomSourceTexture.Texture.Description.Width, bloomSourceTexture.Texture.Description.Height));
                    Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(false);
                    Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget());
                    Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.None);

                    Viewport.Context.InputAssembler.SetIndexBuffer(null, SharpDX.DXGI.Format.Unknown, 0);
                    Viewport.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
                    Viewport.Context.InputAssembler.InputLayout = null;

                    Viewport.Context.VertexShader.Set(vsFullscreenQuad);
                    Viewport.Context.VertexShader.SetConstantBuffer(0, commonConstants.Buffer);

                    Viewport.Context.PixelShader.Set(psDownSample2x2);
                    Viewport.Context.PixelShader.SetShaderResources(0, blurTexture.SRV);
                    Viewport.Context.PixelShader.SetConstantBuffers(1, postProcessConstants);
                    Viewport.Context.PixelShader.SetSamplers(0, D3DUtils.CreateSamplerState(address: TextureAddressMode.Clamp, filter: Filter.MinMagMipLinear));

                    Viewport.Context.Draw(6, 0);
                }
                D3DUtils.EndPerfEvent(Viewport.Context);

                D3DUtils.BeginPerfEvent(Viewport.Context, "Blur");
                {
                    Viewport.Context.MapSubresource(postProcessConstants, 0, MapMode.WriteDiscard, MapFlags.None, out DataStream stream);
                    {
                        float tu = 1.0f / (float)bloomSourceTexture.Texture.Description.Width;
                        float tv = 1.0f / (float)bloomSourceTexture.Texture.Description.Height;

                        Vector4 vWhite = new Vector4(1, 1, 1, 1);
                        Vector4[] avSampleWeight = new Vector4[16];
                        Vector2[] avTexOffsets = new Vector2[16];

                        float totalWeight = 0.0f;
                        int index = 0;
                        for (int x = -2; x <= 2; x++)
                        {
                            for (int y = -2; y <= 2; y++)
                            {
                                if (Math.Abs(x) + Math.Abs(y) > 2)
                                    continue;

                                avTexOffsets[index] = new Vector2(x * tu, y * tv);
                                avSampleWeight[index] = (vWhite * GaussianDistribution((float)x, (float)y, 1.0f));
                                totalWeight += avSampleWeight[index].X;

                                index++;
                            }
                        }

                        for (int i = 0; i < index; i++)
                            avSampleWeight[i] /= totalWeight;

                        for (int i = 0; i < 16; i++)
                        {
                            stream.Write(avTexOffsets[i]);
                            stream.Write(Vector2.Zero);
                        }
                        for (int i = 0; i < 16; i++)
                            stream.Write(avSampleWeight[i]);
                    }
                    Viewport.Context.UnmapSubresource(postProcessConstants, 0);

                    Viewport.Context.OutputMerger.SetRenderTargets(null, bloomTextures[2].RTV);
                    Viewport.Context.Rasterizer.SetViewport(new SharpDX.Viewport(0, 0, bloomTextures[2].Texture.Description.Width, bloomTextures[2].Texture.Description.Height));
                    Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(false);
                    Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget());
                    Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.None);

                    Viewport.Context.InputAssembler.SetIndexBuffer(null, SharpDX.DXGI.Format.Unknown, 0);
                    Viewport.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
                    Viewport.Context.InputAssembler.InputLayout = null;

                    Viewport.Context.VertexShader.Set(vsFullscreenQuad);
                    Viewport.Context.VertexShader.SetConstantBuffer(0, commonConstants.Buffer);

                    Viewport.Context.PixelShader.Set(psGaussianBlur5x5);
                    Viewport.Context.PixelShader.SetShaderResources(0, bloomSourceTexture.SRV);
                    Viewport.Context.PixelShader.SetConstantBuffers(1, postProcessConstants);
                    Viewport.Context.PixelShader.SetSamplers(0, D3DUtils.CreateSamplerState(address: TextureAddressMode.Clamp, filter: Filter.MinMagMipLinear));

                    Viewport.Context.Draw(6, 0);
                }
                D3DUtils.EndPerfEvent(Viewport.Context);

                D3DUtils.BeginPerfEvent(Viewport.Context, "HorizontalBlur");
                {
                    Viewport.Context.MapSubresource(postProcessConstants, 0, MapMode.WriteDiscard, MapFlags.None, out DataStream stream);
                    {
                        float tu = 1.0f / bloomTextures[2].Texture.Description.Width;

                        float weight = 2.0f * GaussianDistribution(0, 0, 3.0f);
                        Vector4[] avColorWeights = new Vector4[16];
                        float[] afTexCoordOffsets = new float[16];

                        avColorWeights[0] = new Vector4(weight, weight, weight, 1.0f);
                        afTexCoordOffsets[0] = 0.0f;

                        for (int i = 1; i < 8; i++)
                        {
                            weight = 2.0f * GaussianDistribution(i, 0, 3.0f);
                            afTexCoordOffsets[i] = i * tu;
                            avColorWeights[i] = new Vector4(weight, weight, weight, 1.0f);
                        }
                        for (int i = 8; i < 15; i++)
                        {
                            avColorWeights[i] = avColorWeights[i - 7];
                            afTexCoordOffsets[i] = -afTexCoordOffsets[i - 7];
                        }

                        for (int i = 0; i < 16; i++)
                        {
                            stream.Write(afTexCoordOffsets[i]);
                            stream.Write(Vector3.Zero);
                        }
                        for (int i = 0; i < 16; i++)
                            stream.Write(avColorWeights[i]);
                    }
                    Viewport.Context.UnmapSubresource(postProcessConstants, 0);

                    Viewport.Context.OutputMerger.SetRenderTargets(null, bloomTextures[1].RTV);
                    Viewport.Context.Rasterizer.SetViewport(new SharpDX.Viewport(0, 0, bloomTextures[1].Texture.Description.Width, bloomTextures[1].Texture.Description.Height));
                    Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(false);
                    Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget());
                    Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.None);

                    Viewport.Context.InputAssembler.SetIndexBuffer(null, SharpDX.DXGI.Format.Unknown, 0);
                    Viewport.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
                    Viewport.Context.InputAssembler.InputLayout = null;

                    Viewport.Context.VertexShader.Set(vsFullscreenQuad);
                    Viewport.Context.VertexShader.SetConstantBuffer(0, commonConstants.Buffer);

                    Viewport.Context.PixelShader.Set(psBloomBlur);
                    Viewport.Context.PixelShader.SetShaderResources(0, bloomTextures[2].SRV);
                    Viewport.Context.PixelShader.SetConstantBuffers(1, postProcessConstants);
                    Viewport.Context.PixelShader.SetSamplers(0, D3DUtils.CreateSamplerState(address: TextureAddressMode.Clamp, filter: Filter.MinMagMipLinear));

                    Viewport.Context.Draw(6, 0);
                }
                D3DUtils.EndPerfEvent(Viewport.Context);

                D3DUtils.BeginPerfEvent(Viewport.Context, "VerticalBlur");
                {
                    Viewport.Context.MapSubresource(postProcessConstants, 0, MapMode.WriteDiscard, MapFlags.None, out DataStream stream);
                    {
                        float tu = 1.0f / bloomTextures[1].Texture.Description.Height;

                        float weight = 2.0f * GaussianDistribution(0, 0, 3.0f);
                        Vector4[] avColorWeights = new Vector4[16];
                        float[] afTexCoordOffsets = new float[16];

                        avColorWeights[0] = new Vector4(weight, weight, weight, 1.0f);
                        afTexCoordOffsets[0] = 0.0f;

                        for (int i = 1; i < 8; i++)
                        {
                            weight = 2.0f * GaussianDistribution(i, 0, 3.0f);
                            afTexCoordOffsets[i] = i * tu;
                            avColorWeights[i] = new Vector4(weight, weight, weight, 1.0f);
                        }
                        for (int i = 8; i < 15; i++)
                        {
                            avColorWeights[i] = avColorWeights[i - 7];
                            afTexCoordOffsets[i] = -afTexCoordOffsets[i - 7];
                        }

                        for (int i = 0; i < 16; i++)
                        {
                            stream.Write(0.0f);
                            stream.Write(afTexCoordOffsets[i]);
                            stream.Write(Vector2.Zero);
                        }
                        for (int i = 0; i < 16; i++)
                            stream.Write(avColorWeights[i]);
                    }
                    Viewport.Context.UnmapSubresource(postProcessConstants, 0);

                    Viewport.Context.OutputMerger.SetRenderTargets(null, bloomTextures[0].RTV);
                    Viewport.Context.Rasterizer.SetViewport(new SharpDX.Viewport(0, 0, bloomTextures[0].Texture.Description.Width, bloomTextures[0].Texture.Description.Height));
                    Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(false);
                    Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget());
                    Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.None);

                    Viewport.Context.InputAssembler.SetIndexBuffer(null, SharpDX.DXGI.Format.Unknown, 0);
                    Viewport.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
                    Viewport.Context.InputAssembler.InputLayout = null;

                    Viewport.Context.VertexShader.Set(vsFullscreenQuad);
                    Viewport.Context.VertexShader.SetConstantBuffer(0, commonConstants.Buffer);

                    Viewport.Context.PixelShader.Set(psBloomBlur);
                    Viewport.Context.PixelShader.SetShaderResources(0, bloomTextures[1].SRV);
                    Viewport.Context.PixelShader.SetConstantBuffers(1, postProcessConstants);
                    Viewport.Context.PixelShader.SetSamplers(0, D3DUtils.CreateSamplerState(address: TextureAddressMode.Clamp, filter: Filter.MinMagMipLinear));

                    Viewport.Context.Draw(6, 0);
                }
                D3DUtils.EndPerfEvent(Viewport.Context);

                D3DUtils.BeginPerfEvent(Viewport.Context, "RenderBloom");
                {
                    Viewport.Context.OutputMerger.SetRenderTargets(null, postProcessTexture.RTV);
                    Viewport.Context.Rasterizer.SetViewport(new SharpDX.Viewport(0, 0, lightAccumulationTexture.Texture.Description.Width, lightAccumulationTexture.Texture.Description.Height));
                    Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(false);
                    Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(new RenderTargetBlendDescription() { IsBlendEnabled = true, SourceBlend = BlendOption.One, DestinationBlend = BlendOption.One, BlendOperation = BlendOperation.Add, SourceAlphaBlend = BlendOption.One, DestinationAlphaBlend = BlendOption.One, AlphaBlendOperation = BlendOperation.Add, RenderTargetWriteMask = ColorWriteMaskFlags.All });
                    Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.None);

                    Viewport.Context.InputAssembler.SetIndexBuffer(null, SharpDX.DXGI.Format.Unknown, 0);
                    Viewport.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
                    Viewport.Context.InputAssembler.InputLayout = null;

                    Viewport.Context.VertexShader.Set(vsFullscreenQuad);
                    Viewport.Context.VertexShader.SetConstantBuffer(0, commonConstants.Buffer);

                    Viewport.Context.PixelShader.Set(psRenderBloom);
                    Viewport.Context.PixelShader.SetShaderResources(0, bloomTextures[0].SRV);
                    Viewport.Context.PixelShader.SetSamplers(0, D3DUtils.CreateSamplerState(address: TextureAddressMode.Clamp, filter: Filter.MinMagMipLinear));

                    Viewport.Context.Draw(6, 0);
                }
                D3DUtils.EndPerfEvent(Viewport.Context);
            }
            D3DUtils.EndPerfEvent(Viewport.Context);
        }

        /// <summary>
        /// 
        /// </summary>
        private void PostProcessHBAO()
        {
            if (!HBAOEnabled)
                return;

            D3DUtils.BeginPerfEvent(Viewport.Context, "HBAO");
            {
                GFSDK_TXAA.GetJitter(out float[] jitter);

                GFSDK_SSAO.InputData inputData = new GFSDK_SSAO.InputData
                {
                    DepthData =
                    {
                        pFullResDepthTextureSRV = Viewport.DepthBufferSRV.NativePointer,
                        DepthTextureType = GFSDK_SSAO.DepthTextureType.HardwareDepths,
                        MetersToViewSpaceUnits = 1.0f,
                        ProjectionMatrix =
                        {
                            Data = camera.GetProjMatrix(jitter),
                            Layout = GFSDK_SSAO.MatrixLayout.RowMajorOrder
                        },
                        Viewport = GFSDK_SSAO.InputViewport.FromViewport(new SharpDX.Viewport(0, 0, Viewport.DepthBuffer.Description.Width, Viewport.DepthBuffer.Description.Height, 0.0f, 1.0f))
                    },

                    NormalData =
                    {
                        Enable = true,
                        pFullResNormalTextureSRV = worldNormalsForHBAOTexture.SRV.NativePointer,
                        WorldToViewMatrix = {Data = Matrix.Scaling(-1, 1, 1) * camera.GetViewMatrix()},
                        DecodeScale = 2.0f,
                        DecodeBias = -1.0f
                    }
                };

                inputData.NormalData.WorldToViewMatrix.Layout = GFSDK_SSAO.MatrixLayout.RowMajorOrder;

                GFSDK_SSAO.Output output = new GFSDK_SSAO.Output
                {
                    pRenderTargetView = lightAccumulationTexture.RTV.NativePointer,

                    Blend =
                    {
                        Mode = (RenderMode == DebugRenderMode.HBAO)
                            ? GFSDK_SSAO.BlendMode.OverwriteRGB
                            : GFSDK_SSAO.BlendMode.MultiplyRGB
                    }
                };

                int retVal = hbaoContext.RenderAO(Viewport.Context, inputData, new GFSDK_SSAO.Parameters(true), output, GFSDK_SSAO.RenderMask.RenderAO);
#if FROSTY_DEVELOPER
                System.Diagnostics.Debug.Assert(retVal == 0);
#endif
            }
            D3DUtils.EndPerfEvent(Viewport.Context);
        }

        /// <summary>
        /// 
        /// </summary>
        private void PostProcessColorLookupTable()
        {
            D3DUtils.BeginPerfEvent(Viewport.Context, "ColorLookupTable");
            {
                Viewport.Context.OutputMerger.SetRenderTargets(null, finalColorTexture.RTV);
                Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(false);
                Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget());
                Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.None);
                Viewport.Context.Rasterizer.SetViewport(new SharpDX.Viewport(0, 0, Viewport.ColorBuffer.Description.Width, Viewport.ColorBuffer.Description.Height));

                Viewport.Context.InputAssembler.SetIndexBuffer(null, SharpDX.DXGI.Format.Unknown, 0);
                Viewport.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
                Viewport.Context.InputAssembler.InputLayout = null;

                Viewport.Context.VertexShader.Set(vsFullscreenQuad);
                Viewport.Context.VertexShader.SetConstantBuffer(0, commonConstants.Buffer);

                if (LookupTable != null)
                {
                    Texture2D lookupTableTexture = LookupTable.ResourceAs<Texture2D>();
                    lookupTableConstants.UpdateData(Viewport.Context, new TableLookupConstants()
                    {
                        LutSize = lookupTableTexture.Description.Width,
                        FlipY = (lookupTableTexture.Description.Width == 33) ? 1.0f : 0.0f
                    });

                    Viewport.Context.PixelShader.Set(psLookupTable);
                    Viewport.Context.PixelShader.SetConstantBuffer(1, lookupTableConstants.Buffer);
                }
                else
                {
                    // otherwise just resolve to final color
                    Viewport.Context.PixelShader.Set(psResolve);
                }

                Viewport.Context.PixelShader.SetShaderResources(0, postProcessTexture.SRV, LookupTable);
                Viewport.Context.PixelShader.SetSampler(0, D3DUtils.CreateSamplerState(address: TextureAddressMode.Clamp, filter: Filter.MinMagMipPoint));
                Viewport.Context.PixelShader.SetSampler(1, D3DUtils.CreateSamplerState(address: TextureAddressMode.Clamp, filter: Filter.MinMagMipLinear));
                Viewport.Context.Draw(6, 0);
            }
            D3DUtils.EndPerfEvent(Viewport.Context);
        }

        protected virtual void PostProcessVignette()
        {
            D3DUtils.BeginPerfEvent(Viewport.Context, "Vignette");
            {
                Viewport.Context.OutputMerger.SetRenderTargets(null, postProcessTexture.RTV);
                Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(false);
                Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget());
                Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.None);

                Viewport.Context.InputAssembler.SetIndexBuffer(null, SharpDX.DXGI.Format.Unknown, 0);
                Viewport.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
                Viewport.Context.InputAssembler.InputLayout = null;

                Viewport.Context.VertexShader.Set(vsFullscreenQuad);
                Viewport.Context.VertexShader.SetConstantBuffer(0, commonConstants.Buffer);

                globalConstants.UpdateData(Viewport.Context, new GlobalConstants()
                {
                    VignetteColor = new Vector4(Globals.VignetteColor, Globals.VignetteOpacity),
                    VignetteParams = new Vector3(Globals.VignetteScale, Globals.VignetteExponent == 0 ? 0 : 1 / Globals.VignetteExponent)
                });

                Viewport.Context.PixelShader.Set(psVignette);
                Viewport.Context.PixelShader.SetConstantBuffer(0, globalConstants.Buffer);
                Viewport.Context.PixelShader.SetShaderResources(0, finalColorTexture.SRV);
                Viewport.Context.PixelShader.SetSampler(0, D3DUtils.CreateSamplerState(address: TextureAddressMode.Clamp, filter: Filter.MinMagMipPoint));

                Viewport.Context.Draw(6, 0);
            }
            D3DUtils.EndPerfEvent(Viewport.Context);
        }

        /// <summary>
        /// Collect the selected objects and render them to the selection buffer
        /// </summary>
        private void PostProcessCollectSelections()
        {
            if (meshes.Count == 0)
                return;

            D3DUtils.BeginPerfEvent(Viewport.Context, "CollectSelections");
            {
                // need to update the view constants to get a non jittered matrix
                UpdateViewConstants(false);

                Viewport.Context.OutputMerger.SetRenderTargets(selectionDepthTexture.DSV, null, null);
                Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(depthComparison: Comparison.LessEqual);
                Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget());
                Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.Front, depthClip: true);

                Viewport.Context.VertexShader.SetConstantBuffer(0, viewConstants.Buffer);

                Viewport.Context.PixelShader.SetConstantBuffer(0, viewConstants.Buffer);
                Viewport.Context.PixelShader.SetShaderResource(0, normalBasisCubemapTexture.SRV);
                Viewport.Context.PixelShader.SetSampler(0, D3DUtils.CreateSamplerState(TextureAddressMode.Clamp, filter: Filter.MinMagMipPoint));

                RenderMeshes(MeshRenderPath.Selection, new List<MeshRenderInstance>() { meshes[0] });
            }
            D3DUtils.EndPerfEvent(Viewport.Context);
        }

        /// <summary>
        /// 
        /// </summary>
        private void PostProcessEditorPrimitives()
        {
            D3DUtils.BeginPerfEvent(Viewport.Context, "EditorPrimitives");
            {
                // resolve main depth into MSAA depth target
                {
                    Viewport.Context.OutputMerger.SetRenderTargets(editorCompositeDepthTexture.DSV, null, null);
                    Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(true, depthComparison: Comparison.Less);
                    Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget());
                    Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.None);

                    Viewport.Context.InputAssembler.SetIndexBuffer(null, SharpDX.DXGI.Format.Unknown, 0);
                    Viewport.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
                    Viewport.Context.InputAssembler.InputLayout = null;

                    Viewport.Context.VertexShader.Set(vsFullscreenQuad);
                    Viewport.Context.VertexShader.SetConstantBuffer(0, commonConstants.Buffer);

                    Viewport.Context.PixelShader.Set(psResolveDepthToMsaa);
                    Viewport.Context.PixelShader.SetShaderResources(0, Viewport.DepthBufferSRV);
                    Viewport.Context.PixelShader.SetSampler(0, D3DUtils.CreateSamplerState(address: TextureAddressMode.Clamp, filter: Filter.MinMagMipPoint));

                    Viewport.Context.Draw(6, 0);
                }

                // render editor primitives
                Viewport.Context.OutputMerger.SetRenderTargets(editorCompositeDepthTexture.DSV, editorCompositeTexture.RTV);
                Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(depthComparison: Comparison.LessEqual, depthWriteMask: DepthWriteMask.Zero);
                Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget());
                Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.Front, depthClip: true);

                Viewport.Context.VertexShader.SetConstantBuffer(0, viewConstants.Buffer);

                Viewport.Context.PixelShader.SetConstantBuffer(0, viewConstants.Buffer);
                Viewport.Context.PixelShader.SetShaderResource(0, normalBasisCubemapTexture.SRV);
                Viewport.Context.PixelShader.SetSampler(0, D3DUtils.CreateSamplerState(TextureAddressMode.Clamp, filter: Filter.MinMagMipPoint));

                RenderMeshes(MeshRenderPath.Forward, editorMeshes);
            }
            D3DUtils.EndPerfEvent(Viewport.Context);
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void PostProcessSelectionOutline()
        {
            D3DUtils.BeginPerfEvent(Viewport.Context, "SelectionOutline");
            {
                Viewport.Context.OutputMerger.SetRenderTargets(null, selectionOutlineTexture.RTV);
                Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(false);
                Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget());
                Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.None);

                Viewport.Context.InputAssembler.SetIndexBuffer(null, SharpDX.DXGI.Format.Unknown, 0);
                Viewport.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
                Viewport.Context.InputAssembler.InputLayout = null;

                Viewport.Context.VertexShader.Set(vsFullscreenQuad);
                Viewport.Context.VertexShader.SetConstantBuffer(0, commonConstants.Buffer);

                Viewport.Context.PixelShader.Set(psSelectionOutline);
                Viewport.Context.PixelShader.SetShaderResources(0, postProcessTexture.SRV, selectionDepthTexture.SRV);
                Viewport.Context.PixelShader.SetSampler(0, D3DUtils.CreateSamplerState(address: TextureAddressMode.Clamp, filter: Filter.MinMagMipPoint));

                Viewport.Context.Draw(6, 0);
            }
            D3DUtils.EndPerfEvent(Viewport.Context);
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void PostProcessEditorComposite()
        {
            D3DUtils.BeginPerfEvent(Viewport.Context, "EditorComposite");
            {
                Viewport.Context.OutputMerger.SetRenderTargets(null, Viewport.ColorBufferRTV);
                Viewport.Context.OutputMerger.DepthStencilState = D3DUtils.CreateDepthStencilState(false);
                Viewport.Context.OutputMerger.BlendState = D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget());
                Viewport.Context.Rasterizer.State = D3DUtils.CreateRasterizerState(CullMode.None);

                Viewport.Context.InputAssembler.SetIndexBuffer(null, SharpDX.DXGI.Format.Unknown, 0);
                Viewport.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
                Viewport.Context.InputAssembler.InputLayout = null;

                Viewport.Context.VertexShader.Set(vsFullscreenQuad);
                Viewport.Context.VertexShader.SetConstantBuffer(0, commonConstants.Buffer);

                Viewport.Context.PixelShader.Set(psEditorComposite);
                Viewport.Context.PixelShader.SetShaderResources(0, selectionOutlineTexture.SRV, editorCompositeTexture.SRV);
                Viewport.Context.PixelShader.SetSampler(0, D3DUtils.CreateSamplerState(address: TextureAddressMode.Clamp, filter: Filter.MinMagMipPoint));

                Viewport.Context.Draw(6, 0);
            }
            D3DUtils.EndPerfEvent(Viewport.Context);
        }
        #endregion

        #endregion

        /// <summary>
        /// Calculates a bounding box that encompasses all render meshes in the current world
        /// </summary>
        protected virtual BoundingBox CalcWorldBoundingBox()
        {
            return new BoundingBox();
        }

#if FROSTY_DEVELOPER
        /// <summary>
        /// Sets the next frame for capturing
        /// </summary>
        public void CaptureNextFrame()
        {
            renderDocCaptureState = RenderDocCaptureState.BeginCapture;
        }
#endif

        /// <summary>
        /// Do any actions required at the beginning of the frame
        /// </summary>
        protected virtual void BeginFrameActions()
        {
#if FROSTY_DEVELOPER
            // begin frame capturing if requested
            if (renderDocCaptureState == RenderDocCaptureState.BeginCapture)
            {
                renderDocApi.StartFrameCapture(Viewport.Device, IntPtr.Zero);
                renderDocCaptureState = RenderDocCaptureState.CaptureInProgress;
            }
#endif
        }

        /// <summary>
        /// Do any actions required at the end of the frame (where present would normally occur)
        /// </summary>
        protected virtual void EndFrameActions()
        {
#if FROSTY_DEVELOPER
            // end frame capturing and launch ui if in progress
            if (renderDocCaptureState == RenderDocCaptureState.CaptureInProgress)
            {
                renderDocApi.EndFrameCapture(Viewport.Device, IntPtr.Zero);
                renderDocApi.LaunchReplayUI(true, "");
                renderDocCaptureState = RenderDocCaptureState.NotStarted;
            }
#endif
        }

#if FROSTY_DEVELOPER
        /// <summary>
        /// Attempts to initialize the renderdoc api, if dll is present
        /// </summary>
        protected virtual void InitializeRenderDoc()
        {
            try
            {
                // try to load renderdoc
                renderDocApi = RenderDoc.GetAPI(10101);
                renderDocApi.SetCaptureOptionU32(RENDERDOC_CaptureOption.eRENDERDOC_Option_DebugOutputMute, 0);
                renderDocApi.SetCaptureOptionU32(RENDERDOC_CaptureOption.eRENDERDOC_Option_HookIntoChildren, 1);
                renderDocApi.SetActiveWindow(null, IntPtr.Zero);
            }
            catch
            {
                // failed to load renderdoc, ignore.
            }
        }
#endif

        protected virtual float GaussianDistribution(float x, float y, float rho)
        {
            float g = 1.0f / (float)Math.Sqrt(2.0f * Math.PI * rho * rho);
            g *= (float)Math.Exp(-(x * x + y * y) / (2 * rho * rho));

            return g;
        }
    }
}
