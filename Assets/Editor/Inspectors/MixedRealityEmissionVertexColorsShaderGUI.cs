// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Microsoft.MixedReality.Toolkit.Editor
{
    /// <summary>
    /// A custom shader inspector for the "Mixed Reality Toolkit/Standard" shader.
    /// </summary>
    public class MixedRealityEmissionVertexColorsShaderGUI : MixedRealityStandardShaderGUI
    {
        protected static class Styles2
        {
            public static GUIContent vertexColorsToAlpha = new GUIContent("Vertex Colors To Alpha", "Convert vertex color to alpha values");
        }

        protected MaterialProperty vertexColorsToAlpha;

        protected void FindProperties2(MaterialProperty[] props)
        {
            vertexColorsToAlpha = FindProperty("_VertexColorsToAlpha", props);
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            Material material = (Material)materialEditor.target;

            FindProperties(props);
            FindProperties2(props);
            Initialize(material);

            RenderingModeOptions(materialEditor);
            MainMapOptions(materialEditor, material);
            RenderingOptions2(materialEditor, material);
            FluentOptions(materialEditor, material);
            AdvancedOptions(materialEditor, material);
        }

        protected void RenderingOptions2(MaterialEditor materialEditor, Material material)
        {
            EditorGUILayout.Space();
            GUILayout.Label(Styles.renderingOptionsTitle, EditorStyles.boldLabel);

            materialEditor.ShaderProperty(directionalLight, Styles.directionalLight);

            if (PropertyEnabled(directionalLight))
            {
                materialEditor.ShaderProperty(specularHighlights, Styles.specularHighlights, 2);
            }

            materialEditor.ShaderProperty(sphericalHarmonics, Styles.sphericalHarmonics);

            materialEditor.ShaderProperty(reflections, Styles.reflections);

            if (PropertyEnabled(reflections))
            {
                materialEditor.ShaderProperty(refraction, Styles.refraction, 2);

                if (PropertyEnabled(refraction))
                {
                    materialEditor.ShaderProperty(refractiveIndex, Styles.refractiveIndex, 4);
                }
            }

            materialEditor.ShaderProperty(rimLight, Styles.rimLight);

            if (PropertyEnabled(rimLight))
            {
                materialEditor.ShaderProperty(rimColor, Styles.rimColor, 2);
                materialEditor.ShaderProperty(rimPower, Styles.rimPower, 2);
            }

            materialEditor.ShaderProperty(vertexColors, Styles.vertexColors);

            materialEditor.ShaderProperty(vertexExtrusion, Styles.vertexExtrusion);

            if (PropertyEnabled(vertexExtrusion))
            {
                materialEditor.ShaderProperty(vertexExtrusionValue, Styles.vertexExtrusionValue, 2);
            }

            materialEditor.ShaderProperty(clippingPlane, Styles.clippingPlane);

            if (PropertyEnabled(clippingPlane))
            {
                GUILayout.Box(string.Format(Styles.propertiesComponentHelp, nameof(ClippingPlane), Styles.clippingPlane.text), EditorStyles.helpBox, new GUILayoutOption[0]);
            }

            materialEditor.ShaderProperty(clippingSphere, Styles.clippingSphere);

            if (PropertyEnabled(clippingSphere))
            {
                GUILayout.Box(string.Format(Styles.propertiesComponentHelp, nameof(ClippingSphere), Styles.clippingSphere.text), EditorStyles.helpBox, new GUILayoutOption[0]);
            }

            materialEditor.ShaderProperty(clippingBox, Styles.clippingBox);

            if (PropertyEnabled(clippingBox))
            {
                GUILayout.Box(string.Format(Styles.propertiesComponentHelp, nameof(ClippingBox), Styles.clippingBox.text), EditorStyles.helpBox, new GUILayoutOption[0]);
            }

            if (PropertyEnabled(clippingPlane) || PropertyEnabled(clippingSphere) || PropertyEnabled(clippingBox))
            {
                materialEditor.ShaderProperty(clippingBorder, Styles.clippingBorder);
                
                if (PropertyEnabled(clippingBorder))
                {
                    materialEditor.ShaderProperty(clippingBorderWidth, Styles.clippingBorderWidth, 2);
                    materialEditor.ShaderProperty(clippingBorderColor, Styles.clippingBorderColor, 2);
                }
            }

            materialEditor.ShaderProperty(nearPlaneFade, Styles.nearPlaneFade);

            if (PropertyEnabled(nearPlaneFade))
            {
                materialEditor.ShaderProperty(nearLightFade, Styles.nearLightFade, 2);
                materialEditor.ShaderProperty(fadeBeginDistance, Styles.fadeBeginDistance, 2);
                materialEditor.ShaderProperty(fadeCompleteDistance, Styles.fadeCompleteDistance, 2);
                materialEditor.ShaderProperty(fadeMinValue, Styles.fadeMinValue, 2);
            }
        }

        protected static void SetupMaterialWithAlbedo(Material material, MaterialProperty albedoMap, MaterialProperty albedoAlphaMode, MaterialProperty albedoAssignedAtRuntime)
        {
            if (albedoMap.textureValue || PropertyEnabled(albedoAssignedAtRuntime))
            {
                material.DisableKeyword(Styles.disableAlbedoMapName);
            }
            else
            {
                material.EnableKeyword(Styles.disableAlbedoMapName);
            }

            switch ((AlbedoAlphaMode)albedoAlphaMode.floatValue)
            {
                case AlbedoAlphaMode.Transparency:
                    {
                        material.DisableKeyword(Styles.albedoMapAlphaMetallicName);
                        material.DisableKeyword(Styles.albedoMapAlphaSmoothnessName);
                    }
                    break;

                case AlbedoAlphaMode.Metallic:
                    {
                        material.EnableKeyword(Styles.albedoMapAlphaMetallicName);
                        material.DisableKeyword(Styles.albedoMapAlphaSmoothnessName);
                    }
                    break;

                case AlbedoAlphaMode.Smoothness:
                    {
                        material.DisableKeyword(Styles.albedoMapAlphaMetallicName);
                        material.EnableKeyword(Styles.albedoMapAlphaSmoothnessName);
                    }
                    break;
            }
        }

        protected static void SetupMaterialWithRenderingMode(Material material, RenderingMode mode, CustomRenderingMode customMode, int renderQueueOverride)
        {
            switch (mode)
            {
                case RenderingMode.Opaque:
                    {
                        material.SetOverrideTag(Styles.renderTypeName, Styles.renderingModeNames[(int)RenderingMode.Opaque]);
                        material.SetInt(Styles.customRenderingModeName, (int)CustomRenderingMode.Opaque);
                        material.SetInt(Styles.sourceBlendName, (int)BlendMode.One);
                        material.SetInt(Styles.destinationBlendName, (int)BlendMode.Zero);
                        material.SetInt(Styles.blendOperationName, (int)BlendOp.Add);
                        material.SetInt(Styles.depthTestName, (int)CompareFunction.LessEqual);
                        material.SetInt(Styles.depthWriteName, (int)DepthWrite.On);
                        material.SetFloat(Styles.depthOffsetFactorName, 0.0f);
                        material.SetFloat(Styles.depthOffsetUnitsName, 0.0f);
                        material.SetInt(Styles.colorWriteMaskName, (int)ColorWriteMask.All);
                        material.DisableKeyword(Styles.alphaTestOnName);
                        material.DisableKeyword(Styles.alphaBlendOnName);
                        material.renderQueue = (renderQueueOverride >= 0) ? renderQueueOverride : (int)RenderQueue.Geometry;
                    }
                    break;

                case RenderingMode.TransparentCutout:
                    {
                        material.SetOverrideTag(Styles.renderTypeName, Styles.renderingModeNames[(int)RenderingMode.TransparentCutout]);
                        material.SetInt(Styles.customRenderingModeName, (int)CustomRenderingMode.TransparentCutout);
                        material.SetInt(Styles.sourceBlendName, (int)BlendMode.One);
                        material.SetInt(Styles.destinationBlendName, (int)BlendMode.Zero);
                        material.SetInt(Styles.blendOperationName, (int)BlendOp.Add);
                        material.SetInt(Styles.depthTestName, (int)CompareFunction.LessEqual);
                        material.SetInt(Styles.depthWriteName, (int)DepthWrite.On);
                        material.SetFloat(Styles.depthOffsetFactorName, 0.0f);
                        material.SetFloat(Styles.depthOffsetUnitsName, 0.0f);
                        material.SetInt(Styles.colorWriteMaskName, (int)ColorWriteMask.All);
                        material.EnableKeyword(Styles.alphaTestOnName);
                        material.DisableKeyword(Styles.alphaBlendOnName);
                        material.renderQueue = (renderQueueOverride >= 0) ? renderQueueOverride : (int)RenderQueue.AlphaTest;
                    }
                    break;

                case RenderingMode.Transparent:
                    {
                        material.SetOverrideTag(Styles.renderTypeName, Styles.renderingModeNames[(int)RenderingMode.Transparent]);
                        material.SetInt(Styles.customRenderingModeName, (int)CustomRenderingMode.Transparent);
                        material.SetInt(Styles.sourceBlendName, (int)BlendMode.SrcAlpha);
                        material.SetInt(Styles.destinationBlendName, (int)BlendMode.OneMinusSrcAlpha);
                        material.SetInt(Styles.blendOperationName, (int)BlendOp.Add);
                        material.SetInt(Styles.depthTestName, (int)CompareFunction.LessEqual);
                        material.SetInt(Styles.depthWriteName, (int)DepthWrite.Off);
                        material.SetFloat(Styles.depthOffsetFactorName, 0.0f);
                        material.SetFloat(Styles.depthOffsetUnitsName, 0.0f);
                        material.SetInt(Styles.colorWriteMaskName, (int)ColorWriteMask.All);
                        material.DisableKeyword(Styles.alphaTestOnName);
                        material.EnableKeyword(Styles.alphaBlendOnName);
                        material.renderQueue = (renderQueueOverride >= 0) ? renderQueueOverride : (int)RenderQueue.Transparent;
                    }
                    break;

                case RenderingMode.PremultipliedTransparent:
                    {
                        material.SetOverrideTag(Styles.renderTypeName, Styles.renderingModeNames[(int)RenderingMode.Transparent]);
                        material.SetInt(Styles.customRenderingModeName, (int)CustomRenderingMode.Transparent);
                        material.SetInt(Styles.sourceBlendName, (int)BlendMode.One);
                        material.SetInt(Styles.destinationBlendName, (int)BlendMode.OneMinusSrcAlpha);
                        material.SetInt(Styles.blendOperationName, (int)BlendOp.Add);
                        material.SetInt(Styles.depthTestName, (int)CompareFunction.LessEqual);
                        material.SetInt(Styles.depthWriteName, (int)DepthWrite.Off);
                        material.SetFloat(Styles.depthOffsetFactorName, 0.0f);
                        material.SetFloat(Styles.depthOffsetUnitsName, 0.0f);
                        material.SetInt(Styles.colorWriteMaskName, (int)ColorWriteMask.All);
                        material.DisableKeyword(Styles.alphaTestOnName);
                        material.EnableKeyword(Styles.alphaBlendOnName);
                        material.renderQueue = (renderQueueOverride >= 0) ? renderQueueOverride : (int)RenderQueue.Transparent;
                    }
                    break;

                case RenderingMode.Additive:
                    {
                        material.SetOverrideTag(Styles.renderTypeName, Styles.renderingModeNames[(int)RenderingMode.Transparent]);
                        material.SetInt(Styles.customRenderingModeName, (int)CustomRenderingMode.Transparent);
                        material.SetInt(Styles.sourceBlendName, (int)BlendMode.One);
                        material.SetInt(Styles.destinationBlendName, (int)BlendMode.One);
                        material.SetInt(Styles.blendOperationName, (int)BlendOp.Add);
                        material.SetInt(Styles.depthTestName, (int)CompareFunction.LessEqual);
                        material.SetInt(Styles.depthWriteName, (int)DepthWrite.Off);
                        material.SetFloat(Styles.depthOffsetFactorName, 0.0f);
                        material.SetFloat(Styles.depthOffsetUnitsName, 0.0f);
                        material.SetInt(Styles.colorWriteMaskName, (int)ColorWriteMask.All);
                        material.DisableKeyword(Styles.alphaTestOnName);
                        material.EnableKeyword(Styles.alphaBlendOnName);
                        material.renderQueue = (renderQueueOverride >= 0) ? renderQueueOverride : (int)RenderQueue.Transparent;
                    }
                    break;

                case RenderingMode.Custom:
                    {
                        material.SetOverrideTag(Styles.renderTypeName, Styles.customRenderingModeNames[(int)customMode]);
                        // _SrcBlend, _DstBlend, _BlendOp, _ZTest, _ZWrite, _ColorWriteMask are controlled by UI.

                        switch (customMode)
                        {
                            case CustomRenderingMode.Opaque:
                                {
                                    material.DisableKeyword(Styles.alphaTestOnName);
                                    material.DisableKeyword(Styles.alphaBlendOnName);
                                }
                                break;

                            case CustomRenderingMode.TransparentCutout:
                                {
                                    material.EnableKeyword(Styles.alphaTestOnName);
                                    material.DisableKeyword(Styles.alphaBlendOnName);
                                }
                                break;

                            case CustomRenderingMode.Transparent:
                                {
                                    material.DisableKeyword(Styles.alphaTestOnName);
                                    material.EnableKeyword(Styles.alphaBlendOnName);
                                }
                                break;
                        }

                        material.renderQueue = (renderQueueOverride >= 0) ? renderQueueOverride : material.renderQueue;
                    }
                    break;
            }
        }
    }
}
