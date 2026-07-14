using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace Agrovator.PitchSimulator.Core
{
    public static class SaveDataMigrator
    {
        public const int CurrentVersion = 1;

        private const int NormalTimerMode = 0;
        private const int OffTimerMode = 2;
        private const string EnglishLocale = "en";
        private const string MalayLocale = "ms";

        public static SaveData CreateDefault()
        {
            return new SaveData
            {
                Version = CurrentVersion,
                TimerMode = NormalTimerMode,
                ReducedMotion = false,
                MusicVolume = 1f,
                SfxVolume = 1f,
                Locale = EnglishLocale,
            };
        }

        public static string Serialize(SaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            ValidateCurrent(data);
            var serializer = new DataContractJsonSerializer(typeof(SaveData));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, data);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public static SaveData DeserializeAndMigrate(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Save JSON is required.", nameof(json));
            }

            SaveData data;
            try
            {
                EnsureSingleJsonDocument(json, "Save JSON");
                var serializer = new DataContractJsonSerializer(typeof(SaveData));
                using (var reader = JsonReaderWriterFactory.CreateJsonReader(
                    Encoding.UTF8.GetBytes(json),
                    XmlDictionaryReaderQuotas.Max))
                {
                    data = serializer.ReadObject(reader) as SaveData;
                    if (reader.Read())
                    {
                        throw new FormatException("Save JSON must contain exactly one document.");
                    }
                }
            }
            catch (SerializationException exception)
            {
                throw new FormatException("Save JSON is malformed.", exception);
            }
            catch (XmlException exception)
            {
                throw new FormatException("Save JSON is malformed.", exception);
            }

            if (data == null)
            {
                throw new FormatException("Save JSON did not contain an object.");
            }

            if (data.Version == 0)
            {
                return CreateDefault();
            }

            ValidateCurrent(data);
            return data;
        }

        private static void EnsureSingleJsonDocument(string json, string description)
        {
            var index = 0;
            while (index < json.Length && char.IsWhiteSpace(json[index]))
            {
                index++;
            }

            if (index == json.Length || json[index] != '{')
            {
                throw new FormatException($"{description} must contain one object.");
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
                            throw new FormatException($"{description} must contain exactly one document.");
                        }

                        return;
                    }
                }
            }

            throw new FormatException($"{description} is incomplete.");
        }

        private static void ValidateCurrent(SaveData data)
        {
            if (data.Version != CurrentVersion)
            {
                throw new NotSupportedException($"Save version {data.Version} is not supported.");
            }

            if (data.TimerMode < NormalTimerMode || data.TimerMode > OffTimerMode)
            {
                throw new ArgumentException("Timer mode is invalid.", nameof(data));
            }

            if (!IsValidVolume(data.MusicVolume) || !IsValidVolume(data.SfxVolume))
            {
                throw new ArgumentException("Audio volume is invalid.", nameof(data));
            }

            if (!string.Equals(data.Locale, EnglishLocale, StringComparison.Ordinal) &&
                !string.Equals(data.Locale, MalayLocale, StringComparison.Ordinal))
            {
                throw new ArgumentException("Locale is invalid.", nameof(data));
            }
        }

        private static bool IsValidVolume(float volume)
        {
            return !float.IsNaN(volume) && !float.IsInfinity(volume) && volume >= 0f && volume <= 1f;
        }
    }
}
