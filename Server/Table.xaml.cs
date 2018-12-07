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
            None,
            First,
            Second
        }

        private TcpListener tcpListener;
        private Socket socket;

        private List<Question> _questions = new List<Question>();
        private Question _currentQuestion;
        private Team _firstAnsweringTeam = Team.None;
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
                //TableCommand tableCommand = JMessage.Deserialize<TableCommand>(msg.ObjectJson);
                Random r = new Random();
                Question q = _questions[r.Next() % _questions.Count];
                SendMessage("RandQuestion", q);
                NewQuestionOnTable(q);
            }
            else if(msg.MessageType == "FirstAnsweringTeam")
            {
                _firstAnsweringTeam = JMessage.Deserialize<Team>(msg.ObjectJson);
            }
            else if(msg.MessageType == "Answer")
            {
                int answerNumber = JMessage.Deserialize<int>(msg.ObjectJson);
                if(answerNumber == -1)
                {

                }
                else
                {
                    ShowAnswerOnTable(answerNumber);
                }
            }
        }

        private void ShowAnswerOnTable(int answerNumber)
        {
            ((Label)stackPanelAnswers.Children[answerNumber]).Content = _currentQuestion.Answers[answerNumber].AnswerText;
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
