using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Marvin
{
    [Serializable]
    public class HandshakeRequest
    {
        public List<Phrase> Phrases { get; private set; }
        public List<string> Sentences { get; private set; }

        public HandshakeRequest(List<Phrase> phrases, List<string> sentences)
        {
            Phrases = phrases;
            Sentences = sentences;
        }
    }
}
