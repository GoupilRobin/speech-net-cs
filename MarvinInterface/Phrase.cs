using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Marvin
{
    [Serializable]
    public class Phrase
    {
        public string Value;
        public string Text;

        public Phrase(string value, string text)
        {
            Value = value;
            Text = text;
        }
    }
}
