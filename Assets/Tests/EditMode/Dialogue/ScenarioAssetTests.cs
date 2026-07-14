using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Agrovator.PitchSimulator.Tests.EditMode.Dialogue
{
    public sealed class ScenarioAssetTests
    {
        [Test]
        public void ScenarioAsset_IsThinUnityFacingReferenceHolder()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(
                    "Agrovator.PitchSimulator.Dialogue.Unity.ScenarioAsset",
                    false))
                .FirstOrDefault(candidate => candidate != null);

            Assert.That(type, Is.Not.Null, "ScenarioAsset Unity wrapper type is missing.");
            Assert.That(type.BaseType, Is.EqualTo(typeof(ScriptableObject)));

            var serializedFields = type.GetFields(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(field => field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)
                .ToArray();

            Assert.That(serializedFields.Select(field => field.Name),
                Is.EquivalentTo(new[] { "_json", "_judge", "_audio" }));
            Assert.That(serializedFields.Single(field => field.Name == "_json").FieldType,
                Is.EqualTo(typeof(TextAsset)));
            Assert.That(serializedFields.Where(field => field.Name != "_json"),
                Has.All.Matches<FieldInfo>(field => field.FieldType == typeof(UnityEngine.Object)));
        }
    }
}
