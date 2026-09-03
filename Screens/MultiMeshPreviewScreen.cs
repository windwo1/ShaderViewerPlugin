using Frosty.Core;
using Frosty.Core.Screens;
using Frosty.Core.Viewport;
using FrostySdk;
using FrostySdk.IO;
using FrostySdk.Managers;
using MeshSetPlugin.Render;
using MeshSetPlugin.Resources;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using DXUT = Frosty.Core.Viewport.DXUT;
using MeshMaterialCollection = MeshSetPlugin.Render.MeshMaterialCollection;

namespace MeshSetPlugin.Screens
{
    public class MeshAndPreviewContainer
    {
        public MeshSet Mesh;
        public MeshRenderMesh Preview;
        public MeshMaterialCollection Materials;
        public bool bUpdateMaterials;
        public Matrix Transform;
        public int MeshId;

        public MeshAndPreviewContainer(int inMeshId, MeshSet inMesh, MeshRenderMesh inPreview, Matrix inTransform, MeshMaterialCollection inMaterials)
        {
            MeshId = inMeshId;
            Mesh = inMesh;
            Preview = inPreview;
            Materials = inMaterials;
            Transform = inTransform;
        }
    }

    public delegate void RenderAction(RenderCreateState2 state);

    public class MultiMeshPreviewScreen : ShaderRenderScreen
    {
        public int CurrentLOD { get; set; }
        public bool IsLoading { get; set; }

        private List<MeshAndPreviewContainer> renderMeshes = new List<MeshAndPreviewContainer>();
        private List<LightRenderInstance> renderLights = new List<LightRenderInstance>();

        private EbxAssetEntry visualEnvironment;

        private int currentMeshId = 0;
        private int currentLightId = 0;
        private Queue<RenderAction> renderTasks = new Queue<RenderAction>();

        // @temp
        private MeshRenderAnim anim;

        public MultiMeshPreviewScreen()
        {
        }

        public MeshSet GetMesh(int meshId)
        {
            if (meshId >= renderMeshes.Count)
                return null;
            return renderMeshes[meshId].Mesh;
        }

        public int AddMesh(MeshSet mesh, MeshMaterialCollection materials, Matrix transform, MeshRenderSkeleton skeleton = null)
        {
            int meshId = currentMeshId;
            renderTasks.Enqueue((RenderCreateState2 state) =>
            {
                // just build a dummy one if one wasnt provided
                if (skeleton == null)
                    skeleton = new MeshRenderSkeleton();

                IsLoading = true;
                MeshRenderMesh renderMesh = new MeshRenderMesh(state, mesh, materials, skeleton);
                renderMeshes.Add(new MeshAndPreviewContainer(meshId, mesh, renderMesh, transform, materials));
                IsLoading = false;
            });

            return currentMeshId++;
        }

        public int AddLight(LightRenderType type, Matrix transform, Vector3 color, float intensity, float attenuationRadius, float sphereRadius)
        {
            renderLights.Add(new LightRenderInstance()
            {
                Type = type,
                Transform = transform,
                Color = color,
                Intensity = intensity,
                AttenuationRadius = attenuationRadius,
                SphereRadius = sphereRadius,
                LightId = currentLightId
            });
            return currentLightId++;
        }

        public void ModifyLight(int lightId, Matrix transform, Vector3 color, float intensity, float attenuationRadius, float sphereRadius)
        {
            int idx = renderLights.FindIndex((LightRenderInstance inst) => inst.LightId == lightId);
            if (idx != -1)
            {
                renderLights[idx] = new LightRenderInstance()
                {
                    Type = renderLights[lightId].Type,
                    Transform = transform,
                    Color = color,
                    Intensity = intensity,
                    AttenuationRadius = attenuationRadius,
                    SphereRadius = sphereRadius,
                    LightId = renderLights[idx].LightId
                };
            }
        }

        public void RemoveMesh(int meshId)
        {
            renderTasks.Enqueue((RenderCreateState2 state) =>
            {
                MeshAndPreviewContainer meshContainer = renderMeshes.Find((MeshAndPreviewContainer a) => a.MeshId == meshId);
                if (meshContainer != null)
                {
                    meshContainer.Preview.Dispose();
                    renderMeshes.Remove(meshContainer);
                }
            });
        }

        public void RemoveLight(int lightId)
        {
            int idx = renderLights.FindIndex((LightRenderInstance inst) => inst.LightId == lightId);
            if (idx != -1)
                renderLights.RemoveAt(idx);
        }

        public void ClearMeshes(bool clearAll = false)
        {
            renderTasks.Enqueue((RenderCreateState2 state) =>
            {
                MeshAndPreviewContainer rootMesh = renderMeshes[0];
                renderMeshes.Clear();
                if (!clearAll)
                    renderMeshes.Add(rootMesh);
            });

            if (clearAll)
                currentMeshId = 0;
        }

        public void ClearLights()
        {
            renderLights.Clear();
        }

        public void LoadMaterials(int meshId, MeshMaterialCollection inMaterials)
        {
            renderTasks.Enqueue((RenderCreateState2 state) =>
            {
                MeshAndPreviewContainer meshContainer = renderMeshes.Find((MeshAndPreviewContainer a) => a.MeshId == meshId);
                meshContainer.Preview.SetMaterials(RenderCreateState2, inMaterials);
            });
        }

        public void SetTransform(int meshId, Matrix transform)
        {
            renderTasks.Enqueue((RenderCreateState2 state) =>
            {
                MeshAndPreviewContainer meshContainer = renderMeshes.Find((MeshAndPreviewContainer a) => a.MeshId == meshId);
                meshContainer.Transform = transform;
            });
        }

        public void SetMeshSectionSelected(int meshId, int lodId, int sectionId, bool newSelected)
        {
            MeshAndPreviewContainer meshContainer = renderMeshes.Find((MeshAndPreviewContainer a) => a.MeshId == meshId);
            meshContainer.Preview.GetLod(lodId).GetSection(sectionId).IsSelected = newSelected;
        }

        public void SetMeshSectionVisible(int meshId, int lodId, int sectionId, bool newVisible)
        {
            MeshAndPreviewContainer meshContainer = renderMeshes.Find((MeshAndPreviewContainer a) => a.MeshId == meshId);
            meshContainer.Preview.GetLod(lodId).GetSection(sectionId).IsVisible = newVisible;
        }

        public void SetDistantLightProbeTexture(EbxAssetEntry entry)
        {
            renderTasks.Enqueue((RenderCreateState2 state) =>
            {
                DistantLightProbe = (entry != null)
                    ? state.TextureLibrary.LoadTextureAsset(entry.Guid, true)
                    : null;
            });
        }

        public void SetLookupTableTexture(EbxAssetEntry entry)
        {
            if (visualEnvironment != null)
                return;

            renderTasks.Enqueue((RenderCreateState2 state) =>
            {
                LookupTable = (entry != null)
                    ? state.TextureLibrary.LoadTextureAsset(entry.Guid)
                    : null;
            });
        }

        public void SetSkyboxTexture(EbxAssetEntry entry)
        {
            if (visualEnvironment != null)
                return;

            renderTasks.Enqueue((RenderCreateState2 state) =>
            {
                Skybox = (entry != null)
                    ? state.TextureLibrary.LoadTextureAsset(entry.Guid, true)
                    : null;
            });
        }

        public void SetVisualEnvironment(EbxAssetEntry entry, MeshSetPreviewSettings previewSettings)
        {
            renderTasks.Enqueue((RenderCreateState2 state) =>
            {
                // set defaults in case visenv is null
                SunColor = SharpDXUtils.FromVec3(previewSettings.SunColor);
                SunIntensity = previewSettings.SunIntensity;
                BloomStrength = previewSettings.BloomStrength;
                MinEV100 = previewSettings.MinEV100;
                MaxEV100 = previewSettings.MaxEV100;

                var defaultLookupTable = App.AssetManager.GetEbxEntry(previewSettings.ColorLookupTable.External.FileGuid);
                var defaultSkybox = App.AssetManager.GetEbxEntry(previewSettings.SkyboxTexture.External.FileGuid);

                LookupTable = (defaultLookupTable != null)
                    ? state.TextureLibrary.LoadTextureAsset(defaultLookupTable.Guid)
                    : null;
                if (ProfilesLibrary.DataVersion != (int)ProfileVersion.PlantsVsZombiesGardenWarfare)
                {
                    Skybox = (defaultSkybox != null)
                        ? state.TextureLibrary.LoadTextureAsset(defaultSkybox.Guid, true)
                        : null;
                }

                Globals = new GlobalsData();

                if (entry == null)
                    return;

                visualEnvironment = entry;

                EbxAsset asset = App.AssetManager.GetEbx(entry);
                dynamic rootObject = asset.RootObject;

                if (rootObject.Object.Type == PointerRefType.Null)
                {
                    visualEnvironment = null;
                    return;
                }

                var globals = new GlobalsData();

                foreach (var pointer in rootObject.Object.Internal.Components)
                {
                    var component = pointer.Internal;
                    switch (component.GetType().Name)
                    {
                        case "OutdoorLightComponentData":
                            if (ProfilesLibrary.DataVersion != (int)ProfileVersion.PlantsVsZombiesGardenWarfare)
                            {
                                SunIntensity = component.SunIntensity * 100;
                            }
                            SunColor = SharpDXUtils.FromVec3(component.SunColor);
                            break;
                        case "TonemapComponentData":
                            BloomStrength = (component.BloomScale.x * component.BloomScale.y * component.BloomScale.z) / 3;

                            float minEv = 8.0f;
                            float maxEv = 20.0f;
                            if (ProfilesLibrary.DataVersion == (int)ProfileVersion.PlantsVsZombiesGardenWarfare)
                            {
                                minEv = component.MinExposure + 8.0f;
                                maxEv = component.MaxExposure + 20.0f;
                            }
                            else
                            {
                                minEv = component.MinEV + 8.0f;
                                maxEv = component.MaxEV + 20.0f;
                            }
                            MinEV100 = minEv;
                            MaxEV100 = maxEv;
                            break;
                        case "ColorCorrectionComponentData":
                            if (component.ColorGradingEnable == true && component.ColorGradingTexture.Type != PointerRefType.Null)
                            {
                                var lookupTable = App.AssetManager.GetEbxEntry(component.ColorGradingTexture.External.FileGuid);
                                LookupTable = (lookupTable != null)
                                    ? state.TextureLibrary.LoadTextureAsset(lookupTable.Guid)
                                    : null; ;
                            }
                            break;
                        case "SkyComponentData":
                            if (ProfilesLibrary.DataVersion != (int)ProfileVersion.PlantsVsZombiesGardenWarfare)
                            {
                                var sky = App.AssetManager.GetEbxEntry(component.HdriTexture.External.FileGuid);
                                Skybox = (sky != null)
                                    ? state.TextureLibrary.LoadTextureAsset(sky.Guid, true)
                                    : null;
                            }
                            break;
                        case "VignetteComponentData":
                            globals.VignetteScale = new Vector2(component.Scale.x, component.Scale.y);
                            globals.VignetteExponent = component.Exponent;
                            globals.VignetteColor = SharpDXUtils.FromVec3(component.Color);
                            globals.VignetteOpacity = component.Opacity;
                            break;
                    }
                }

                Globals = globals;
            });

            return;
        }

        public void SetAnimation(MeshRenderAnim inAnim)
        {
            anim = inAnim;
        }

        public void SetTexture(string name, string paramName, EbxAssetEntry texture)
        {
            //if (!meshes.ContainsKey(name))
            //    return;

            //MeshSet mesh = meshes[name].Mesh;
            //for (int i = 0; i < mesh.Lods[0].Sections.Count; i++)
            //    SetTexture(name, i, paramName, texture);
        }

        public void SetTexture(string name, int sectionId, string paramName, EbxAssetEntry texture, int uvChannel = 0)
        {
            //if (!meshes.ContainsKey(name))
            //    return;

            //MeshRenderMesh preview = meshes[name].Preview;
            //if (preview == null)
            //{
            //    MeshAndPreviewContainer mesh = meshes[name];
            //    mesh.PreviewLoaded += (o, e) => { SetTexture(name, sectionId, paramName, texture, uvChannel); };
            //    return;
            //}

            //MeshRenderLod lod = preview.GetLod(0);
            //if (lod.GetFallbackSection(sectionId) == null)
            //    return;

            //MeshSetPreviewSection section = lod.GetFallbackSection(sectionId);
            //ShaderResourceView srv = null;

            //if (paramName == "base")
            //{
            //    srv = section.DiffuseTexture;
            //    section.DiffuseTexture = LoadTextureAsset(texture.Guid);
            //}
            //else if (paramName == "normal")
            //{
            //    srv = section.NormTexture;
            //    section.NormTexture = LoadTextureAsset(texture.Guid);
            //}
            //else if (paramName == "coeff")
            //{
            //    srv = section.MaskTexture;
            //    section.MaskTexture = LoadTextureAsset(texture.Guid);
            //}

            //if (uvChannel != 0)
            //    section.CustomParam3 = 1;

            //if (srv != null)
            //    UnloadTexture(srv);
        }

        public override void CreateBuffers()
        {
            if (camera == null)
            {
                camera = new DXUT.FirstPersonCamera();
                camera.SetViewParams(new Vector3(0, 1.5f, 4.0f), new Vector3(0, 0, 0));
            }

            base.CreateBuffers();
        }

        public override void DisposeBuffers()
        {
            foreach (MeshAndPreviewContainer mesh in renderMeshes)
            {
                mesh.Preview?.Dispose();
            }
            renderMeshes.Clear();

            base.DisposeBuffers();
        }

        public override void Closed()
        {
            base.Closed();
        }

        public override void Update(double timestep)
        {
            base.Update(timestep);

            if (anim != null)
            {
                anim.Update(timestep);
                foreach (MeshAndPreviewContainer mesh in renderMeshes)
                {
                    foreach (MeshRenderSection section in mesh.Preview.GetLod(CurrentLOD).Sections)
                    {
                        anim.UpdateSkeleton(section.Skeleton);
                    }
                }
            }
        }

        bool oneTimeAction = true;
        public override void Render()
        {
            while (renderTasks.Count > 0)
            {
                RenderAction action = renderTasks.Dequeue();
                action.Invoke(RenderCreateState2);
            }

            // @hack
            if (renderMeshes.Count > 0 && oneTimeAction)
            {
                CenterView();
                oneTimeAction = false;
            }

            base.Render();
        }

        public override List<MeshRenderInstance> CollectMeshInstances()
        {
            List<MeshRenderInstance> instances = new List<MeshRenderInstance>();
            foreach (MeshAndPreviewContainer mesh in renderMeshes)
            {
                MeshRenderLod lod = mesh.Preview.GetLod(CurrentLOD);
                MeshRenderInstance inst = new MeshRenderInstance()
                {
                    RenderMesh = lod,
                    Transform = mesh.Transform
                };
                instances.Add(inst);
            }
            return instances;
        }

        public override List<LightRenderInstance> CollectLightInstances()
        {
            return renderLights;
        }

        public override void CharTyped(char ch)
        {
            base.CharTyped(ch);
            if (ch == 'f')
            {
                CenterView();
            }
        }

        public void CenterView()
        {
            BoundingBox aabb = CalcWorldBoundingBox();
            if (camera is DXUT.ModelViewerCamera mvCamera)
            {
                mvCamera.Reset();
                mvCamera.SetLookAtPt(aabb.Minimum + (aabb.Maximum - aabb.Minimum) * 0.5f);
                mvCamera.SetEyePt(aabb.Minimum + (aabb.Maximum - aabb.Minimum) * 0.5f + Vector3.UnitY);
                mvCamera.SetRadius((aabb.Maximum - aabb.Minimum).Length() * 1.0f);
            }
            else if (camera is DXUT.FirstPersonCamera fpCamera)
            {
                Vector3 center = aabb.Minimum + (aabb.Maximum - aabb.Minimum) * 0.5f;
                Vector3 offset = center + new Vector3(0, Math.Abs(aabb.Maximum.Y - aabb.Minimum.Y) / 1.25f, (aabb.Maximum - aabb.Minimum).Length() * 0.9f);
                if (offset.Y < 0.5f)
                    offset.Y = 0.5f;

                fpCamera.SetViewParams(offset, center);
            }
        }

        protected override BoundingBox CalcWorldBoundingBox()
        {
            BoundingBox aabb = new BoundingBox();
            int i = 0;

            foreach (MeshAndPreviewContainer mesh in renderMeshes)
            {
                BoundingBox bb = mesh.Preview.Bounds;
                bb.Minimum = (bb.Minimum + mesh.Transform.TranslationVector) * new Vector3(-1, 1, 1);
                bb.Maximum = (bb.Maximum + mesh.Transform.TranslationVector) * new Vector3(-1, 1, 1);

                float tmp = bb.Minimum.X;
                bb.Minimum.X = bb.Maximum.X;
                bb.Maximum.X = tmp;

                if (i++ == 0)
                    aabb = bb;
                else
                    aabb = BoundingBox.Merge(aabb, bb);
            }

            return aabb;
        }
    }
}
