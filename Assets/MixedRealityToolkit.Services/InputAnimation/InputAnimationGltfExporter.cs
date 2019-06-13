// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Utilities.Gltf.Schema;
using Microsoft.MixedReality.Toolkit.Utilities.Gltf.Serialization;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Input
{
    public static class InputAnimationGltfExporter
    {
        public static async void OnExportInputAnimation(InputAnimation animation, string path)
        {
            GltfObject exportedObject = new GltfObject();

            exportedObject.extensionsUsed = new string[0];
            exportedObject.extensionsRequired = new string[0];
            exportedObject.accessors = new GltfAccessor[0];
            exportedObject.animations = new GltfAnimation[0];
            exportedObject.asset = new GltfAssetInfo();
            exportedObject.buffers = new GltfBuffer[0];
            exportedObject.bufferViews = new GltfBufferView[0];
            exportedObject.cameras = new GltfCamera[0];
            exportedObject.images = new GltfImage[0];
            exportedObject.materials = new GltfMaterial[0];
            exportedObject.meshes = new GltfMesh[0];
            exportedObject.nodes = new GltfNode[0];
            exportedObject.samplers = new GltfSampler[0];
            exportedObject.scene = 0;
            exportedObject.scenes = new GltfScene[0];
            exportedObject.skins = new GltfSkin[0];
            exportedObject.textures = new GltfTexture[0];

            await GltfUtility.ExportGltfObjectToPathAsync(exportedObject, path);

#if false
            var importedObject = await GltfUtility.ImportGltfObjectFromPathAsync(context.assetPath);

            if (importedObject == null ||
                importedObject.GameObjectReference == null)
            {
                Debug.LogError("Failed to import glTF object");
                return;
            }

            var gltfAsset = (GltfAsset)ScriptableObject.CreateInstance(typeof(GltfAsset));

            gltfAsset.GltfObject = importedObject;
            gltfAsset.name = $"{gltfAsset.GltfObject.Name}{Path.GetExtension(context.assetPath)}";
            gltfAsset.Model = importedObject.GameObjectReference;
            context.AddObjectToAsset("main", gltfAsset.Model);
            context.SetMainObject(importedObject.GameObjectReference);
            context.AddObjectToAsset("glTF data", gltfAsset);

            bool reImport = false;

            for (var i = 0; i < gltfAsset.GltfObject.textures?.Length; i++)
            {
                GltfTexture gltfTexture = gltfAsset.GltfObject.textures[i];

                if (gltfTexture == null) { continue; }

                var path = AssetDatabase.GetAssetPath(gltfTexture.Texture);

                if (string.IsNullOrWhiteSpace(path))
                {
                    var textureName = gltfTexture.name;

                    if (string.IsNullOrWhiteSpace(textureName))
                    {
                        textureName = $"Texture_{i}";
                        gltfTexture.Texture.name = textureName;
                    }

                    context.AddObjectToAsset(textureName, gltfTexture.Texture);
                }
                else
                {
                    if (!gltfTexture.Texture.isReadable)
                    {
                        var textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
                        if (textureImporter != null)
                        {
                            textureImporter.isReadable = true;
                            textureImporter.SetPlatformTextureSettings(new TextureImporterPlatformSettings { format = TextureImporterFormat.RGBA32 });
                            textureImporter.SaveAndReimport();
                            reImport = true;
                        }
                    }
                }
            }

            if (reImport)
            {
                var importer = AssetImporter.GetAtPath(context.assetPath);
                importer.SaveAndReimport();
                return;
            }

            for (var i = 0; i < gltfAsset.GltfObject.meshes?.Length; i++)
            {
                GltfMesh gltfMesh = gltfAsset.GltfObject.meshes[i];

                string meshName = string.IsNullOrWhiteSpace(gltfMesh.name) ? $"Mesh_{i}" : gltfMesh.name;

                gltfMesh.Mesh.name = meshName;
                context.AddObjectToAsset($"{meshName}", gltfMesh.Mesh);
            }

            if (gltfAsset.GltfObject.materials != null)
            {
                foreach (GltfMaterial gltfMaterial in gltfAsset.GltfObject.materials)
                {
                    if (context.assetPath.EndsWith(".glb"))
                    {
                        context.AddObjectToAsset(gltfMaterial.name, gltfMaterial.Material);
                    }
                    else
                    {
                        var path = Path.GetFullPath(Path.GetDirectoryName(context.assetPath));
                        path = path.Replace("\\", "/").Replace(Application.dataPath, "Assets");
                        path = $"{path}/{gltfMaterial.name}.mat";
                        AssetDatabase.CreateAsset(gltfMaterial.Material, path);
                        gltfMaterial.Material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    }
                }
            }
#endif
        }
    }
}