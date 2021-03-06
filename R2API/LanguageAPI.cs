﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using MonoMod.Utils;
using R2API.Utils;
using RoR2;
using SimpleJSON;
using UnityEngine;

namespace R2API {
    /// <summary>
    /// class for language files to load
    /// </summary>
    [R2APISubmodule]
    public static class LanguageAPI {
        public static bool Loaded {
            get; private set;
        }

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void LanguageAwake() {
            if (Loaded) {
                return;
            }

            Loaded = true;

            var languagePaths = Directory.GetFiles(Paths.PluginPath, "*.language", SearchOption.AllDirectories);
            foreach (var path in languagePaths) {
                AddPath(path);
            }

            Language.onCurrentLanguageChanged += OnCurrentLanguageChanged;
        }

        private static void OnCurrentLanguageChanged() {
            var currentLanguage = Language.currentLanguage;
            if (currentLanguage is null)
                return;

            currentLanguage.stringsByToken = currentLanguage.stringsByToken.ReplaceAndAddRange(GenericTokens);
                
            if (LanguageSpecificTokens.TryGetValue(currentLanguage.name, out var languageSpecificDic)) {

                currentLanguage.stringsByToken = currentLanguage.stringsByToken.ReplaceAndAddRange(languageSpecificDic);
            }
        }

        private static Dictionary<string, string> ReplaceAndAddRange(this Dictionary<string, string> dict, Dictionary<string, string> other) {
            dict = dict.Where(kvp => !other.ContainsKey(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            dict.AddRange(other);

            return dict;
        }

        //based upon RoR2.language.LoadTokensFromFile but with specific language support
        private static void LoadCustomTokensFromFile(string file) {
            try {
                JSONNode jsonNode = JSON.Parse(file);
                if (jsonNode == null) {
                    return;
                }

                var genericsAdded = false;
                var languages = jsonNode.Keys;
                foreach (var language in languages) {
                    JSONNode languageTokens = jsonNode[language];
                    if (languageTokens == null) {
                        return;
                    }

                    if (!genericsAdded) {
                        foreach (string text in languageTokens.Keys) {
                            Add(text, languageTokens[text].Value);
                        }
                        genericsAdded = true;
                    }

                    foreach (string text in languageTokens.Keys) {
                        Add(text, languageTokens[text].Value, language);
                    }
                }
            }
            catch (Exception ex) {
                Debug.LogFormat("Parsing error in language file , Error: {0}", ex);
            }
        }

        /// <summary>
        /// Adds a single languagetoken and its associated value to all languages
        /// </summary>
        /// <param name="key">Token the game asks</param>
        /// <param name="value">Value it gives back</param>
        public static void Add(string key, string value) {
            if(!Loaded) {
                throw new InvalidOperationException($"{nameof(LanguageAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(LanguageAPI)})]");
            }
            if (GenericTokens.ContainsKey(key)) {
                GenericTokens[key] = value;
            }
            else {
                GenericTokens.Add(key, value);
            }
        }

        /// <summary>
        /// Adds a single languagetoken and value to a specific language
        /// </summary>
        /// <param name="key">Token the game asks</param>
        /// <param name="value">Value it gives back</param>
        /// <param name="language">Language you want to add this to</param>
        public static void Add(string key, string value, string language) {
            if(!Loaded) {
                throw new InvalidOperationException($"{nameof(LanguageAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(LanguageAPI)})]");
            }
            if (!LanguageSpecificTokens.ContainsKey(language)) {
                LanguageSpecificTokens.Add(language, new Dictionary<string, string>());
            }

            if (LanguageSpecificTokens[language].ContainsKey(key)) {
                R2API.Logger.LogDebug($"Overriding token {key} in {language} dictionary");
                LanguageSpecificTokens[language][key] = value;
            }
            else {
                LanguageSpecificTokens[language].Add(key, value);
            }
        }

        /// <summary>
        /// adding an file via path (.language is added automatically)
        /// </summary>
        /// <param name="path">absolute path to file</param>
        public static void AddPath(string path) {
            if(!Loaded) {
                throw new InvalidOperationException($"{nameof(LanguageAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(LanguageAPI)})]");
            }
            if (File.Exists(path)) {
                Add(File.ReadAllText(path));
            }
        }

        /// <summary>
        /// Adding an file which is read into an string
        /// </summary>
        /// <param name="file">entire file as string</param>
        public static void Add(string file) {
            if(!Loaded) {
                throw new InvalidOperationException($"{nameof(LanguageAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(LanguageAPI)})]");
            }
            LoadCustomTokensFromFile(file);
        }

        /// <summary>
        /// Adds multiple languagetokens and value
        /// </summary>
        /// <param name="tokenDictionary">dictionaries of key-value (eg ["mytoken"]="mystring")</param>
        public static void Add(Dictionary<string, string> tokenDictionary) {
            if(!Loaded) {
                throw new InvalidOperationException($"{nameof(LanguageAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(LanguageAPI)})]");
            }
            foreach (var token in tokenDictionary.Keys) {
                Add(token, tokenDictionary[token]);
            }
        }

        /// <summary>
        /// Adds multiple languagetokens and value to a specific language
        /// </summary>
        /// <param name="tokenDictionary">dictionaries of key-value (eg ["mytoken"]="mystring")</param>
        /// <param name="language">Language you want to add this to</param>
        public static void Add(Dictionary<string, string> tokenDictionary, string language) {
            if(!Loaded) {
                throw new InvalidOperationException($"{nameof(LanguageAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(LanguageAPI)})]");
            }
            foreach (var token in tokenDictionary.Keys) {
                Add(token, tokenDictionary[token], language);
            }
        }

        /// <summary>
        /// Adds multiple languagetokens and value to languages
        /// </summary>
        /// <param name="languageDictionary">dictionary of languages containing dictionaries of key-value (eg ["en"]["mytoken"]="mystring")</param>
        public static void Add(Dictionary<string, Dictionary<string, string>> languageDictionary) {
            if(!Loaded) {
                throw new InvalidOperationException($"{nameof(LanguageAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(LanguageAPI)})]");
            }
            foreach (var language in languageDictionary.Keys) {
                foreach (var token in languageDictionary[language].Keys) {
                    Add(languageDictionary[language][token], token, language);
                }
            }
        }

        internal static Dictionary<string, string> GenericTokens = new Dictionary<string, string>();

        internal static Dictionary<string, Dictionary<string, string>> LanguageSpecificTokens = new Dictionary<string, Dictionary<string, string>>();
    }
}
