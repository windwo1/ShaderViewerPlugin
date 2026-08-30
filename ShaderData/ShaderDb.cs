using Frosty.Controls;
using Frosty.Core;
using Frosty.Core.Controls;
using Frosty.Core.Windows;
using FrostySdk;
using FrostySdk.Attributes;
using FrostySdk.Ebx;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
#if FROSTY_107
using FrostySdk.Managers.Entries;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace MeshSetPlugin.ShaderData
{

    public enum ShaderType
    {
        PixelShader,
        VertexShader
    }

    public class ShaderDb
    {

        #region Properties
        public bool Loaded { get; private set; }
        private int numStreams
        {
            get
            {
                if (Version == (int)ShaderDBVersion.StarWarsBattlefront1)
                {
                    return 6;
                }
                else if (Version >= (int)ShaderDBVersion.StarWarsBattlefront2Alpha)
                {
                    if (Version == (int)ShaderDBVersion.MassEffectAndromeda
                    || Version == (int)ShaderDBVersion.Battlefield1
                    || Version == (int)ShaderDBVersion.BattlefieldV
                    || ProfilesLibrary.DataVersion == (int)ProfileVersion.MirrorsEdgeCatalyst)
                        return 8;
                    else
                        return 16;
                }
                else
                {
                    return 4;
                }
            }
        }
        #endregion

        #region Fields
        public static uint Version = 150;

        private AssetEntry assetEntry;
        private ILogger logger;

        // cached data so subsequent loads are faster
        private static Dictionary<uint, List<RenderPath>> shaderMap = new Dictionary<uint, List<RenderPath>>();
        private static Dictionary<uint, long> shaderMapOffsets = new Dictionary<uint, long>();

        private string mapCachePath;

        private uint dbSize = 0;
        static bool firstTimeLoad = true;
        #endregion

        public ShaderDb(ILogger inLogger)
        {
            logger = inLogger;
        }

        public ShaderDb(AssetEntry asset, ILogger inLogger)
        {
            assetEntry = asset;
            logger = inLogger ?? App.Logger;
        }

        /// <summary>
        /// Loads ShaderDb data into memory on the initial load. Subsequent loads pull from the built cache
        /// </summary>
        /// <param name="targetData">The data to be displayed in the property grid</param>
        /// <param name="task"></param>
        public void Load(ShaderGraphData targetData = null)
        {
            string baseCachePath = Path.Combine(Environment.CurrentDirectory, "Caches", "ShaderData");
            Directory.CreateDirectory(baseCachePath);

            mapCachePath = Path.Combine(baseCachePath, ProfilesLibrary.CacheName + "_shaders.cache");

            if (!firstTimeLoad)
            {
                targetData.Guid = ((EbxAssetEntry)assetEntry).Guid;
                targetData.Name = assetEntry.Name;
            }

            // on the first load, build a dictionary with all the necessary shader info
            // also load the texture hash cache for the games that need it
            if (firstTimeLoad)
            {
                if (!File.Exists(mapCachePath))
                {
                    IEnumerable<ResAssetEntry> shaderDbs = App.AssetManager.EnumerateRes((uint)Utils.HashString("IShaderDatabase", true));
                    int dbCount = shaderDbs.Count();
                    int progress = 0;
                    bool failed = false;
                    foreach (ResAssetEntry resEntry in shaderDbs)
                    {
                        logger.Log($"Loading database: {resEntry.Name}", (progress++ / (double)dbCount) * 100.0);
                        Read(resEntry);
                        if (!Loaded)
                        {
                            logger.Log($"A shader database has failed to load");
                            failed = true;
                            break;
                        }
                    }
                    firstTimeLoad = failed;

                    logger.Log("Writing Shader Cache");
                    WriteShaderMapCache();
                }
                else
                {
                    firstTimeLoad = false;
                }

                logger.Log("Loading Shader Database");
                ReadShaderMapCache();
            }
            else
            {
                uint shaderHash = (uint)Utils.HashString(assetEntry.Name, true);
                if (shaderMapOffsets.ContainsKey(shaderHash) && !firstTimeLoad)
                {
                    Loaded = true;
                    targetData.RenderPaths = GetShaderMapEntry(shaderHash);
                }
                else
                {
                    Loaded = false;
                    logger.Log($"Shader {assetEntry.Filename} not found in any database");
                }
            }
        }

        private List<RenderPath> GetShaderMapEntry(uint key)
        {
            long offset = shaderMapOffsets[key];
            List<RenderPath> renderPaths = new List<RenderPath>();
            using (var reader = new NativeReader(new FileStream(mapCachePath, FileMode.Open)))
            {
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                int renderPathCount = reader.ReadInt();
                
                for (int i = 0; i < renderPathCount; i++)
                {
                    string renderPathName = reader.ReadNullTerminatedString();

                    List<PermutationPair> pairs = new List<PermutationPair>();
                    int pairCount = reader.ReadInt();
                    for (int j = 0; j < pairCount; j++)
                    {
                        var pair = new PermutationPair
                        {
                            DoubleSided = reader.ReadBoolean(),
                            GeometryDeclarationHash = reader.ReadUInt(),
                            state = new ShaderSolutionState
                            {
                                surfaceShaderNameHash = reader.ReadUInt(),
                                vertexShaderFragmentNameHash = reader.ReadUInt(),
                                tessellationShaderFragmentNameHash = reader.ReadUInt(),
                                geometryDeclarationHash = reader.ReadUInt(),
                                stateInfo1 = reader.ReadBytes(reader.ReadInt()),
                                stateInfo2 = reader.ReadBytes(reader.ReadInt()),
                                skinningMethod = (ShaderSkinningMethod)reader.ReadInt(),
                                renderMode = (ShaderRenderMode)reader.ReadInt(),
                                unknown = reader.ReadByte(),
                                instancingMethod = (ShaderInstancingMethod)reader.ReadInt(),
                                tessellationEnable = reader.ReadBoolean()
                            },
                            ps = ReadPermutation(reader),
                            vs = ReadPermutation(reader),
                        };

                        pairs.Add(pair);
                    }

                    var path = new RenderPath
                    {
                        RenderPathName = renderPathName,
                        PermutationPairs = pairs
                    };
                    renderPaths.Add(path);
                }
            }

            return renderPaths;
        }

        #region Read Cache
        private void ReadShaderMapCache()
        {
            using (var reader = new NativeReader(new FileStream(mapCachePath, FileMode.Open)))
            {
                int count = reader.ReadInt();
                for (int i = 0; i < count; i++)
                {
                    uint key = reader.ReadUInt();
                    long offset = reader.ReadLong();
                    long position = reader.BaseStream.Position;

                    shaderMapOffsets.Add(key, position);
                    reader.BaseStream.Seek(offset, SeekOrigin.Current);
                }
            }
        }

        private ShaderGraphPermutation ReadPermutation(NativeReader reader)
        {
            var constFuncs = new List<ConstantFunction>();
            int constFuncsCount = reader.ReadInt();
            for (int i = 0; i < constFuncsCount; i++)
            {
                constFuncs.Add(new ConstantFunction
                {
                    Name = reader.ReadNullTerminatedString(),
                    CBufferIndex = reader.ReadUInt(),
                    ArraySize = reader.ReadUInt(),
                    MatrixDims = reader.ReadUInt(),
                    funcType = reader.ReadUInt()
                });
            }

            var texFuncs = new List<TextureFunction>();
            int texFuncsCount = reader.ReadInt();
            for (int i = 0; i < texFuncsCount; i++)
            {
                texFuncs.Add(new TextureFunction
                {
                    Name = reader.ReadNullTerminatedString(),
                    Type = reader.ReadNullTerminatedString(),
                    Index = reader.ReadUInt(),
                    funcType = reader.ReadUInt(),
                    texType = reader.ReadUInt()
                });
            }

            var bufferFuncs = new List<BufferFunction>();
            int bufferFuncsCount = reader.ReadInt();
            for (int i = 0; i < bufferFuncsCount; i++)
            {
                bufferFuncs.Add(new BufferFunction
                {
                    Name = reader.ReadNullTerminatedString(),
                    Type = reader.ReadNullTerminatedString(),
                    Index = reader.ReadUInt(),
                    funcType = reader.ReadUInt(),
                    bufType = reader.ReadUInt()
                });
            }

            var valueConsts = new List<dynamic>();
            int valueConstsCount = reader.ReadInt();
            for (int i = 0; i < valueConstsCount; i++)
            {
                valueConsts.Add(ReadDynamic(reader));
            }

            var texConsts = new List<ConstantTexture>();
            int texConstsCount = reader.ReadInt();
            for (int i = 0; i < texConstsCount; i++)
            {
                texConsts.Add(new ConstantTexture
                {
                    Name = reader.ReadNullTerminatedString(),
                    Type = reader.ReadNullTerminatedString(),
                    Index = reader.ReadByte(),
                    textureType = reader.ReadByte(),
                    nameHash = reader.ReadUInt()
                });
            }

            var externalValueConsts = new List<ExternalValue>();
            int externalValueConstsCount = reader.ReadInt();
            for (int i = 0; i < externalValueConstsCount; i++)
            {
                externalValueConsts.Add(new ExternalValue
                {
                    Name = reader.ReadNullTerminatedString(),
                    DefaultValue = ReadDynamic(reader),
                    Type = reader.ReadNullTerminatedString(),
                    Required = reader.ReadBoolean()
                });
            }

            var externalTexConsts = new List<CString>();
            int externalTexConstsCount = reader.ReadInt();
            for (int i = 0; i < externalTexConstsCount; i++)
            {
                externalTexConsts.Add(reader.ReadNullTerminatedString());
            }

            var externalBufferConsts = new List<ExternalBuffer>();
            int externalBufferConstsCount = reader.ReadInt();
            for (int i = 0; i < externalBufferConstsCount; i++)
            {
                externalBufferConsts.Add(new ExternalBuffer
                {
                    Name = reader.ReadNullTerminatedString(),
                    Type = reader.ReadNullTerminatedString(),
                    Required = reader.ReadBoolean()
                });
            }

            var samplerStates = new List<SamplerState>();
            int samplerStatesCount = reader.ReadInt();
            for (int i = 0; i < samplerStatesCount; i++)
            {
                uint index = reader.ReadUInt();
                var filter = (D3D11_FILTER)reader.ReadInt();
                var addressU = (D3D11_TEXTURE_ADDRESS_MODE)reader.ReadInt();
                var addressV = (D3D11_TEXTURE_ADDRESS_MODE)reader.ReadInt();
                var addressW = (D3D11_TEXTURE_ADDRESS_MODE)reader.ReadInt();
                float mipLODBias = reader.ReadFloat();
                uint maxAnisotropy = reader.ReadUInt();
                var comparisonFunc = (D3D11_COMPARISON_FUNC)reader.ReadInt();

                var borderColor = new List<float>();
                int borderColorCount = reader.ReadInt();
                for (int j = 0; j < borderColorCount; j++)
                {
                    borderColor.Add(reader.ReadFloat());
                }

                float minLOD = reader.ReadFloat();
                float maxLOD = reader.ReadFloat();
                ulong state = reader.ReadULong();

                samplerStates.Add(new SamplerState
                {
                    index = index,
                    desc = new D3D11_SAMPLER_DESC
                    {
                        Filter = filter,
                        AddressU = addressU,
                        AddressV = addressV,
                        AddressW = addressW,
                        MipLODBias = mipLODBias,
                        MaxAnisotropy = maxAnisotropy,
                        ComparisonFunc = comparisonFunc,
                        BorderColor = borderColor,
                        MinLOD = minLOD,
                        MaxLOD = maxLOD
                    },
                    state = state
                });
            }

            var vertexElements = new List<VertexElementBase>();
            int vertexElementsCount = reader.ReadInt();
            for (int i = 0; i < vertexElementsCount; i++)
            {
                string type = reader.ReadNullTerminatedString();
                if (type == "VertexElement_Old")
                {
                    vertexElements.Add(new VertexElement_Old
                    {
                        usage = reader.ReadByte(),
                        format = reader.ReadByte(),
                        offset = reader.ReadByte(),
                        streamIndex = reader.ReadByte(),
                        Stream = new VertexElement_Old.VertexStream
                        {
                            stride = reader.ReadByte(),
                            classification = reader.ReadByte()
                        }
                    });
                }
                else if (type == "VertexElement_New")
                {
                    vertexElements.Add(new VertexElement_New
                    {
                        format = reader.ReadByte(),
                        classification = reader.ReadByte(),
                        offset = reader.ReadByte(),
                        streamIndex = reader.ReadByte()
                    });
                }
            }

            var lookup = new ShaderDataPermutation
            {
                ShaderBytecodeGuid = reader.ReadGuid(),
                ShaderSize = reader.ReadUInt(),
                ConstantFunctionBlocksIndex = reader.ReadUInt(),
                TextureFunctionBlocksIndex = reader.ReadUInt(),
                BufferFunctionBlocksIndex = reader.ReadUInt(),
                DbOffset = reader.ReadLong(),
                DbPath = reader.ReadNullTerminatedString()
            };

            var permutation = new ShaderGraphPermutation
            {
                ConstantFunctions = constFuncs,
                TextureFunctions = texFuncs,
                BufferFunctions = bufferFuncs,
                ValueConstants = valueConsts,
                TextureConstants = texConsts,
                ExternalValueConstants = externalValueConsts,
                ExternalTextureConstants = externalTexConsts,
                ExternalBufferConstants = externalBufferConsts,
                SamplerStates = samplerStates,
                VertexElements = vertexElements,
                shaderDataLookup = lookup
            };

            return permutation;
        }

        private dynamic ReadDynamic(NativeReader reader)
        {
            string type = reader.ReadNullTerminatedString();

            switch (type)
            {
                case "bool": return reader.ReadBoolean();
                case "byte": return reader.ReadByte();
                case "sbyte": return reader.ReadSByte();
                case "double": return reader.ReadDouble();
                case "float": return reader.ReadFloat();
                case "int": return reader.ReadInt();
                case "uint": return reader.ReadUInt();
                case "long": return reader.ReadLong();
                case "ulong": return reader.ReadULong();
                case "short": return reader.ReadShort();
                case "ushort": return reader.ReadUShort();
                case "string": return reader.ReadNullTerminatedString();

                case "CString": return reader.ReadNullTerminatedString();
                case "Guid": return reader.ReadGuid();

                case "FrostySdk.Ebx.Vec4":
                    dynamic vec4 = TypeLibrary.CreateObject("Vec4");
                    vec4.x = reader.ReadFloat();
                    vec4.y = reader.ReadFloat();
                    vec4.z = reader.ReadFloat();
                    vec4.w = reader.ReadFloat();
                    return vec4;

                default:
                    throw new NotSupportedException("Can't read dynamic type " + type);
            }
        }
        #endregion

        #region Write Cache
        private void WriteShaderMapCache()
        {
            using (var writer = new NativeWriter(File.Open(mapCachePath, FileMode.Create)))
            {
                writer.Write(shaderMap.Count);
                foreach (var kvp in shaderMap)
                {
                    writer.Write(kvp.Key);

                    long sizePos = writer.BaseStream.Position;
                    writer.Write(0L);
                    long dataStart = writer.BaseStream.Position;

                    writer.Write(kvp.Value.Count);
                    foreach (var renderPath in kvp.Value)
                    {
                        writer.WriteNullTerminatedString(renderPath.RenderPathName);
                        writer.Write(renderPath.PermutationPairs.Count);
                        foreach (var pair in renderPath.PermutationPairs)
                        {
                            writer.Write(pair.DoubleSided);
                            writer.Write(pair.GeometryDeclarationHash);

                            var state = pair.SolutionState;
                            writer.Write(state.surfaceShaderNameHash);
                            writer.Write(state.vertexShaderFragmentNameHash);
                            writer.Write(state.tessellationShaderFragmentNameHash);
                            writer.Write(state.geometryDeclarationHash);
                            writer.Write(state.stateInfo1.Length);
                            writer.Write(state.stateInfo1);
                            writer.Write(state.stateInfo2.Length);
                            writer.Write(state.stateInfo2);
                            writer.Write((int)state.skinningMethod);
                            writer.Write((int)state.renderMode);
                            writer.Write(state.unknown);
                            writer.Write((int)state.instancingMethod);
                            writer.Write(state.tessellationEnable);

                            WritePermutation(writer, pair.PixelShader);
                            WritePermutation(writer, pair.VertexShader);
                        }
                    }

                    long dataEnd = writer.BaseStream.Position;
                    writer.BaseStream.Position = sizePos;
                    writer.Write(dataEnd - dataStart);
                    writer.BaseStream.Position = dataEnd;
                }
            }
        }

        private void WritePermutation(NativeWriter writer, ShaderGraphPermutation permutation)
        {
            // ConstantFunctions (List<ConstantFunction>)
            writer.Write(permutation.ConstantFunctions.Count);
            foreach (var constFunc in permutation.ConstantFunctions)
            {
                writer.WriteNullTerminatedString(constFunc.Name);
                writer.Write(constFunc.CBufferIndex);
                writer.Write(constFunc.ArraySize);
                writer.Write(constFunc.MatrixDims);
                writer.Write(constFunc.funcType);
            }

            // TextureFunctions (List<TextureFunction>)
            writer.Write(permutation.TextureFunctions.Count);
            foreach (var texFunc in permutation.TextureFunctions)
            {
                writer.WriteNullTerminatedString(texFunc.Name);
                writer.WriteNullTerminatedString(texFunc.Type);
                writer.Write(texFunc.Index);
                writer.Write(texFunc.funcType);
                writer.Write(texFunc.texType);
            }

            // BufferFunctions (List<BufferFunction>)
            writer.Write(permutation.BufferFunctions.Count);
            foreach (var bufferFunc in permutation.BufferFunctions)
            {
                writer.WriteNullTerminatedString(bufferFunc.Name);
                writer.WriteNullTerminatedString(bufferFunc.Type);
                writer.Write(bufferFunc.Index);
                writer.Write(bufferFunc.funcType);
                writer.Write(bufferFunc.bufType);
            }

            // ValueConstants (List<dynamic>)
            writer.Write(permutation.ValueConstants.Count);
            foreach (var valueConst in permutation.ValueConstants)
            {
                WriteDynamic(writer, valueConst);
            }

            // TextureConstants (List<ConstantTexture>)
            writer.Write(permutation.TextureConstants.Count);
            foreach (var texConst in permutation.TextureConstants)
            {
                writer.WriteNullTerminatedString(texConst.Name);
                writer.WriteNullTerminatedString(texConst.Type);
                writer.Write(texConst.Index);
                writer.Write(texConst.textureType);
                writer.Write(texConst.nameHash);
            }

            // ExternalValueConstants (List<ExternalValue>)
            writer.Write(permutation.ExternalValueConstants.Count);
            foreach (var externalValueConst in permutation.ExternalValueConstants)
            {
                writer.WriteNullTerminatedString(externalValueConst.Name);
                WriteDynamic(writer, externalValueConst.DefaultValue);
                writer.WriteNullTerminatedString(externalValueConst.Type);
                writer.Write(externalValueConst.Required);
            }

            // ExternalTextureConstants (List<CString>)
            writer.Write(permutation.ExternalTextureConstants.Count);
            foreach (var externalTexConst in permutation.ExternalTextureConstants)
            {
                writer.WriteNullTerminatedString(externalTexConst);
            }

            // ExternalBufferConstants (List<ExternalBuffer>)
            writer.Write(permutation.ExternalBufferConstants.Count);
            foreach (var externalBufferConst in permutation.ExternalBufferConstants)
            {
                writer.WriteNullTerminatedString(externalBufferConst.Name);
                writer.WriteNullTerminatedString(externalBufferConst.Type);
                writer.Write(externalBufferConst.Required);
            }

            // SamplerStates (List<SamplerState>)
            writer.Write(permutation.SamplerStates.Count);
            foreach (var samplerState in permutation.SamplerStates)
            {
                writer.Write(samplerState.index);

                var desc = samplerState.desc;
                writer.Write((int)desc.Filter);
                writer.Write((int)desc.AddressU);
                writer.Write((int)desc.AddressV);
                writer.Write((int)desc.AddressW);
                writer.Write(desc.MipLODBias);
                writer.Write(desc.MaxAnisotropy);
                writer.Write((int)desc.ComparisonFunc);
                writer.Write(desc.BorderColor.Count);
                foreach (float value3 in desc.BorderColor)
                {
                    writer.Write(value3);
                }
                writer.Write(desc.MinLOD);
                writer.Write(desc.MaxLOD);

                writer.Write(samplerState.state);
            }

            // VertexElements (List<VertexElementBase>)
            writer.Write(permutation.VertexElements.Count);
            foreach (var vertexElement in permutation.VertexElements)
            {
                writer.WriteNullTerminatedString(vertexElement.GetType().Name);

                if (vertexElement is VertexElement_Old oldElement)
                {
                    writer.Write(oldElement.usage);
                    writer.Write(oldElement.format);
                    writer.Write(oldElement.offset);
                    writer.Write(oldElement.streamIndex);
                    writer.Write(oldElement.Stream.stride);
                    writer.Write(oldElement.Stream.classification);
                }
                else if (vertexElement is VertexElement_New newElement)
                {
                    writer.Write(newElement.format);
                    writer.Write(newElement.classification);
                    writer.Write(newElement.offset);
                    writer.Write(newElement.streamIndex);
                }
            }

            // shaderDataLookup (ShaderDataPermutation)
            var lookup = permutation.shaderDataLookup;
            writer.Write(lookup.ShaderBytecodeGuid.ToByteArray());
            writer.Write(lookup.ShaderSize);
            writer.Write(lookup.ConstantFunctionBlocksIndex);
            writer.Write(lookup.TextureFunctionBlocksIndex);
            writer.Write(lookup.BufferFunctionBlocksIndex);
            writer.Write(lookup.DbOffset);
            writer.WriteNullTerminatedString(lookup.DbPath);
        }

        private void WriteDynamic(NativeWriter writer, dynamic value)
        {
            switch (value)
            {
                case bool boolVal: writer.Write("bool"); writer.Write(boolVal); break;
                case byte byteVal: writer.Write("byte"); writer.Write(byteVal); break;
                case sbyte sbyteVal: writer.Write("sbyte"); writer.Write(sbyteVal); break;
                case double doubleVal: writer.Write("double"); writer.Write(doubleVal); break;
                case float floatVal: writer.Write("float"); writer.Write(floatVal); break;
                case int intVal: writer.Write("int"); writer.Write(intVal); break;
                case uint uintVal: writer.Write("uint"); writer.Write(uintVal); break;
                case long longVal: writer.Write("long"); writer.Write(longVal); break;
                case ulong ulongVal: writer.Write("ulong"); writer.Write(ulongVal); break;
                case short shortVal: writer.Write("short"); writer.Write(shortVal); break;
                case ushort ushortVal: writer.Write("ushort"); writer.Write(ushortVal); break;
                case string stringVal: writer.Write("string"); writer.Write(stringVal); break;

                default:
                    if (value is CString cstring)
                    {
                        writer.Write("CString");
                        writer.WriteNullTerminatedString(cstring);
                    }
                    else if (value is Guid guid)
                    {
                        writer.Write("Guid");
                        writer.Write(guid.ToByteArray());
                    }
                    else if (value.GetType().ToString() == "FrostySdk.Ebx.Vec4")
                    {
                        writer.WriteNullTerminatedString("FrostySdk.Ebx.Vec4");
                        writer.Write(value.x);
                        writer.Write(value.y);
                        writer.Write(value.z);
                        writer.Write(value.w);
                    }
                    else
                    {
                        throw new NotSupportedException("Can't write dynamic type " + value.GetType());
                    }

                    break;
            }
        }
        #endregion

        private void Read(ResAssetEntry db)
        {
            Stream resStream = App.AssetManager.GetRes(db);
            using (NativeReader reader = new NativeReader(resStream))
            {
                uint numRenderPaths;

                // Anthem uses the res meta as the database header
                // and this is just the beginning of many Anthem exclusive changes...
                if (ProfilesLibrary.DataVersion == (int)ProfileVersion.Anthem)
                {
                    using (NativeReader metaReader = new NativeReader(new MemoryStream(db.ResMeta)))
                    {
                        numRenderPaths = metaReader.ReadUInt();
                        metaReader.ReadUInt();
                        dbSize = metaReader.ReadUInt();
                        Version = metaReader.ReadUInt();
                    }
                }
                else
                {
                    numRenderPaths = reader.ReadUInt();
                }

                for (int pathIt = 0; pathIt < numRenderPaths; ++pathIt)
                {
                    // get render path header data
                    uint renderPath = reader.ReadUInt();
                    if (ProfilesLibrary.DataVersion == (int)ProfileVersion.Anthem)
                    {
                        // quality level
                        reader.ReadUInt();
                    }
                    else
                    {
                        dbSize = reader.ReadUInt();
                        Version = reader.ReadUInt();
                        // path
                        reader.ReadUInt();
                        // quality level
                        reader.ReadUInt();
                        // BFV unknown byte
                        if (Version == (int)ShaderDBVersion.BattlefieldV)
                            reader.ReadByte();
                        // Unbound unknown uint
                        if (Version == (int)ShaderDBVersion.NFSUnbound)
                            reader.ReadUInt(); // seems to always be 1
                    }

                    switch ((ShaderDBVersion)Version)
                    {
                        case ShaderDBVersion.Battlefield4:
                        case ShaderDBVersion.DragonAgeInquisition:
                        case ShaderDBVersion.PvZGardenWarfare1:
                        case ShaderDBVersion.NFSRivals:
                        case ShaderDBVersion.NFS2015_PvZGardenWarfare2:
                        case ShaderDBVersion.StarWarsBattlefront1:
                        case ShaderDBVersion.StarWarsBattlefront2Alpha:
                        case ShaderDBVersion.NFSPayback_MECatalyst:
                        case ShaderDBVersion.MassEffectAndromeda:
                        case ShaderDBVersion.Battlefield1:
                        case ShaderDBVersion.BattlefieldV:
                        case ShaderDBVersion.Anthem:
                        case ShaderDBVersion.PvZBattleForNeighborville:
                        case ShaderDBVersion.NFSHeat:
                        case ShaderDBVersion.NFSUnbound:
                            break;
                        default:
                            Loaded = false;
                            logger.Log($"Shader database version {Version} is not supported at this time");
                            return;
                    }

                    // shader constants size
                    if (Version == (int)ShaderDBVersion.Anthem || Version == (int)ShaderDBVersion.NFSUnbound)
                        reader.ReadUInt();
                    // get constants for this database
                    uint shaderConstantsCount = reader.ReadUInt();
                    List<GenericShaderConstants> shaderConstants = new List<GenericShaderConstants>();
                    for (int i = 0; i < shaderConstantsCount; ++i)
                        shaderConstants.Add(new GenericShaderConstants(reader));

                    // constant function blocks size
                    if (Version == (int)ShaderDBVersion.Anthem || Version == (int)ShaderDBVersion.NFSUnbound)
                        reader.ReadUInt();
                    uint constantFunctionBlocksCount = reader.ReadUInt();
                    List<ConstantFunctionBlock> constantFunctionBlocks = new List<ConstantFunctionBlock>();
                    // get constant function blocks
                    for (int i = 0; i < constantFunctionBlocksCount; ++i)
                        constantFunctionBlocks.Add(new ConstantFunctionBlock(reader));

                    // texture function blocks size
                    if (Version == (int)ShaderDBVersion.Anthem || Version == (int)ShaderDBVersion.NFSUnbound)
                        reader.ReadUInt();
                    uint textureFunctionBlocksCount = reader.ReadUInt();
                    List<TextureFunctionBlock> textureFunctionBlocks = new List<TextureFunctionBlock>();
                    // get texture function blocks
                    for (int i = 0; i < textureFunctionBlocksCount; ++i)
                        textureFunctionBlocks.Add(new TextureFunctionBlock(reader));

                    List<BufferFunctionBlock> bufferFunctionBlocks = new List<BufferFunctionBlock>();
                    // FB2013 games (BF4, PvZ GW1, NFS Rivals, etc.) don't have buffer function blocks
                    if (Version > (int)ShaderDBVersion.NFSRivals)
                    {
                        // buffer function blocks size
                        if (Version == (int)ShaderDBVersion.Anthem || Version == (int)ShaderDBVersion.NFSUnbound)
                            reader.ReadUInt();
                        uint bufferFunctionBlocksCount = reader.ReadUInt();
                        // get buffer function blocks
                        for (int i = 0; i < bufferFunctionBlocksCount; ++i)
                            bufferFunctionBlocks.Add(new BufferFunctionBlock(reader));
                    }


                    //
                    // read all shader permutations
                    // Anthem has an extra array of structs after each permutation array
                    //

                    // get vertex shader permutations
                    uint vertexShaderPermutationCount = reader.ReadUInt();
                    List<ShaderDataPermutation> vsPermutations = new List<ShaderDataPermutation>();
                    for (int i = 0; i < vertexShaderPermutationCount; ++i)
                        vsPermutations.Add(new VertexShaderPermutation(reader, db.Name));

                    if (Version == (int)ShaderDBVersion.Anthem)
                    {
                        uint count = reader.ReadUInt();
                        for (int i = 0; i < count; ++i)
                            new AnthemUnkStruct(reader);
                    }

                    // get pixel shader permutations
                    uint pixelShaderPermutationCount = reader.ReadUInt();
                    List<ShaderDataPermutation> psPermutations = new List<ShaderDataPermutation>();
                    for (int i = 0; i < pixelShaderPermutationCount; ++i)
                        psPermutations.Add(new PixelShaderPermutation(reader, db.Name));

                    if (Version == (int)ShaderDBVersion.Anthem)
                    {
                        uint count = reader.ReadUInt();
                        for (int i = 0; i < count; ++i)
                            new AnthemUnkStruct(reader);
                    }

                    // read geometry shader permutations
                    uint geometryShaderPermutationCount = reader.ReadUInt();
                    for (int i = 0; i < geometryShaderPermutationCount; ++i)
                        new GeometryShaderPermutation(reader);

                    if (Version == (int)ShaderDBVersion.Anthem)
                    {
                        uint count = reader.ReadUInt();
                        for (int i = 0; i < count; ++i)
                            new AnthemUnkStruct(reader);
                    }

                    // read hull shader permutations
                    uint hullShaderPermutationCount = reader.ReadUInt();
                    for (int i = 0; i < hullShaderPermutationCount; ++i)
                        new HullShaderPermutation(reader, db.Name);

                    if (Version == (int)ShaderDBVersion.Anthem)
                    {
                        uint count = reader.ReadUInt();
                        for (int i = 0; i < count; ++i)
                            new AnthemUnkStruct(reader);
                    }

                    // get domain shader permutations
                    uint domainShaderPermutationCount = reader.ReadUInt();
                    for (int i = 0; i < domainShaderPermutationCount; ++i)
                        new DomainShaderPermutation(reader, db.Name);

                    if (Version == (int)ShaderDBVersion.Anthem)
                    {
                        uint count = reader.ReadUInt();
                        for (int i = 0; i < count; ++i)
                            new AnthemUnkStruct(reader);

                        count = reader.ReadUInt();
                        for (int i = 0; i < count; ++i)
                            new AnthemUnkStruct1(reader);
                    }

                    // BFV has 3 new arrays of shader permutations, not sure what they're for
                    if (Version == (int)ShaderDBVersion.BattlefieldV)
                    {
                        uint count = reader.ReadUInt();
                        for (int i = 0; i < count; ++i)
                            new BFVUnkShaderPermutation(reader, db.Name);

                        // these next 2 permutation arrays contain DXIL data
                        count = reader.ReadUInt();
                        for (int i = 0; i < count; ++i)
                            new BFVUnkShaderPermutation1(reader, db.Name);

                        count = reader.ReadUInt();
                        for (int i = 0; i < count; ++i)
                            new BFVUnkShaderPermutation1(reader, db.Name);
                    }

                    // Unbound unknown 12 bytes
                    if (Version == (int)ShaderDBVersion.NFSUnbound)
                    {
                        reader.Position += 12;
                    }

                    // get shader solutions
                    uint solutionCount = reader.ReadUInt();
                    List<ShaderSolution> solutions = new List<ShaderSolution>();
                    for (int i = 0; i < solutionCount; ++i)
                        solutions.Add(new ShaderSolution(reader));

                    // get shader state solutions
                    uint stateCount = reader.ReadUInt();
                    List<uint> geomDeclLookupList = new List<uint>();
                    List<ShaderSolutionState> states = new List<ShaderSolutionState>();
                    for (int i = 0; i < stateCount; ++i)
                    {
                        ShaderSolutionState state = new ShaderSolutionState(reader);
                        states.Add(state);

                        // since newer games don't look up vertex elements with this hash, we can skip adding them to the list
                        if (Version < (int)ShaderDBVersion.Anthem)
                            geomDeclLookupList.Add(state.geometryDeclarationHash);
                    }

                    uint geometryDeclarationsCount = reader.ReadUInt();
                    Dictionary<uint, List<VertexElementBase>> geomDecls = new Dictionary<uint, List<VertexElementBase>>();
                    // get geometry declarations
                    for (int i = 0; i < geometryDeclarationsCount; ++i)
                    {
                        List<VertexElementBase> elements = new List<VertexElementBase>();

                        if (Version == (int)ShaderDBVersion.StarWarsSquadrons || Version < (int)ShaderDBVersion.Anthem)
                        {
                            uint elementsHash = reader.ReadUInt();

                            // skip over the elements and streams and store the offset before both so we can grab their counts and return to them later
                            long elementsOffset = reader.Position;
                            reader.ReadBytes(64); // struct is 4 bytes, and the array has 16 elements
                            long streamsOffset = reader.Position;
                            reader.ReadBytes(2 * numStreams); // 2 byte struct

                            byte elementCount = reader.ReadByte();
                            byte streamCount = reader.ReadByte();
                            // unknown
                            reader.ReadUShort();
                            long endOffset = reader.Position;

                            // read streams first so we can look them up in a list when reading the elements
                            reader.Position = streamsOffset;
                            List<VertexElement_Old.VertexStream> streams = new List<VertexElement_Old.VertexStream>();
                            // streamCount is unreliable because for some reason it doesn't always give the true number of streams
                            // so instead we can fall back to NumStreams and discard any null streams (when stride and classification are zero)
                            for (int streamIt = 0; streamIt < numStreams; ++streamIt)
                            {
                                VertexElement_Old.VertexStream stream = new VertexElement_Old.VertexStream();
                                stream.stride = reader.ReadByte();
                                stream.classification = reader.ReadByte();
                                // the vertex stride can never be zero or else the vertex stream wouldn't be able to advance, so we can use this to discard null streams
                                if (stream.stride != 0)
                                    streams.Add(stream);
                            }

                            reader.Position = elementsOffset;
                            for (int elemIt = 0; elemIt < elementCount; ++elemIt)
                            {
                                VertexElement_Old element = new VertexElement_Old();
                                element.usage = reader.ReadByte();
                                element.format = reader.ReadByte();
                                element.offset = reader.ReadByte();
                                element.streamIndex = reader.ReadByte();
                                element.Stream = streams[element.streamIndex];
                                elements.Add(element);
                            }

                            // return to the end position
                            reader.Position = endOffset;
                            geomDecls.Add(elementsHash, elements);
                        }
                        else
                        {
                            // newer games don't have a separate streams array and the vertex element struct is slightly different
                            // there also isn't an element hash so the vertex element lookup is handled differently

                            uint elementCount = reader.ReadUInt();
                            for (int elemIt = 0; elemIt < elementCount; ++elemIt)
                            {
                                VertexElement_New element = new VertexElement_New();
                                element.format = reader.ReadByte();
                                element.classification = reader.ReadByte();
                                element.offset = reader.ReadByte();
                                element.streamIndex = reader.ReadByte();
                                elements.Add(element);
                            }

                            // skip the rest of the elements since they're empty and don't need to be stored
                            reader.ReadBytes((int)(64 - (elementCount * 4)));
                            // just do it like this so we don't have to use a separate list
                            geomDecls.Add((uint)i, elements);
                        }

                    }

                    // database versions >229 all have a list of indices that map to the geometry declaration that a shader uses
                    if (Version > (int)ShaderDBVersion.BattlefieldV)
                    {
                        uint count = reader.ReadUInt();
                        for (int i = 0; i < count; ++i)
                            geomDeclLookupList.Add(reader.ReadUInt());
                    }

                    if (Version == (int)ShaderDBVersion.NFSUnbound)
                    {
                        // unknown data
                        reader.ReadUInt();
                    }

                    uint shaderCount = reader.ReadUInt();
                    Dictionary<uint, List<uint>> surfaceShaderSolutionMap = new Dictionary<uint, List<uint>>();
                    // get shader map
                    for (int i = 0; i < shaderCount; ++i)
                    {
                        uint key = reader.ReadUInt();

                        // shaderType, maps to SurfaceShaderType
                        if (Version != (int)ShaderDBVersion.MassEffectAndromeda && Version != (int)ShaderDBVersion.BattlefieldV)
                            reader.ReadUInt();
                        else
                            reader.ReadByte();
                        // unknown data
                        switch ((ShaderDBVersion)Version)
                        {
                            case ShaderDBVersion.Battlefield4:
                            case ShaderDBVersion.DragonAgeInquisition:
                            case ShaderDBVersion.PvZGardenWarfare1:
                            case ShaderDBVersion.NFSRivals:
                            case ShaderDBVersion.NFS2015_PvZGardenWarfare2:
                            case ShaderDBVersion.StarWarsBattlefront1:
                            case ShaderDBVersion.NFSPayback_MECatalyst:
                            case ShaderDBVersion.Battlefield1:
                                // Certain games have 4 more bytes in whatever this data is
                                if (ProfilesLibrary.DataVersion == (int)ProfileVersion.NeedForSpeed || Version == (int)ShaderDBVersion.StarWarsBattlefront1)
                                    reader.ReadBytes(40);
                                else
                                    reader.ReadBytes(36);
                                break;
                            case ShaderDBVersion.BattlefieldV:
                                reader.ReadBytes(39);
                                break;
                            case ShaderDBVersion.StarWarsBattlefront2Alpha:
                            case ShaderDBVersion.MassEffectAndromeda:
                            case ShaderDBVersion.Anthem:
                            case ShaderDBVersion.StarWarsSquadrons:
                            case ShaderDBVersion.PvZBattleForNeighborville:
                            case ShaderDBVersion.NFSHeat:
                                reader.ReadBytes(104);
                                break;
                            case ShaderDBVersion.NFSUnbound:
                                reader.ReadBytes(284);
                                break;
                            default:
                                break;
                        }


                        int count = reader.ReadInt();
                        // streamableTextures
                        switch ((ShaderDBVersion)Version)
                        {
                            case ShaderDBVersion.DragonAgeInquisition:
                            case ShaderDBVersion.MassEffectAndromeda:
                                // - (4 bytes) nameHash, uint
                                // - (4 bytes) coordType, maps to ShaderTextureCoordType
                                // - (4 bytes) vertexUsage, maps to VertexElementUsage
                                // - (4 bytes) textureTilesPerCoord, float
                                // - (1 byte)  unknown
                                reader.ReadBytes(count * 17);
                                break;
                            default:
                                // - (4 bytes) nameHash, uint
                                // - (4 bytes) coordType, maps to ShaderTextureCoordType
                                // - (4 bytes) vertexUsage, maps to VertexElementUsage
                                // - (4 bytes) textureTilesPerCoord, float
                                reader.ReadBytes(count * 16);
                                break;
                        }

                        count = reader.ReadInt();
                        // streamableExternalTextures
                        switch ((ShaderDBVersion)Version)
                        {
                            case ShaderDBVersion.DragonAgeInquisition:
                            case ShaderDBVersion.MassEffectAndromeda:
                                // - (4 bytes) nameHash, uint
                                // - (4 bytes) textureHandle, uint
                                // - (4 bytes) coordType, maps to ShaderTextureCoordType
                                // - (4 bytes) vertexUsage, maps to VertexElementUsage
                                // - (4 bytes) textureTilesPerCoord, float
                                // - (1 byte)  unknown
                                reader.ReadBytes(count * 21);
                                break;
                            case ShaderDBVersion.NFSUnbound:
                                // - (4 bytes) nameHash, uint
                                // - (4 bytes) textureHandle, uint
                                // - (4 bytes) unknown
                                // - (4 bytes) unknown
                                // - (4 bytes) coordType, maps to ShaderTextureCoordType
                                // - (4 bytes) vertexUsage, maps to VertexElementUsage
                                // - (4 bytes) textureTilesPerCoord, float
                                reader.ReadBytes(count * 28);
                                break;
                            default:
                                // - (4 bytes) nameHash, uint
                                // - (4 bytes) textureHandle, uint
                                // - (4 bytes) coordType, maps to ShaderTextureCoordType
                                // - (4 bytes) vertexUsage, maps to VertexElementUsage
                                // - (4 bytes) textureTilesPerCoord, float
                                reader.ReadBytes(count * 20);
                                break;
                        }

                        // unknown data, seems to always be zero
                        if (Version == (int)ShaderDBVersion.NFSUnbound)
                            reader.ReadUInt();

                        count = reader.ReadInt();
                        List<uint> solutionIndices = new List<uint>();
                        for (int j = 0; j < count; ++j)
                        {
                            // FB2013 games store these indices as uint16 values
                            if (Version <= (int)ShaderDBVersion.NFSRivals)
                                solutionIndices.Add(reader.ReadUShort());
                            else
                            {
                                try
                                {
                                    solutionIndices.Add(reader.ReadUInt());
                                }
                                catch
                                {
                                    throw new Exception($"Encountered error in database: {db.Name}");
                                }
                            }
                        }
                        surfaceShaderSolutionMap.Add(key, solutionIndices);
                    }

                    try
                    {
                        foreach (uint key in surfaceShaderSolutionMap.Keys)
                        {
                            List<uint> indices = surfaceShaderSolutionMap[key];

                            if (!shaderMap.ContainsKey(key))
                            {
                                List<RenderPath> paths = new List<RenderPath>();
                                shaderMap.Add(key, paths);
                            }
                            string pathName = TypeLibrary.GetType("ShaderRenderPath").GetEnumName(renderPath);

                            List<PermutationPair> permutationPairs;

                            var match = shaderMap[key].Find(x => x.RenderPathName == pathName);
                            if (match != null)
                            {
                                permutationPairs = match.PermutationPairs;
                            }
                            else
                            {
                                RenderPath path = new RenderPath() { RenderPathName = pathName };
                                shaderMap[key].Add(path);
                                permutationPairs = path.PermutationPairs;
                            }

                            // fill in shader data
                            for (int i = 0; i < indices.Count; ++i)
                            {
                                int solutionIndex = (int)indices[i];
                                PermutationPair pair = new PermutationPair();
                                pair.ps = new ShaderGraphPermutation();
                                pair.vs = new ShaderGraphPermutation();
                                pair.state = states[solutionIndex]; // @todo: create a new class for this
                                List<VertexElementBase> elems;
                                uint geomDeclHash;
                                if (Version > (int)ShaderDBVersion.BattlefieldV)
                                {
                                    geomDeclHash = geomDeclLookupList[(int)solutions[solutionIndex].vertexPermutationIndex];
                                    elems = geomDecls[geomDeclHash];
                                }
                                else
                                {
                                    geomDeclHash = geomDeclLookupList[solutionIndex];
                                    elems = geomDecls[geomDeclHash];
                                }

                                pair.GeometryDeclarationHash = geomDeclHash;
                                pair.DoubleSided = (solutions[solutionIndex].renderFlags & 0x01) != 0;
                                pair.PixelShader.VertexElements = elems;
                                int pixelPermutationIdx = (int)solutions[solutionIndex].pixelPermutationIndex;
                                if (pixelPermutationIdx > 0)
                                {
                                    pair.ps.shaderDataLookup = psPermutations[pixelPermutationIdx];
                                    GetShaderPermutation(shaderConstants[(int)solutions[solutionIndex].pixelConstantsIndex],
                                        constantFunctionBlocks.Count > 0 ? constantFunctionBlocks[(int)pair.ps.shaderDataLookup.ConstantFunctionBlocksIndex] : null,
                                        textureFunctionBlocks.Count > 0 ? textureFunctionBlocks[(int)pair.ps.shaderDataLookup.TextureFunctionBlocksIndex] : null,
                                        bufferFunctionBlocks.Count > 0 ? bufferFunctionBlocks[(int)pair.ps.shaderDataLookup.BufferFunctionBlocksIndex] : null,
                                        ref pair.ps);
                                }

                                pair.VertexShader.VertexElements = elems;

                                if (solutions[solutionIndex].vertexPermutationIndex != ulong.MaxValue)
                                {
                                    pair.vs.shaderDataLookup = vsPermutations[(int)solutions[solutionIndex].vertexPermutationIndex];
                                    GetShaderPermutation(shaderConstants[(int)solutions[solutionIndex].vertexConstantsIndex],
                                        constantFunctionBlocks.Count > 0 ? constantFunctionBlocks[(int)pair.vs.shaderDataLookup.ConstantFunctionBlocksIndex] : null,
                                        textureFunctionBlocks.Count > 0 ? textureFunctionBlocks[(int)pair.vs.shaderDataLookup.TextureFunctionBlocksIndex] : null,
                                        bufferFunctionBlocks.Count > 0 ? bufferFunctionBlocks[(int)pair.vs.shaderDataLookup.BufferFunctionBlocksIndex] : null,
                                        ref pair.vs);
                                }

                                permutationPairs.Add(pair);
                            }
                        }

                        Loaded = true;
                    }
                    catch (Exception ex)
                    {
                        Loaded = false;
                        logger.Log($"Encountered error building shader map in database: {db.Name}" +
                            $"\nMessage ---\n{ex.Message}" +
                            $"\nStackTrace ---\n{ex.StackTrace}" +
                            $"\nTargetSite ---\n{ex.TargetSite}");
                    }
                }
            }
        }

        private void GetShaderPermutation(GenericShaderConstants constants,
            ConstantFunctionBlock constantFunctionBlock,
            TextureFunctionBlock textureFunctionBlock,
            BufferFunctionBlock bufferFunctionBlock,
            ref ShaderGraphPermutation permutation)
        {
            if (constantFunctionBlock != null)
            {
                foreach (ConstantFunctionBlock.Constant cfunc in constantFunctionBlock.Constants)
                {
                    permutation.ConstantFunctions.Add
                    (
                        new ConstantFunction
                        {
                            funcType = cfunc.constFunction,
                            CBufferIndex = cfunc.index,
                            ArraySize = cfunc.arraySize,
                            MatrixDims = cfunc.matrixDims
                        }
                    );
                }
            }

            if (textureFunctionBlock != null)
            {
                foreach (TextureFunctionBlock.Texture tfunc in textureFunctionBlock.Textures)
                {
                    permutation.TextureFunctions.Add
                    (
                        new TextureFunction
                        {
                            funcType = tfunc.constFunction,
                            texType = tfunc.valueType,
                            Index = tfunc.index
                        }
                    );
                }
            }

            if (bufferFunctionBlock != null)
            {
                foreach (BufferFunctionBlock.Buffer bfunc in bufferFunctionBlock.Buffers)
                {
                    permutation.BufferFunctions.Add
                    (
                        new BufferFunction
                        {
                            funcType = bfunc.constFunction,
                            bufType = bfunc.valueType,
                            Index = bfunc.index
                        }
                    );
                }
            }

            permutation.ValueConstants = constants.valueConstants;
            foreach (TextureConstant tex in constants.textureConstants)
            {
#if false
                if (!textureHashMap.ContainsKey(tex.nameHash))
                {
                    // I've only seen this happen with Unbound
                    // probably some debug texture that doesn't exist in retail builds
                    permutation.TextureConstants.Add($"Unknown texture: 0x{tex.nameHash:X8}");
                }
                else
                {
                    permutation.TextureConstants.Add(tex.name);
                }
#endif
                permutation.TextureConstants.Add
                (
                    new ConstantTexture
                    {
                        Name = tex.name,
                        Index = tex.index,
                        nameHash = tex.nameHash
                    }
                );
            }
            foreach (ExternalValueConstant val in constants.externalValueConstants)
            {
                Type extType = TypeLibrary.GetType("ExternalValueConstantType");
                permutation.ExternalValueConstants.Add
                (
                    new ExternalValue
                    {
                        Name = val.name,
                        Type = extType == null ? "ExternalValueConstantType_Vec" : extType.GetEnumName(val.type),
                        Required = Convert.ToBoolean(val.required),
                        DefaultValue = val.defaultValue
                    }
                );
            }
            foreach (ExternalTextureConstant tex in constants.externalTextureConstants)
            {
                permutation.ExternalTextureConstants.Add(tex.name);
            }
            foreach (ExternalBufferConstant buf in constants.externalBufferConstants)
            {
                Type extType = TypeLibrary.GetType("ShaderValueType");
                permutation.ExternalBufferConstants.Add
                (
                    new ExternalBuffer
                    {
                        Name = buf.name,
                        Required = Convert.ToBoolean(buf.required),
                        Type = extType.GetEnumName(buf.valueType)
                    }
                );
            }
            permutation.SamplerStates = constants.samplerStates;
        }

    }
}
