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
    public class ShaderSolutionState
    {
        public uint surfaceShaderNameHash;
        public uint vertexShaderFragmentNameHash;
        public uint tessellationShaderFragmentNameHash;
        public uint geometryDeclarationHash;
        public byte[] stateInfo1;
        public byte[] stateInfo2;

        public uint skinningMethod; // maps to ShaderSkinningMethod
        public uint renderMode; // maps to ShaderRenderMode
        public byte unknown;
        public uint instancingMethod; // maps to ShaderInstancingMethod

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

            skinningMethod = (uint)(stateInfo1[1] >> 4);
            renderMode = (uint)(stateInfo1[1] & 0x0F);
            unknown = (byte)(stateInfo1[2] >> 4);
            instancingMethod = (uint)(stateInfo1[2] & 0x0F);

            tessellationEnable = stateInfo2[0] == 1;
        }
    }
}
