using Frosty.Core;
using Frosty.Core.Controls;
using Frosty.Core.Windows;
using FrostySdk;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace MeshSetPlugin.ShaderData
{
    public class ShaderGraphAssetDefinition : AssetDefinition
    {
        protected static ImageSource shaderGraphImageSource = new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyCore;Component/Images/Assets/ShaderFileType.png") as ImageSource;

        public override void GetSupportedExportTypes(List<AssetExportType> exportTypes)
        {
            exportTypes.Add(new AssetExportType("cso", "Compiled Shader Object"));
            base.GetSupportedExportTypes(exportTypes);
        }

        public override FrostyAssetEditor GetEditor(ILogger logger)
        {
            return new ShaderGraphEditor(logger);
        }

        public override ImageSource GetIcon()
        {
            return shaderGraphImageSource;
        }

    }

    public class ShaderGraphOverride : BaseTypeOverride
    {
        public BaseFieldOverride MaxSubMaterialCount { get; set; }
        public BaseFieldOverride GammaCorrectionEnable { get; set; }
    }

    public class ShaderGraphEditor : FrostyAssetEditor
    {
        private FrostyPropertyGrid pgAsset;
        ShaderGraphData graphInfo = new ShaderGraphData();
        private ShaderDb db;

        static ShaderGraphEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ShaderGraphEditor), new FrameworkPropertyMetadata(typeof(ShaderGraphEditor)));
        }

        public ShaderGraphEditor(ILogger inLogger)
            : base(inLogger)
        {
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            pgAsset = GetTemplateChild("PART_AssetPropertyGrid") as FrostyPropertyGrid;

            Loaded += ShaderGraphEditor_Loaded;

        }

        private void ShaderGraphEditor_Loaded(object sender, RoutedEventArgs e)
        {
            List<BundleEntry> bundleEntries = new List<BundleEntry>();
            foreach (int i in AssetEntry.Bundles)
            {
                bundleEntries.Add(App.AssetManager.GetBundleEntry(i));
            }

            db = new ShaderDb(AssetEntry, logger);

            // start loading shaderdb
            FrostyTaskWindow.Show("Loading shader databases", "", (task) =>
            {
                db.Load(graphInfo);
            });

            if (db.Loaded)
            {
                pgAsset.SetClass(graphInfo);
                logger.Log($"Loaded info for shader {AssetEntry.Filename}");
            }
            else
                logger.Log($"Shader database has failed loading");
        }
    }
}
