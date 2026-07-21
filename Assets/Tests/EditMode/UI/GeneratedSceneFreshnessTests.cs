using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Agrovator.PitchSimulator.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Agrovator.PitchSimulator.Tests.EditMode.UI
{
    /// <summary>
    /// Proves the committed scenes are what today's builder produces.
    ///
    /// The generator stamp makes regeneration reachable, but bumping it is
    /// manual: a builder change with no bump still leaves a stale scene saved,
    /// which is exactly how `semanticHoldSeconds` shipped as 1.6 against a
    /// 2.5f constant. This closes that by regenerating into the working copy
    /// and comparing the result against what is committed, so a builder change
    /// that never reached the scenes fails here whether or not anyone
    /// remembered the bump.
    ///
    /// The comparison is a hierarchy fingerprint rather than file bytes,
    /// because Unity assigns fresh fileIDs on every regeneration - a byte
    /// comparison differs on every run even when nothing changed. Object
    /// references are rendered as hierarchy paths or asset paths so they
    /// survive that renumbering.
    /// </summary>
    public sealed class GeneratedSceneFreshnessTests
    {
        private static readonly string[] GeneratedScenePaths =
        {
            "Assets/Scenes/Bootstrap.unity",
            "Assets/Scenes/Game.unity",
            "Assets/Scenes/WebIntegrationTest.unity",
        };

        [Test]
        public void CommittedScenes_MatchWhatTheCurrentBuilderGenerates()
        {
            var originalBytes = GeneratedScenePaths.ToDictionary(
                path => path, File.ReadAllBytes, StringComparer.Ordinal);
            try
            {
                var committed = GeneratedScenePaths.ToDictionary(
                    path => path, Fingerprint, StringComparer.Ordinal);

                // Force regeneration without relying on the version constant
                // having been bumped, which is the mistake being guarded.
                foreach (var path in GeneratedScenePaths)
                {
                    StaleTheStamp(path);
                }

                PitchSimulatorProjectBuilder.BuildProjectFoundationBatch();

                foreach (var path in GeneratedScenePaths)
                {
                    Assert.That(Fingerprint(path), Is.EqualTo(committed[path]),
                        path + " is not what the current builder generates. A builder change " +
                        "did not reach the saved scene: regenerate through " +
                        "PitchSimulatorProjectBuilder.BuildProjectFoundationBatch and commit " +
                        "the result.");
                }
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                foreach (var pair in originalBytes)
                {
                    File.WriteAllBytes(pair.Key, pair.Value);
                }
                AssetDatabase.Refresh(
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            }
        }

        private static void StaleTheStamp(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                var stamps = scene.GetRootGameObjects()
                    .Select(root => root.GetComponent<PitchSimulator.UI.GeneratedSceneStamp>())
                    .Where(stamp => stamp != null)
                    .ToArray();
                Assert.That(stamps, Has.Length.EqualTo(1),
                    scenePath + " must carry exactly one generator stamp.");
                var serialized = new SerializedObject(stamps[0]);
                serialized.FindProperty("generatorVersion").intValue =
                    PitchSimulatorProjectBuilder.GeneratorVersion - 1;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.SaveScene(scene, scenePath);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static string Fingerprint(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                var builder = new StringBuilder();
                foreach (var root in scene.GetRootGameObjects()
                             .OrderBy(root => root.name, StringComparer.Ordinal))
                {
                    AppendTransform(builder, root.transform, root.name);
                }

                return builder.ToString();
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void AppendTransform(StringBuilder builder, Transform transform, string path)
        {
            builder.Append(path)
                .Append("|active=").Append(transform.gameObject.activeSelf)
                .Append('\n');

            foreach (var component in transform.GetComponents<Component>())
            {
                if (component == null)
                {
                    builder.Append(path).Append("|<missing script>\n");
                    continue;
                }

                builder.Append(path).Append('|').Append(component.GetType().FullName).Append('\n');
                AppendSerializedProperties(builder, path, component);
            }

            // Sibling order is part of the generated contract - screens are
            // asserted in order elsewhere - so children are not sorted.
            for (var index = 0; index < transform.childCount; index++)
            {
                var child = transform.GetChild(index);
                AppendTransform(builder, child, path + "/" + index + ":" + child.name);
            }
        }

        private static void AppendSerializedProperties(
            StringBuilder builder, string path, Component component)
        {
            var iterator = new SerializedObject(component).GetIterator();
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (IgnoredProperties.Contains(iterator.propertyPath, StringComparer.Ordinal))
                {
                    continue;
                }

                builder.Append(path).Append('|').Append(iterator.propertyPath).Append('=')
                    .Append(DescribeProperty(iterator)).Append('\n');
            }
        }

        /// <summary>
        /// Renumbered on every regeneration, so they say nothing about whether
        /// the scene is current.
        /// </summary>
        private static readonly string[] IgnoredProperties =
        {
            "m_ObjectHideFlags", "m_CorrespondingSourceObject", "m_PrefabInstance",
            "m_PrefabAsset", "m_GameObject", "m_Script",
        };

        private static string DescribeProperty(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    return DescribeReference(property.objectReferenceValue);
                case SerializedPropertyType.Generic:
                    return property.isArray ? "array[" + property.arraySize + "]" : "generic";
                case SerializedPropertyType.Float:
                    return property.floatValue.ToString("R");
                default:
                    return SerializedPropertyToString(property);
            }
        }

        private static string SerializedPropertyToString(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer: return property.intValue.ToString();
                case SerializedPropertyType.Boolean: return property.boolValue.ToString();
                case SerializedPropertyType.String: return property.stringValue;
                case SerializedPropertyType.Color: return property.colorValue.ToString();
                case SerializedPropertyType.Enum: return property.enumValueIndex.ToString();
                case SerializedPropertyType.Vector2: return property.vector2Value.ToString("R");
                case SerializedPropertyType.Vector3: return property.vector3Value.ToString("R");
                case SerializedPropertyType.Vector4: return property.vector4Value.ToString("R");
                case SerializedPropertyType.Rect: return property.rectValue.ToString();
                case SerializedPropertyType.Bounds: return property.boundsValue.ToString();
                case SerializedPropertyType.Quaternion:
                    return property.quaternionValue.ToString("R");
                default: return property.propertyType.ToString();
            }
        }

        /// <summary>
        /// A reference is described by what it points at, not by the fileID it
        /// happens to carry this regeneration: assets by path, scene objects by
        /// hierarchy path.
        /// </summary>
        private static string DescribeReference(UnityEngine.Object value)
        {
            if (value == null)
            {
                return "<null>";
            }

            var assetPath = AssetDatabase.GetAssetPath(value);
            if (!string.IsNullOrEmpty(assetPath))
            {
                return "asset:" + assetPath + "#" + value.name;
            }

            switch (value)
            {
                case GameObject gameObject:
                    return "scene:" + HierarchyPath(gameObject.transform);
                case Component component:
                    return "scene:" + HierarchyPath(component.transform) +
                        "#" + component.GetType().Name;
                default:
                    return "object:" + value.GetType().FullName + "#" + value.name;
            }
        }

        private static string HierarchyPath(Transform transform)
        {
            var segments = new List<string>();
            for (var current = transform; current != null; current = current.parent)
            {
                segments.Add(current.name);
            }

            segments.Reverse();
            return string.Join("/", segments);
        }
    }
}
