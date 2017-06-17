using System;
using System.Collections.Generic;
using System.Text;
using System.Speech.Recognition;
using System.Globalization;
using System.Threading;
using System.Text.RegularExpressions;

namespace Marvin
{
    public class SpeechToText
    {
        // https://regex101.com/r/RVQfAW/1
        private static readonly Regex m_PhraseCompilRegex = new Regex(@"(?<phrase>(?:\ *\w+)+) | (?<any>(?:\.{3})+) | (?<optional>(?:\?\w+)+)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        private static readonly Regex m_SentenceCompilRegex = new Regex(@"(?<optional>(?:\?\w+)+) | (?<optionals>\?\((?:\w+\|?)+\)) | (?<value>(?:\w+)) | (?<values>\((?:\w+\|?)+\)) | (?<any>(?:\.{3})+)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        private SpeechRecognitionEngine m_SpeechEngine;
        private List<Phrase> m_Vocabulary = new List<Phrase>();
        private List<string> m_Sentences = new List<string>();
        private Grammar m_LoadedGrammar;
        private object grammarLock = new object();
        private CultureInfo m_Culture = new CultureInfo("en-US");
        private Thread m_Thread;

        public delegate void SpeechRecognizedEvent(string result);
        public event SpeechRecognizedEvent SpeechRecognized;
        public bool IsListening { get; private set; }

        public SpeechToText()
        {
            IsListening = false;
        }

        public void AddVocabulary(Phrase phrase)
        {
            lock (m_Vocabulary)
            {
                m_Vocabulary.Add(phrase);
            }
        }

        public void AddSentence(string sentence)
        {
            lock (m_Sentences)
            {
                m_Sentences.Add(sentence);
            }
        }

        /// <summary>
        /// Turn a phrase (usually from Phrase.Text) into an array of phrases based
        /// on all possible permutations allowed by the markings
        /// </summary>
        /// <param name="phrase">Phrase string to compile</param>
        /// <returns>An array of all possible permutations of the phrase based on the markings</returns>
        private string[] CompilePhrase(string phrase)
        {
            List<string> expandedTexts = new List<string>();
            MatchCollection matches = m_PhraseCompilRegex.Matches(phrase);
            foreach (Match match in matches)
            {
                string str = "";
                bool optional = false;

                // identify the word, clean it and mark it as optional if needed
                if (match.Groups["phrase"].Length > 0 || match.Groups["any"].Length > 0)
                {
                    str = match.Groups["phrase"].Value;
                    if (str.Length <= 0)
                    {
                        str = match.Groups["any"].Value;
                    }
                }
                else if (match.Groups["optional"].Length > 0)
                {
                    str = match.Groups["optional"].Value;
                    // remove the '?'
                    str = str.Remove(0, 1);
                    optional = true;
                }
                else
                {
                    Console.WriteLine("Error: unknown str found in phrase '" + phrase + "'");
                    continue;
                }

                str = str.Trim();

                // Keep building permutations with the new word
                if (expandedTexts.Count <= 0)
                {
                    expandedTexts.Add(str);
                    if (optional)
                    {
                        expandedTexts.Add("");
                    }
                }
                else
                {
                    string[] copy = null;

                    if (optional)
                    {
                        copy = expandedTexts.ToArray();
                    }

                    // append the text to the existing str
                    for (int i = 0; i < expandedTexts.Count; ++i)
                    {
                        if (!expandedTexts[i].EndsWith(" ")) expandedTexts[i] += " ";
                        expandedTexts[i] += str;
                    }

                    // if optional, duplicate the current texts so they exists without the optional str
                    if (copy != null && copy.Length > 0)
                    {
                        expandedTexts.AddRange(copy);
                    }
                }
            }

            // remove all empty elements
            expandedTexts.RemoveAll(elem => elem.Length <= 0);

            return expandedTexts.ToArray();
        }

        private string[] CompileSentence(string sentence)
        {
            List<string> splitted = new List<string>();

            // remove '\n' and '\t'
            sentence = Regex.Replace(sentence, @"[\n\r\t]", string.Empty);
            // normalize spaces
            sentence = Regex.Replace(sentence, @"\|[ ]+", "|");
            sentence = Regex.Replace(sentence, @"[ ]{2,}", " ");

            MatchCollection matches = m_SentenceCompilRegex.Matches(sentence);
            foreach (Match match in matches)
            {
                List<string> strs = new List<string>();
                bool optional = false;

                // identify the word or words, clean them and mark them as optional if needed
                if (match.Groups["value"].Length > 0 || match.Groups["any"].Length > 0)
                {
                    string str = match.Groups["value"].Value;
                    if (str.Length <= 0)
                    {
                        str = match.Groups["any"].Value;
                    }
                    strs.Add(str);
                }
                else if (match.Groups["optional"].Length > 0)
                {
                    string str = match.Groups["optional"].Value;
                    str = str.Remove(0, 1);
                    optional = true;
                    strs.Add(str);
                }
                else if (match.Groups["values"].Length > 0)
                {
                    string str = match.Groups["values"].Value;
                    // remove the '(' and ')'
                    str = str.Substring(1, str.Length - 2);

                    strs.AddRange(str.Split('|'));
                }
                else if (match.Groups["optionals"].Length > 0)
                {
                    string str = match.Groups["optionals"].Value;
                    // remove the '?(' and ')'
                    str = str.Substring(2, str.Length - 2);
                    optional = true;

                    strs.AddRange(str.Split('|'));
                }
                else
                {
                    Console.WriteLine("Error: unknown str found in sentence '" + sentence + "'");
                    continue;
                }

                if (splitted.Count <= 0)
                {
                    splitted.AddRange(strs);
                    if (optional)
                    {
                        splitted.Add("");
                    }
                }
                else
                {
                    string[] copy = splitted.ToArray();

                    // keep the original string if the word is optional
                    // (small optimisation, simply don't clear instead of adding the copy again)
                    if (!optional)
                    {
                        splitted.Clear();
                    }
                    
                    foreach (string str in strs)
                    {
                        foreach (string existingStr in copy)
                        {
                            string newSentence = existingStr;
                            if (!newSentence.EndsWith(" ")) newSentence += " ";
                            newSentence += str;
                            splitted.Add(newSentence);
                        }
                    }
                }
            }

            return splitted.ToArray();
        }

        private void UpdateGrammar()
        {
            Thread.CurrentThread.CurrentUICulture = m_Culture;

            Dictionary<string, List<string>> expandedPhrases = new Dictionary<string, List<string>>();

            foreach (Phrase phrase in m_Vocabulary)
            {
                string[] compiledPhrase = CompilePhrase(phrase.Text);
                string key = phrase.Value;

                if (!expandedPhrases.ContainsKey(key))
                {
                    expandedPhrases.Add(key, new List<string>());
                }

                expandedPhrases[key].AddRange(compiledPhrase);
            }

            List<string[]> splittedSentences = new List<string[]>();
            foreach (string sentence in m_Sentences)
            {
                string[] compiledSentences = CompileSentence(sentence);

                foreach (string compiledSentence in compiledSentences)
                {
                    string[] split = compiledSentence.Split(' ');
                    splittedSentences.Add(split);
                }
            }

            Choices rootChoices = new Choices();
            Dictionary<string, SemanticResultValue> semanticValues = new Dictionary<string, SemanticResultValue>();
            int choichesCount = 0;

            foreach (string[] split in splittedSentences)
            {
                GrammarBuilder sentenceGrammar = new GrammarBuilder();
                sentenceGrammar.Culture = m_Culture;
                int i = 0;

                foreach (string value in split)
                {
                    if (value == "...")
                    {
                        sentenceGrammar.AppendWildcard();
                        choichesCount++;
                    }
                    else
                    {
                        // add all potential words for that given value
                        Choices wordChoices = new Choices();

                        if (expandedPhrases.ContainsKey(value))
                        {
                            foreach (string word in expandedPhrases[value])
                            {
                                wordChoices.Add(word);
                                choichesCount++;
                            }
                        }
                        else
                        {
                            // ignore unknown words for now as they are not interesting
                            wordChoices.Add(value);
                            choichesCount++;
                        }

                        // assign the value to get the phrase backed from the speech engine
                        // TODO: skip word not part of Phrase in semantic
                        SemanticResultValue choiceSemanticValue;
                        if (semanticValues.ContainsKey(value))
                        {
                            choiceSemanticValue = semanticValues[value];
                        }
                        else
                        {
                            choiceSemanticValue = new SemanticResultValue(wordChoices, value);
                            semanticValues[value] = choiceSemanticValue;
                        }

                        SemanticResultKey choiceSemanticKey = new SemanticResultKey(value + "_" + i, choiceSemanticValue);

                        sentenceGrammar.Append(choiceSemanticKey);
                        i++;
                    }
                }

                rootChoices.Add(sentenceGrammar);
            }

            if (m_LoadedGrammar != null)
            {
                m_SpeechEngine.UnloadGrammar(m_LoadedGrammar);
            }

            GrammarBuilder rootGrammar = new GrammarBuilder(rootChoices);
            rootGrammar.Culture = m_Culture;

            m_LoadedGrammar = new Grammar(rootGrammar);
            m_LoadedGrammar.Name = "Command";

            m_SpeechEngine.UnloadAllGrammars();

            // Actual client grammar
            m_SpeechEngine.LoadGrammar(m_LoadedGrammar);

            Console.WriteLine("Loaded " + splittedSentences.Count + " sentences (" + choichesCount + " choices)");
        }

        public void Start()
        {
            if (IsListening) return;

            if (m_Thread == null)
            {
                m_Thread = new Thread(() =>
                {
                    m_SpeechEngine = new SpeechRecognitionEngine(m_Culture);
                    m_SpeechEngine.MaxAlternates = 10;

                    UpdateGrammar();

                    m_SpeechEngine.SpeechRecognized += OnSpeechRecognized;
                    m_SpeechEngine.SetInputToDefaultAudioDevice();
                    m_SpeechEngine.RecognizeAsync(RecognizeMode.Multiple);
                    IsListening = true;
                });
                m_Thread.Start();
            }
            else
            {
                lock (m_SpeechEngine)
                {
                    m_SpeechEngine.SpeechRecognized += OnSpeechRecognized;
                    m_SpeechEngine.SetInputToDefaultAudioDevice();
                    m_SpeechEngine.RecognizeAsync(RecognizeMode.Multiple);
                    IsListening = true;
                }
            }
        }

        public void Stop()
        {
            lock (m_SpeechEngine)
            {
                m_SpeechEngine.SpeechRecognized -= OnSpeechRecognized;
                m_SpeechEngine.RecognizeAsyncStop();
                IsListening = false;
            }
        }

        private void OnSpeechRecognized(object sender, SpeechRecognizedEventArgs arg)
        {
            if (arg.Result.Confidence <= 0.6 || arg.Result.Grammar.Name != "Command") return;

            string semanticStr = "";
            foreach (var semanticKvp in arg.Result.Semantics)
            {
                SemanticValue semantic = semanticKvp.Value;
                if (semanticStr.Length > 0) semanticStr += " ";
                semanticStr += semantic.Value;
            }

            SpeechRecognized?.Invoke(semanticStr);
        }
    }
}
