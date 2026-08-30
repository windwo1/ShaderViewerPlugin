using FrostySdk.IO;
using MeshSetPlugin.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Effects;

namespace MeshSetPlugin.ShaderData
{
    public enum ShaderSkinningMethod
    {
        ShaderSkinningMethod_None = 0,
        ShaderSkinningMethod_Linear1Bone = 1,
        ShaderSkinningMethod_Linear2Bone = 2,
        ShaderSkinningMethod_Linear4Bone = 4,
        ShaderSkinningMethod_Linear6Bone = 6,
        ShaderSkinningMethod_Linear8Bone = 8,
        ShaderSkinningMethod_Null = 9,
        ShaderSkinningMethod_DualQuaternion4Bone = 10,
        ShaderSkinningMethodCount = 11
    }

    public enum ShaderRenderMode
    {
        ShaderRenderMode_Forward = 0,
        ShaderRenderMode_ForwardSimple = 1,
        ShaderRenderMode_ZOnly = 2,
        ShaderRenderMode_DeferredShadingGBufferLayout0 = 3,
        ShaderRenderMode_DeferredShadingGBufferLayout1 = 4,
        ShaderRenderMode_DeferredShadingGBufferLayout2 = 5,
        ShaderRenderMode_DeferredShadingGBufferLayout3 = 6,
        ShaderRenderMode_DeferredShadingGBufferLayout4 = 7,
        ShaderRenderMode_DeferredShadingGBufferLayout5 = 8,
        ShaderRenderMode_DeferredShadingGBufferLayout6 = 9,
        ShaderRenderMode_DeferredShadingGBufferLayout7 = 10,
        ShaderRenderMode_ForwardEmissive = 11,
        ShaderRenderMode_VelocityVector = 12,
        ShaderRenderMode_DistortionVector = 13,
        ShaderRenderMode_DebugMulti = 14,
        ShaderRenderMode_ForwardOpaque_RuntimeOnly = 15,
        ShaderRenderModeCount = 16
    }

    public enum ShaderInstancingMethod
    {
        ShaderInstancingMethod_None = 0,
        ShaderInstancingMethod_ObjectTransform4x3Half = 1,
        ShaderInstancingMethod_ObjectTransform4x3InstanceData4x1Half = 2,
        ShaderInstancingMethod_ObjectTransform4x3InstanceData4x2Half = 3,
        ShaderInstancingMethod_WorldTransform4x3Float = 4,
        ShaderInstancingMethod_WorldTransform4x3FloatInstanceData4x2Half = 5,
        ShaderInstancingMethod_PrevWorldTransform4x3FloatInstanceData4x2Half = 6,
        ShaderInstancingMethod_ObjectTranslationScaleHalf = 7,
        ShaderInstancingMethod_ObjectTranslationScaleHalfInstanceData4x1Half = 8,
        ShaderInstancingMethod_ObjectTranslationScaleHalfInstanceData4x2Half = 9,
        ShaderInstancingMethod_PositionStream = 10,
        ShaderInstancingMethod_PositionStreamAux = 11,
        ShaderInstancingMethod_DxBuffer = 12,
        ShaderInstancingMethod_DxBufferInstanceData4x1Float = 13,
        ShaderInstancingMethod_DxBufferInstanceData4x2Float = 14,
        ShaderInstancingMethod_Manual = 15,
        ShaderInstancingMethodCount = 16
    }

    public class ShaderSolutionState
    {
        public uint surfaceShaderNameHash;
        public uint vertexShaderFragmentNameHash;
        public uint tessellationShaderFragmentNameHash;
        public uint geometryDeclarationHash;
        public byte[] stateInfo1;
        public byte[] stateInfo2;

        public ShaderSkinningMethod skinningMethod;
        public ShaderRenderMode renderMode;
        public byte unknown;
        public ShaderInstancingMethod instancingMethod;

        public bool tessellationEnable;

        public ShaderSolutionState()
        {
        }

        public ShaderSolutionState(NativeReader reader)
        {
            surfaceShaderNameHash = reader.ReadUInt();
            vertexShaderFragmentNameHash = reader.ReadUInt();
            tessellationShaderFragmentNameHash = reader.ReadUInt();
            geometryDeclarationHash = reader.ReadUInt();
            stateInfo1 = reader.ReadBytes(8);
            stateInfo2 = reader.ReadBytes(8);

            skinningMethod = (ShaderSkinningMethod)(stateInfo1[1] >> 4);
            renderMode = (ShaderRenderMode)(stateInfo1[1] & 0x0F);
            unknown = (byte)(stateInfo1[2] >> 4);
            instancingMethod = (ShaderInstancingMethod)(stateInfo1[2] & 0x0F);

            tessellationEnable = stateInfo2[0] == 1;
        }
    }
}
