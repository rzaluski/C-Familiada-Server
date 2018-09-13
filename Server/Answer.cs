using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Answer
    {
        public string AnswerText { get; set; }
        public int Points { get; set; }

        public Answer(string _AnswerText, int _Points)
        {
            AnswerText = _AnswerText;
            Points = _Points;
        }
    }
}
