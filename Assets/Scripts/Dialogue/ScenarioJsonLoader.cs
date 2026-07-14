using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace Agrovator.PitchSimulator.Dialogue
{
    public static class ScenarioJsonLoader
    {
        private const string CompileFailed = "dialogue.compile_failed";
        private const string JsonMalformed = "dialogue.json_malformed";
        private const string JsonMissing = "dialogue.json_missing";

        public static ScenarioJsonLoadResult Load(string json, IEnumerable<string> localizationKeys)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return Failure(JsonMissing);
            }

            ScenarioDefinitionDto definition;
            try
            {
                EnsureSingleJsonDocument(json);
                var serializer = new DataContractJsonSerializer(typeof(ScenarioDefinitionDto));
                using (var reader = JsonReaderWriterFactory.CreateJsonReader(
                    Encoding.UTF8.GetBytes(json),
                    XmlDictionaryReaderQuotas.Max))
                {
                    definition = serializer.ReadObject(reader) as ScenarioDefinitionDto;
                    if (reader.Read())
                    {
                        return Failure(JsonMalformed);
                    }
                }
            }
            catch (SerializationException)
            {
                return Failure(JsonMalformed);
            }
            catch (XmlException)
            {
                return Failure(JsonMalformed);
            }
            catch (FormatException)
            {
                return Failure(JsonMalformed);
            }

            if (definition == null)
            {
                return Failure(JsonMalformed);
            }

            var validationIssues = ScenarioValidator.Validate(definition, localizationKeys);
            if (validationIssues.Count > 0)
            {
                return new ScenarioJsonLoadResult(null, validationIssues);
            }

            try
            {
                return new ScenarioJsonLoadResult(RuntimeScenario.Compile(definition), Array.Empty<ValidationIssue>());
            }
            catch (InvalidOperationException)
            {
                return Failure(CompileFailed);
            }
        }

        private static ScenarioJsonLoadResult Failure(string code)
        {
            return new ScenarioJsonLoadResult(
                null,
                new[] { new ValidationIssue(code, "$", ValidationSeverity.Error) });
        }

        private static void EnsureSingleJsonDocument(string json)
        {
            var index = 0;
            while (index < json.Length && char.IsWhiteSpace(json[index]))
            {
                index++;
            }

            if (index == json.Length || json[index] != '{')
            {
                throw new FormatException("Scenario JSON must contain one object.");
            }

            var depth = 0;
            var inString = false;
            var escaped = false;
            for (; index < json.Length; index++)
            {
                var character = json[index];
                if (inString)
                {
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (character == '\\')
                    {
                        escaped = true;
                    }
                    else if (character == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (character == '"')
                {
                    inString = true;
                }
                else if (character == '{' || character == '[')
                {
                    depth++;
                }
                else if (character == '}' || character == ']')
                {
                    depth--;
                    if (depth == 0)
                    {
                        index++;
                        while (index < json.Length && char.IsWhiteSpace(json[index]))
                        {
                            index++;
                        }

                        if (index != json.Length)
                        {
                            throw new FormatException("Scenario JSON must contain exactly one document.");
                        }

                        return;
                    }
                }
            }

            throw new FormatException("Scenario JSON is incomplete.");
        }
    }

    public sealed class ScenarioJsonLoadResult
    {
        internal ScenarioJsonLoadResult(RuntimeScenario scenario, IEnumerable<ValidationIssue> issues)
        {
            Scenario = scenario;
            var issueCopy = new List<ValidationIssue>(issues ?? Array.Empty<ValidationIssue>());
            Issues = new ReadOnlyCollection<ValidationIssue>(issueCopy);
        }

        public bool IsSuccess => Scenario != null && Issues.Count == 0;

        public RuntimeScenario Scenario { get; }

        public IReadOnlyList<ValidationIssue> Issues { get; }
    }
}
