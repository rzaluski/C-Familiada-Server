using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Question
    {
        public string QuestionText { get; set; }
        public List<Answer> Answers { get; set; } = new List<Answer>();

        public Question(string _QuestionText)
        {
            QuestionText = _QuestionText;
        }
    }
    public class Questions
    {
        public List<Question> QuestionList { get; set; } = new List<Question>();
    }
}
