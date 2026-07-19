using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace Agrovator.PitchSimulator.GuidedPitch
{
    public static class GuidedPitchContentLoader
    {
        public static GuidedPitchContentLoadResult Load(
            string json,
            IEnumerable<string> localizationKeys)
        {
            return LoadCore(json, dto => GuidedPitchContentValidator.Validate(dto, localizationKeys));
        }

        public static GuidedPitchContentLoadResult LoadWithLocalizationValues(
            string json,
            IReadOnlyDictionary<string, string> localizationValues)
        {
            return LoadCore(
                json,
                dto => GuidedPitchContentValidator.ValidateWithLocalizationValues(dto, localizationValues));
        }

        private static GuidedPitchContentLoadResult LoadCore(
            string json,
            Func<GuidedPitchContentDto, IReadOnlyList<GuidedPitchContentIssue>> validate)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return Failure("guided.json_missing");
            }

            GuidedPitchContentDto dto;
            try
            {
                EnsureSingleJsonDocument(json);
                var serializer = new DataContractJsonSerializer(typeof(GuidedPitchContentDto));
                using (var reader = JsonReaderWriterFactory.CreateJsonReader(
                    Encoding.UTF8.GetBytes(json),
                    XmlDictionaryReaderQuotas.Max))
                {
                    dto = serializer.ReadObject(reader) as GuidedPitchContentDto;
                    if (reader.Read())
                    {
                        return Failure("guided.json_malformed");
                    }
                }
            }
            catch (SerializationException)
            {
                return Failure("guided.json_malformed");
            }
            catch (XmlException)
            {
                return Failure("guided.json_malformed");
            }
            catch (FormatException)
            {
                return Failure("guided.json_malformed");
            }

            if (dto == null)
            {
                return Failure("guided.json_malformed");
            }

            var issues = validate(dto);
            if (HasError(issues))
            {
                return new GuidedPitchContentLoadResult(null, issues);
            }

            try
            {
                return new GuidedPitchContentLoadResult(GuidedPitchContent.Compile(dto), issues);
            }
            catch (Exception exception) when (
                exception is ArgumentException ||
                exception is InvalidOperationException ||
                exception is NullReferenceException)
            {
                return Failure("guided.compile_failed");
            }
        }

        private static bool HasError(IEnumerable<GuidedPitchContentIssue> issues)
        {
            foreach (var issue in issues)
            {
                if (issue.Severity == GuidedPitchContentIssueSeverity.Error)
                {
                    return true;
                }
            }

            return false;
        }

        private static GuidedPitchContentLoadResult Failure(string code)
        {
            return new GuidedPitchContentLoadResult(
                null,
                new[]
                {
                    new GuidedPitchContentIssue(code, "$", GuidedPitchContentIssueSeverity.Error),
                });
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
                throw new FormatException("Guided pitch JSON must contain one object.");
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
                            throw new FormatException("Guided pitch JSON must contain exactly one document.");
                        }

                        return;
                    }
                }
            }

            throw new FormatException("Guided pitch JSON is incomplete.");
        }
    }

    public sealed class GuidedPitchContentLoadResult
    {
        internal GuidedPitchContentLoadResult(
            GuidedPitchContent content,
            IEnumerable<GuidedPitchContentIssue> issues)
        {
            Content = content;
            Issues = new ReadOnlyCollection<GuidedPitchContentIssue>(
                new List<GuidedPitchContentIssue>(issues ?? Array.Empty<GuidedPitchContentIssue>()));
        }

        public bool IsSuccess => Content != null && !ContainsError(Issues);
        public GuidedPitchContent Content { get; }
        public IReadOnlyList<GuidedPitchContentIssue> Issues { get; }

        private static bool ContainsError(IEnumerable<GuidedPitchContentIssue> issues)
        {
            foreach (var issue in issues)
            {
                if (issue.Severity == GuidedPitchContentIssueSeverity.Error)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
