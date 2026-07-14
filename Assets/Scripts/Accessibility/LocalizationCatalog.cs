using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

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

            return $"[[missing:{key ?? "<null>"}]]";
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
                var serializer = new DataContractJsonSerializer(typeof(CatalogDocument));
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    document = serializer.ReadObject(stream) as CatalogDocument;
                }
            }
            catch (SerializationException exception)
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
                if (entry == null || string.IsNullOrWhiteSpace(entry.Key) || entry.Value == null)
                {
                    throw new FormatException("Catalog entries require non-null keys and values.");
                }

                if (entries.ContainsKey(entry.Key))
                {
                    throw new FormatException($"Duplicate localization key '{entry.Key}'.");
                }

                entries.Add(entry.Key, entry.Value);
            }

            return new LocaleCatalog(document.Locale, document.TranslationStatus, entries);
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
