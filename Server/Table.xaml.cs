using FamiliadaClientForms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Server
{
    /// <summary>
    /// Interaction logic for Table.xaml
    /// </summary>
    public partial class Table : Page
    {
        private enum Team
        {
            First,
            Second,
            None
        }

        private TcpListener tcpListener;
        private Socket socket;

        private List<Question> _questions = new List<Question>();
        private Question _currentQuestion;
        private int _correctAnswers;
        private int _roundPoints;
        private int _round = 0;
        private int _pointsMultiplier = 1;
        private int _totalAnswers;
        private int _incorrectAnswers;
        private List<int> _teamPoints = new List<int>{0, 0};
        private Team _teamWonBattle;
        private Team _currentAnsweringTeam;
        public Table(TcpListener _tcpListener, Socket _socket)
        {
            InitializeComponent();
            tcpListener = _tcpListener;
            socket = _socket;
            LoadQuestions();
            Task task = new Task(ListenToClient);
            task.Start();

            //StopListening();
        }

        private void LoadQuestions()
        {
            try
            {
                string filename = @"questions.json";
                string json = File.ReadAllText(filename);
                _questions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Question>>(json);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void NewRound()
        {
            _correctAnswers = 0;
            _incorrectAnswers = 0;
            _totalAnswers = 0;
            _currentAnsweringTeam = Team.None;
            _roundPoints = 0;
            _teamWonBattle = Team.None;
        }

        private void ListenToClient()
        {
            byte[] b = new byte[1000];
            while(true)
            {
                int k = socket.Receive(b);
                string msgString = System.Text.Encoding.ASCII.GetString(b, 0, k);
                HandleMessage(msgString);
            }
        }

        private void HandleMessage(string msgString)
        {
            JMessage msg = JMessage.Deserialize(msgString);
            if (msg.MessageType == "RandQuestion")
            {
                Random r = new Random();
                Question q = _questions[r.Next() % _questions.Count];
                SendMessage("RandQuestion", q);
                NewQuestionOnTable(q);
            }
            else if(msg.MessageType == "FirstAnsweringTeam")
            {
                _currentAnsweringTeam = JMessage.Deserialize<Team>(msg.ObjectJson);
            }
            else if(msg.MessageType == "Answer")
            {
                int answerNumber = JMessage.Deserialize<int>(msg.ObjectJson);
                if(answerNumber == -1)
                {
                    ProceedUncorrectAnswer();
                }
                else
                {
                    ProceedCorrectAnswer(answerNumber);
                }
            }
        }

        private void ProceedCorrectAnswer(int answerNumber)
        {
            _totalAnswers++;
            _correctAnswers++;
            ((Label)stackPanelAnswers.Children[answerNumber]).Content = _currentQuestion.Answers[answerNumber].AnswerText;
            int pointsToAdd = _currentQuestion.Answers[answerNumber].Points * _pointsMultiplier;
            _roundPoints += pointsToAdd;
            if(_totalAnswers == 1 && answerNumber == 0)
            {
                _teamWonBattle = _currentAnsweringTeam;
            }
            else if(_totalAnswers == 1 && answerNumber > 0)
            {
                _currentAnsweringTeam = GetOppositeTeam(_currentAnsweringTeam);
            }
            else if(_teamWonBattle == Team.None && _totalAnswers == 2)
            {
                if(_roundPoints - pointsToAdd > _roundPoints)
                {
                    _teamWonBattle = _currentAnsweringTeam;
                }
                else
                {
                    _teamWonBattle = GetOppositeTeam(_currentAnsweringTeam);
                }
            }
            else if(_teamWonBattle == Team.None)
            {
                _teamWonBattle = _currentAnsweringTeam;
            }
            else if(_correctAnswers == _currentQuestion.Answers.Count || _currentAnsweringTeam != _teamWonBattle)
            {
                EndRound(_currentAnsweringTeam);
            }
        }

        private void ProceedUncorrectAnswer()
        {
            _totalAnswers++;
            _incorrectAnswers++;
            if(_teamWonBattle != Team.None && _currentAnsweringTeam == _teamWonBattle)
            {
                ShowSmallX();
            }
            if(_teamWonBattle == Team.None || _currentAnsweringTeam != _teamWonBattle)
            {
                ShowFullX();
                _currentAnsweringTeam = GetOppositeTeam(_currentAnsweringTeam);
            }
            if(_incorrectAnswers == 3)
            {
                _currentAnsweringTeam = GetOppositeTeam(_currentAnsweringTeam);
            }
            if(_incorrectAnswers == 4)
            {
                EndRound(GetOppositeTeam(_currentAnsweringTeam));
            }
        }

        private void ShowFullX()
        {
        }

        private void ShowSmallX()
        {
        }

        private void EndRound(Team winningTeam)
        {
            _teamPoints[(int)winningTeam] += _roundPoints;
        }

        private Team GetOppositeTeam(Team currentTeam)
        {
            return currentTeam == Team.First ? Team.Second : Team.First;
        }

        private void NewQuestionOnTable(Question q)
        {
            _currentQuestion = q;
            for(int i=0; i < _currentQuestion.Answers.Count; i++)
            {
                Label l = new Label();
                l.Content = i.ToString() + " ........................";
                stackPanelAnswers.Children.Add(l);
            }
            NewRound();
        }

        private void SendMessage(string header, object obj)
        {
            string msgString = JMessage.CreateMessage(header, obj);
            ASCIIEncoding asen = new ASCIIEncoding();
            byte[] b = asen.GetBytes(msgString);
            socket.Send(b);
        }

        private void StopListening()
        {
            socket.Close();
            tcpListener.Stop();
        }
    }
}
