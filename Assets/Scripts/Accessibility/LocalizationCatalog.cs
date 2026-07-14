using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace Agrovator.PitchSimulator.Accessibility
{
    public sealed class LocalizationCatalog
    {
        private const string EnglishLocale = "en";

        private readonly IReadOnlyDictionary<string, LocaleCatalog> _catalogs;

        private LocalizationCatalog(IReadOnlyDictionary<string, LocaleCatalog> catalogs)
        {
            _catalogs = catalogs;
        }

        public static LocalizationCatalog Load(string englishJson, params string[] localizedJson)
        {
            var catalogs = new Dictionary<string, LocaleCatalog>(StringComparer.Ordinal);
            var english = Parse(englishJson);
            if (!string.Equals(english.Locale, EnglishLocale, StringComparison.Ordinal))
            {
                throw new FormatException("The canonical catalog locale must be 'en'.");
            }

            Add(catalogs, english);
            if (localizedJson != null)
            {
                foreach (var json in localizedJson)
                {
                    Add(catalogs, Parse(json));
                }
            }

            return new LocalizationCatalog(catalogs);
        }

        public string Resolve(string locale, string key)
        {
            var catalog = GetCatalog(locale);
            if (key != null && catalog.Entries.TryGetValue(key, out var value))
            {
                return value;
            }

            var english = _catalogs[EnglishLocale];
            if (!ReferenceEquals(catalog, english) && key != null && english.Entries.TryGetValue(key, out value))
            {
                return value;
            }

            return $"[[missing:{EscapeDiagnosticKey(key)}]]";
        }

        public IReadOnlyCollection<string> GetKeys(string locale)
        {
            return GetCatalog(locale).Keys;
        }

        public string GetTranslationStatus(string locale)
        {
            return GetCatalog(locale).TranslationStatus;
        }

        private LocaleCatalog GetCatalog(string locale)
        {
            return locale != null && _catalogs.TryGetValue(locale, out var catalog)
                ? catalog
                : _catalogs[EnglishLocale];
        }

        private static void Add(IDictionary<string, LocaleCatalog> catalogs, LocaleCatalog catalog)
        {
            if (catalogs.ContainsKey(catalog.Locale))
            {
                throw new FormatException($"Duplicate locale '{catalog.Locale}'.");
            }

            catalogs.Add(catalog.Locale, catalog);
        }

        private static LocaleCatalog Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new FormatException("Catalog JSON is required.");
            }

            CatalogDocument document;
            try
            {
                EnsureSingleJsonDocument(json, "Catalog JSON");
                var serializer = new DataContractJsonSerializer(typeof(CatalogDocument));
                using (var reader = JsonReaderWriterFactory.CreateJsonReader(
                    Encoding.UTF8.GetBytes(json),
                    XmlDictionaryReaderQuotas.Max))
                {
                    document = serializer.ReadObject(reader) as CatalogDocument;
                    if (reader.Read())
                    {
                        throw new FormatException("Catalog JSON must contain exactly one document.");
                    }
                }
            }
            catch (SerializationException exception)
            {
                throw new FormatException("Catalog JSON is malformed.", exception);
            }
            catch (XmlException exception)
            {
                throw new FormatException("Catalog JSON is malformed.", exception);
            }

            if (document == null || string.IsNullOrWhiteSpace(document.Locale) ||
                string.IsNullOrWhiteSpace(document.TranslationStatus) || document.Entries == null)
            {
                throw new FormatException("Catalog locale, translation status and entries are required.");
            }

            var entries = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entry in document.Entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Key) ||
                    string.IsNullOrWhiteSpace(entry.Value))
                {
                    throw new FormatException("Catalog entries require non-empty keys and values.");
                }

                if (entries.ContainsKey(entry.Key))
                {
                    throw new FormatException($"Duplicate localization key '{entry.Key}'.");
                }

                entries.Add(entry.Key, entry.Value);
            }

            return new LocaleCatalog(document.Locale, document.TranslationStatus, entries);
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

        private static string EscapeDiagnosticKey(string key)
        {
            var source = key ?? "<null>";
            var escaped = new StringBuilder(source.Length);
            foreach (var character in source)
            {
                if (IsSafeKeyCharacter(character))
                {
                    escaped.Append(character);
                    continue;
                }

                escaped.Append("\\u");
                escaped.Append(((int)character).ToString("X4", CultureInfo.InvariantCulture));
            }

            return escaped.ToString();
        }

        private static bool IsSafeKeyCharacter(char character)
        {
            return (character >= 'a' && character <= 'z') ||
                (character >= 'A' && character <= 'Z') ||
                (character >= '0' && character <= '9') ||
                character == '.' || character == '_' || character == '-';
        }

        [DataContract]
        private sealed class CatalogDocument
        {
            [DataMember(Name = "locale", Order = 0)]
            public string Locale;

            [DataMember(Name = "translationStatus", Order = 1)]
            public string TranslationStatus;

            [DataMember(Name = "entries", Order = 2)]
            public CatalogEntry[] Entries;
        }

        [DataContract]
        private sealed class CatalogEntry
        {
            [DataMember(Name = "key", Order = 0)]
            public string Key;

            [DataMember(Name = "value", Order = 1)]
            public string Value;
        }

        private sealed class LocaleCatalog
        {
            public LocaleCatalog(string locale, string translationStatus, Dictionary<string, string> entries)
            {
                Locale = locale;
                TranslationStatus = translationStatus;
                Entries = entries;
                Keys = new List<string>(entries.Keys).AsReadOnly();
            }

            public string Locale { get; }

            public string TranslationStatus { get; }

            public IReadOnlyDictionary<string, string> Entries { get; }

            public IReadOnlyCollection<string> Keys { get; }
        }
    }
}
