using FrostySdk;
using FrostySdk.Attributes;
using FrostySdk.Ebx;
using FrostySdk.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MeshSetPlugin.ShaderData
{
    [EbxClassMeta(EbxFieldType.Struct)]
    public class PermutationPair
    {
        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.UInt32, arrayType: EbxFieldType.Struct)]
        public uint GeometryDeclarationHash { get; set; }

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.Boolean)]
        public bool DoubleSided { get; set; }

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.Array, arrayType: EbxFieldType.Struct)]
        public ShaderGraphPermutation PixelShader => ps;

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.Array, arrayType: EbxFieldType.Struct)]
        public ShaderGraphPermutation VertexShader => vs;

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.Array, arrayType: EbxFieldType.Struct)]
        public SolutionState SolutionState => state;

        public ShaderGraphPermutation ps = new ShaderGraphPermutation();
        public ShaderGraphPermutation vs = new ShaderGraphPermutation();
        public SolutionState state;

        public bool IsForwardRendered()
        {
            string renderMode = state.RenderMode.ToString();

            if (renderMode == "forward" || renderMode == "forwardSimple" || renderMode == "dynamicEnvmap" || renderMode.Contains("Emissive") || renderMode.Contains("Unlit"))
                return true;

            Type type = TypeLibrary.GetType("ShaderRenderMode");
            // some games start the gbuffer layout count at 0, some start it at 1
            string deferredName = type.GetEnumNames().Contains("ShaderRenderMode_DeferredShadingGBufferLayout4") ? "deferredShadingGBufferLayout4" : "deferredShadingGBufferLayout3";

            return renderMode != deferredName;
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RenderPath
    {
        [IsReadOnly]
        public CString RenderPathName { get; set; } = "";

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.CString)]
        public CString ShaderType
        {
            get
            {
                Type type = TypeLibrary.GetType("SurfaceShaderType");
                string typeStr;
                if (type != null)
                {
                    typeStr = type.GetEnumName(shaderType);
                    if (!string.IsNullOrEmpty(typeStr))
                    {
                        typeStr = typeStr.Replace("SurfaceShaderType_", "");
                        typeStr = char.ToLowerInvariant(typeStr[0]) + typeStr.Substring(1);
                    }
                    else
                    {
                        typeStr = "INVALID";
                    }
                }
                else
                {
                    typeStr = "INVALID";
                }
                return typeStr;
            }
        }

        [IsReadOnly]
        [Description("Contains information for pairs of pixel and vertex shader permutations.")]
        [EbxFieldMeta(EbxFieldType.Array, arrayType: EbxFieldType.Struct)]
        public List<PermutationPair> PermutationPairs { get; set; } = new List<PermutationPair>();

        public uint shaderType;
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class ShaderGraphData
    {
        [IsReadOnly]
        [Category("Annotations")]
        [EbxFieldMeta(EbxFieldType.Guid)]
        public Guid Guid { get; set; } = default;

        [IsReadOnly]
        [Category("Annotations")]
        public CString Name { get; set; } = "";

        [IsReadOnly]
        [Category("Misc")]
        [Description("Contains shader information for each render path in the database.")]
        [EbxFieldMeta(EbxFieldType.Array, arrayType: EbxFieldType.Struct)]
        public List<RenderPath> RenderPaths { get; set; } = new List<RenderPath>();
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class ExternalValue
    {
        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.CString)]
        public CString Name { get; set; }

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.Struct)]
        public dynamic DefaultValue { get; set; }

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.CString)]
        public CString Type { get; set; }

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.Boolean)]
        public bool Required { get; set; }

        public ExternalValue()
        {
            DefaultValue = TypeLibrary.CreateObject("Vec4");
            DefaultValue.x = 1f;
            DefaultValue.y = 1f;
            DefaultValue.z = 1f;
            DefaultValue.w = 1f;
            //Type = TypeLibrary.CreateObject("ExternalValueConstantType");
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class SolutionState
    {
        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint SurfaceShaderNameHash { get; set; }

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint VertexShaderFragmentNameHash { get; set; }

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint TessellationShaderFragmentNameHash { get; set; }

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint GeometryDeclarationNameHash { get; set; }

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.Boolean)]
        public bool TessellationEnable { get; set; }

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.CString)]
        public CString SkinningMethod
        {
            get
            {
                Type type = TypeLibrary.GetType("ShaderSkinningMethod");
                string typeStr;
                if (type != null)
                {
                    typeStr = type.GetEnumName(skinningMethodType);
                    if (!string.IsNullOrEmpty(typeStr))
                    {
                        typeStr = typeStr.Replace("ShaderSkinningMethod_", "");
                        typeStr = char.ToLowerInvariant(typeStr[0]) + typeStr.Substring(1);
                    }
                    else
                    {
                        typeStr = "INVALID";
                    }
                }
                else
                {
                    typeStr = "INVALID";
                }
                return typeStr;
            }
        }

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.CString)]
        public CString RenderMode
        {
            get
            {
                Type type = TypeLibrary.GetType("ShaderRenderMode");
                string typeStr;
                if (type != null)
                {
                    typeStr = type.GetEnumName(renderModeType);
                    if (!string.IsNullOrEmpty(typeStr))
                    {
                        typeStr = typeStr.Replace("ShaderRenderMode_", "");
                        typeStr = char.ToLowerInvariant(typeStr[0]) + typeStr.Substring(1);
                    }
                    else
                    {
                        typeStr = "INVALID";
                    }
                }
                else
                {
                    typeStr = "INVALID";
                }
                return typeStr;
            }
        }

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.CString)]
        public CString InstancingMethod
        {
            get
            {
                Type type = TypeLibrary.GetType("ShaderInstancingMethod");
                string typeStr;
                if (type != null)
                {
                    typeStr = type.GetEnumName(instancingType);
                    if (!string.IsNullOrEmpty(typeStr))
                    {
                        typeStr = typeStr.Replace("ShaderInstancingMethod_", "");
                        typeStr = char.ToLowerInvariant(typeStr[0]) + typeStr.Substring(1);
                    }
                    else
                    {
                        typeStr = "INVALID";
                    }
                }
                else
                {
                    typeStr = "INVALID";
                }
                return typeStr;
            }
        }

        public uint skinningMethodType;
        public uint renderModeType;
        public uint instancingType;
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class ExternalBuffer
    {
        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.CString)]
        public CString Name { get; set; }

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.CString)]
        public CString Type { get; set; }

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.Boolean)]
        public bool Required { get; set; }

        public ExternalBuffer()
        {
            //Type = TypeLibrary.CreateObject("ShaderValueType");
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class ConstantFunction
    {
        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.CString)]
        public CString Name
        {
            get
            {
                Type type = TypeLibrary.GetType("ShaderConstantFunction");
                string funcStr;
                if (type != null)
                {
                    funcStr = type.GetEnumName(funcType);
                    if (!string.IsNullOrEmpty(funcStr))
                    {
                        funcStr = funcStr.Replace("ShaderConstantFunction_", "");
                        funcStr = char.ToLowerInvariant(funcStr[0]) + funcStr.Substring(1);
                    }
                    else
                    {
                        funcStr = "INVALID";
                    }
                }
                else
                {
                    funcStr = "INVALID";
                }
                return funcStr;
            }
        }

        //[IsReadOnly]
        //[EbxFieldMeta(EbxFieldType.CString)]
        //public CString Type { get; set; }

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint CBufferIndex { get; set; }

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint ArraySize { get; set; }

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint MatrixDims { get; set; }

        public uint funcType;
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class ConstantTexture
    {
        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.CString)]
        public CString Name { get; set; }

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.CString)]
        public CString Type
        {
            get
            {
                Type type = TypeLibrary.GetType("ShaderValueType");
                string typeStr;
                if (type != null)
                {
                    typeStr = type.GetEnumName(textureType);
                    if (string.IsNullOrEmpty(typeStr))
                    {
                        typeStr = "INVALID";
                    }
                }
                else
                {
                    typeStr = "INVALID";
                }
                return typeStr;
            }
        }

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.UInt8)]
        public byte Index { get; set; }

        public byte textureType;
        public uint nameHash;
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class TextureFunction
    {
        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.CString)]
        public CString Name
        {
            get
            {
                Type type = TypeLibrary.GetType("ShaderConstantFunction");
                string funcStr;
                if (type != null)
                {
                    funcStr = type.GetEnumName(funcType);
                    if (!string.IsNullOrEmpty(funcStr))
                    {
                        funcStr = funcStr.Replace("ShaderConstantFunction_", "");
                        funcStr = char.ToLowerInvariant(funcStr[0]) + funcStr.Substring(1);
                    }
                    else
                    {
                        funcStr = "INVALID";
                    }
                }
                else
                {
                    funcStr = "INVALID";
                }
                return funcStr;
            }
        }

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.CString)]
        public CString Type
        {
            get
            {
                Type type = TypeLibrary.GetType("ShaderValueType");
                string funcStr;
                if (type != null)
                {
                    funcStr = type.GetEnumName(texType);
                    if (string.IsNullOrEmpty(funcStr))
                    {
                        funcStr = "INVALID";
                    }
                }
                else
                {
                    funcStr = "INVALID";
                }
                return funcStr;
            }
        }

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Index { get; set; }

        public uint funcType;
        public uint texType;
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class BufferFunction
    {
        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.CString)]
        public CString Name
        {
            get
            {
                Type type = TypeLibrary.GetType("ShaderConstantFunction");
                string funcStr;
                if (type != null)
                {
                    funcStr = type.GetEnumName(funcType);
                    if (!string.IsNullOrEmpty(funcStr))
                    {
                        funcStr = funcStr.Replace("ShaderConstantFunction_", "");
                        funcStr = char.ToLowerInvariant(funcStr[0]) + funcStr.Substring(1);
                    }
                    else
                    {
                        funcStr = "INVALID";
                    }
                }
                else
                {
                    funcStr = "INVALID";
                }
                return funcStr;
            }
        }

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.CString)]
        public CString Type
        {
            get
            {
                Type type = TypeLibrary.GetType("ShaderValueType");
                string funcStr;
                if (type != null)
                {
                    funcStr = type.GetEnumName(bufType);
                    if (string.IsNullOrEmpty(funcStr))
                    {
                        funcStr = "INVALID";
                    }
                }
                else
                {
                    funcStr = "INVALID";
                }
                return funcStr;
            }
        }

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Index { get; set; }

        public uint funcType;
        public uint bufType;
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class ShaderGraphPermutation
    {
        [IsReadOnly]
        [DisplayName("ConstantFunctions")]
        [Description("Contains engine constants passed into the shader.")]
        [EbxFieldMeta(EbxFieldType.Array, arrayType: EbxFieldType.Struct)]
        public List<ConstantFunction> ConstantFunctions { get; set; } = new List<ConstantFunction>();

        [IsReadOnly]
        [DisplayName("TextureFunctions")]
        [Description("Contains engine textures passed into the shader.")]
        [EbxFieldMeta(EbxFieldType.Array, arrayType: EbxFieldType.Struct)]
        public List<TextureFunction> TextureFunctions { get; set; } = new List<TextureFunction>();

        [IsReadOnly]
        [DisplayName("BufferFunctions")]
        [Description("Contains engine buffers passed into the shader.")]
        [EbxFieldMeta(EbxFieldType.Array, arrayType: EbxFieldType.Struct)]
        public List<BufferFunction> BufferFunctions { get; set; } = new List<BufferFunction>();

        [IsReadOnly]
        [DisplayName("ConstantValues")]
        [EbxFieldMeta(EbxFieldType.Array, arrayType: EbxFieldType.Inherited)]
        public List<dynamic> ValueConstants { get; set; } = new List<dynamic>();

        [IsReadOnly]
        [DisplayName("ConstantTextures")]
        [EbxFieldMeta(EbxFieldType.Array, arrayType: EbxFieldType.CString)]
        public List<ConstantTexture> TextureConstants { get; set; } = new List<ConstantTexture>();

        [IsReadOnly]
        [DisplayName("ValueParameters")]
        [Description("Contains default values, names, and other information for values that are passed into a shader externally.")]
        [EbxFieldMeta(EbxFieldType.Array, arrayType: EbxFieldType.Struct)]
        public List<ExternalValue> ExternalValueConstants { get; set; } = new List<ExternalValue>();

        [IsReadOnly]
        [DisplayName("TextureParameters")]
        [Description("Contains names of parameters for textures that are passed into a shader externally.")]
        [EbxFieldMeta(EbxFieldType.Array, arrayType: EbxFieldType.CString)]
        public List<CString> ExternalTextureConstants { get; set; } = new List<CString>();

        [IsReadOnly]
        [DisplayName("BufferParameters")]
        [Description("Contains names and other information for buffers that are passed into a shader externally.")]
        [EbxFieldMeta(EbxFieldType.Array, arrayType: EbxFieldType.Struct)]
        public List<ExternalBuffer> ExternalBufferConstants { get; set; } = new List<ExternalBuffer>();

        [IsReadOnly]
        [IsHidden]
        [EbxFieldMeta(EbxFieldType.Array, arrayType: EbxFieldType.Struct)]
        public List<SamplerState> SamplerStates { get; set; } = new List<SamplerState>();

        [IsReadOnly]
        [Description("Describes the vertex layout used for this shader permutation.")]
        [EbxFieldMeta(EbxFieldType.Array, arrayType: EbxFieldType.Struct)]
        public List<VertexElementBase> VertexElements { get; set; } = new List<VertexElementBase>();

        // won't be shown in the editor, contains data to look up the bytecode for this shader
        public ShaderDataPermutation shaderDataLookup = new ShaderDataPermutation();
    }
}
