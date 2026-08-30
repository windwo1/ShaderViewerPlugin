using Frosty.Core;
using FrostySdk.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MeshSetPlugin.ShaderData
{
    public class Startup : StartupAction
    {
        public override Action<ILogger> Action => (logger) =>
        {
            ShaderDb shaderDb = new ShaderDb(logger);
            shaderDb.Load();
        };
    }
}
