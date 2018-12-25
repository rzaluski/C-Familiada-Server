using FamiliadaClientForms;
using System;
using System.Collections.Generic;
using System.Drawing;
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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
            Left,
            Right,
            None
        }

        private TcpListener tcpListener;
        private Socket socket;

        private List<Question> _questions = new List<Question>();
        private Question _currentQuestion;
        private bool _isRoundOn;
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
            PlaySound(Properties.Resources.intro);
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
            _round++;
            _correctAnswers = 0;
            _incorrectAnswers = 0;
            _totalAnswers = 0;
            _currentAnsweringTeam = Team.None;
            _roundPoints = 0;
            _teamWonBattle = Team.None;
            _isRoundOn = true;
            PlaySound(Properties.Resources.roundsound);
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
                _currentQuestion = q;
            }
            else if(msg.MessageType == "SubmitQuestion")
            {
                NewQuestion(_currentQuestion);
            }
            else if(msg.MessageType == "FirstAnsweringTeam")
            {
                _currentAnsweringTeam = (Team)JMessage.Deserialize<int>(msg.ObjectJson);
                SendMessage("Confirm", null);
            }
            else if(msg.MessageType == "CorrectAnswer")
            {
                Answer answer = JMessage.Deserialize<Answer>(msg.ObjectJson);
                ProceedCorrectAnswer(GetAnswerNumber(answer));
            }
            else if(msg.MessageType == "IncorrectAnswer")
            {
                ProceedUncorrectAnswer();
            }
        }

        private int GetAnswerNumber(Answer answer)
        {
            return _currentQuestion.Answers.FindIndex(c => c.AnswerText == answer.AnswerText);
        }
        private void ShowAnswer(int number)
        {
            Dispatcher.Invoke(() =>
            {
                Label labelAnswer = (Label)((DockPanel)dockPanelAnswers.Children[number]).Children[2];
                labelAnswer.Content = _currentQuestion.Answers[number].AnswerText.ToUpper();
                DoubleAnimation answerAnimation = new DoubleAnimation();
                answerAnimation.From = 0;
                answerAnimation.To = labelAnswer.ActualWidth;
                answerAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(500));

                Storyboard.SetTarget(answerAnimation, labelAnswer);
                Storyboard.SetTargetProperty(answerAnimation, new PropertyPath(Label.WidthProperty));

                Storyboard myWidthAnimatedLabelStoryboard = new Storyboard();
                myWidthAnimatedLabelStoryboard.Children.Add(answerAnimation);
                myWidthAnimatedLabelStoryboard.Begin(labelAnswer);

                Label labelPoints = (Label)((Grid)((DockPanel)dockPanelAnswers.Children[number]).Children[1]).Children[0];
                labelPoints.Content = _currentQuestion.Answers[number].Points;
                DoubleAnimation pointsAnimation = new DoubleAnimation();
                pointsAnimation.From = 0;
                pointsAnimation.To = labelPoints.ActualWidth;
                pointsAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(200));
                pointsAnimation.BeginTime = TimeSpan.FromMilliseconds(500);
                Storyboard.SetTarget(pointsAnimation, labelPoints);
                Storyboard.SetTargetProperty(pointsAnimation, new PropertyPath(Label.WidthProperty));

                labelPoints.Width = 0;
                myWidthAnimatedLabelStoryboard.Children.Add(pointsAnimation);
                myWidthAnimatedLabelStoryboard.Begin(labelPoints);
            });
        }
        private void ProceedCorrectAnswer(int answerNumber)
        {
            ShowAnswer(answerNumber);
            PlaySound(Properties.Resources.correctanswer);
            if(_isRoundOn)
            {
                PlayClapsWithDelay(1000);
                _totalAnswers++;
                _correctAnswers++;
                int pointsToAdd = _currentQuestion.Answers[answerNumber].Points * _pointsMultiplier;
                _roundPoints += pointsToAdd;
                if (_totalAnswers == 1 && answerNumber == 0)
                {
                    _teamWonBattle = _currentAnsweringTeam;
                }
                else if (_totalAnswers == 1 && answerNumber > 0)
                {
                    _currentAnsweringTeam = GetOppositeTeam(_currentAnsweringTeam);
                }
                else if (_teamWonBattle == Team.None && _totalAnswers == 2)
                {
                    if (_roundPoints - pointsToAdd > pointsToAdd)
                    {
                        _teamWonBattle = GetOppositeTeam(_currentAnsweringTeam);
                    }
                    else
                    {
                        _teamWonBattle = _currentAnsweringTeam;
                    }
                    ClearXPanels();
                }
                else if (_teamWonBattle == Team.None)
                {
                    _teamWonBattle = _currentAnsweringTeam;
                    ClearXPanels();
                }
                else if (_correctAnswers == _currentQuestion.Answers.Count || _currentAnsweringTeam != _teamWonBattle)
                {
                    _roundPoints -= pointsToAdd;
                    EndRound(_currentAnsweringTeam);
                }
                SendMessage("IsRoundOn", _isRoundOn);
                UpdatePoints();
            }
        }

        private void ClearXPanels()
        {
            Dispatcher.Invoke(() =>
            {
                stackPanelLeftX.Children.Clear();
                stackPanelRightX.Children.Clear();
            });
        }

        private void UpdatePoints()
        {
            Dispatcher.Invoke(() =>
            {
                labelPointsTop.Content = _roundPoints;
                labelPointsLeft.Content = _teamPoints[(int)Team.Left];
                labelPointsRight.Content = _teamPoints[(int)Team.Right];
            });
        }

        private void ProceedUncorrectAnswer()
        {
            Team teamAnswered = _currentAnsweringTeam;
            _totalAnswers++;
            if(_teamWonBattle != Team.None)
            {
                _incorrectAnswers++;
            }
            if(_teamWonBattle != Team.None && _currentAnsweringTeam == _teamWonBattle)
            {
                ShowSmallX(teamAnswered);
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
                EndRound(GetOppositeTeam(teamAnswered));
                UpdatePoints();
            }
            SendMessage("IsRoundOn", _isRoundOn);
            PlaySound(Properties.Resources.wronganswer);
        }

        private void ShowFullX()
        {
            Dispatcher.Invoke(()=>
            {
                System.Windows.Controls.Image img = new System.Windows.Controls.Image();
                img.Source = Utils.ImageSourceForBitmap(Properties.Resources.bigX);
                img.Stretch = Stretch.None;
                img.VerticalAlignment = VerticalAlignment.Top;
                if (_currentAnsweringTeam == Team.Left)
                {
                    stackPanelLeftX.Children.Clear();
                    Grid grid = new Grid();
                    grid.Height = Properties.Resources.bigX.Height;
                    grid.Children.Add(img);
                    stackPanelLeftX.Children.Add(grid);
                    stackPanelLeftX.VerticalAlignment = VerticalAlignment.Center;
                }
                else
                {
                    stackPanelRightX.Children.Clear();
                    Grid grid = new Grid();
                    grid.Height = Properties.Resources.bigX.Height;
                    grid.Children.Add(img);
                    stackPanelRightX.Children.Add(grid);
                    stackPanelRightX.VerticalAlignment = VerticalAlignment.Center;
                }
                DoubleAnimation imgAnimation = new DoubleAnimation();
                imgAnimation.From = 0;
                imgAnimation.To = Properties.Resources.bigX.Height;
                imgAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(300));

                Storyboard.SetTarget(imgAnimation, img);
                Storyboard.SetTargetProperty(imgAnimation, new PropertyPath(System.Windows.Controls.Image.HeightProperty));

                Storyboard imgStoryboard = new Storyboard();
                imgStoryboard.Children.Add(imgAnimation);
                imgStoryboard.Begin(img);
            });
        }

        private void ShowSmallX(Team teamAnswered)
        {
            Dispatcher.Invoke(() =>
            {
                StackPanel stackPanel = teamAnswered == Team.Left ? stackPanelLeftX : stackPanelRightX;
                if (stackPanel.Children.Count == 0)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Grid grid = new Grid();
                        grid.Height = Properties.Resources.smallX.Height;
                        grid.VerticalAlignment = VerticalAlignment.Center;
                        grid.Margin = new Thickness() { Bottom = 10 };
                        stackPanel.Children.Add(grid);
                    }

                }
                System.Windows.Controls.Image img = new System.Windows.Controls.Image();
                img.Source = Utils.ImageSourceForBitmap(Properties.Resources.smallX);
                img.Stretch = Stretch.None;
                img.VerticalAlignment = VerticalAlignment.Top;
                ((Grid)stackPanel.Children[_incorrectAnswers - 1]).Children.Add(img);

                DoubleAnimation imgAnimation = new DoubleAnimation();
                imgAnimation.From = 0;
                imgAnimation.To = Properties.Resources.smallX.Height;
                imgAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(300));

                Storyboard.SetTarget(imgAnimation, img);
                Storyboard.SetTargetProperty(imgAnimation, new PropertyPath(System.Windows.Controls.Image.HeightProperty));

                Storyboard imgStoryboard = new Storyboard();
                imgStoryboard.Children.Add(imgAnimation);
                imgStoryboard.Begin(img);
            });
        }


        private void PlaySound(System.IO.Stream sound)
        {
            System.Media.SoundPlayer snd = new System.Media.SoundPlayer(sound);
            snd.Play();
        }

        private void PlaySoundWithDelay(System.IO.Stream sound, int delayMiliseconds)
        {
            new Task(() =>
            {
                System.Threading.Thread.Sleep(delayMiliseconds);
                PlaySound(sound);
            }).Start();
        }

        private void PlayClapsWithDelay(int delayMiliseconds)
        {
            new Task(() =>
            {
                System.Threading.Thread.Sleep(delayMiliseconds);
                System.IO.Stream str = Properties.Resources.claps;
                System.Media.SoundPlayer snd = new System.Media.SoundPlayer(str);
                snd.Play();
            }).Start();
        }


        private void EndRound(Team winningTeam)
        {
            _teamPoints[(int)winningTeam] += _roundPoints;
            _roundPoints = 0;
            _isRoundOn = false;
            PlaySoundWithDelay(Properties.Resources.roundsound, 2000);
        }

        private Team GetOppositeTeam(Team currentTeam)
        {
            return currentTeam == Team.Left ? Team.Right : Team.Left;
        }

        private void NewQuestion(Question q)
        {
            Dispatcher.Invoke(() =>
            {
                if(stackPanelLogoText.Visibility == Visibility.Visible)
                {
                    stackPanelLogoText.Visibility = Visibility.Collapsed;
                    dockPanelMainContent.Visibility = Visibility.Visible;
                }

                dockPanelAnswers.Children.Clear();
                ClearXPanels();

                for (int i = 0; i < _currentQuestion.Answers.Count; i++)
                {
                    DockPanel dockPanel = new DockPanel();

                    Label labelNumber = new Label();
                    labelNumber.Width = 75;
                    labelNumber.Content = i + 1;
                    labelNumber.Style = (Style)FindResource("answers");
                    DockPanel.SetDock(labelNumber, Dock.Left);

                    Grid pointsContainer = new Grid();
                    pointsContainer.Width = 100;
                    Label labelPoints = new Label();
                    labelPoints.Width = 100;
                    labelPoints.Content = "--";
                    labelPoints.Style = (Style)FindResource("answers");
                    labelPoints.HorizontalAlignment = HorizontalAlignment.Left;
                    pointsContainer.Children.Add(labelPoints);
                    DockPanel.SetDock(pointsContainer, Dock.Right);

                    Label labelAnswer = new Label();
                    labelAnswer.Style = (Style)FindResource("answers");
                    labelAnswer.Content = "................................................";
                    labelAnswer.HorizontalAlignment = HorizontalAlignment.Left;

                    dockPanel.Children.Add(labelNumber);
                    dockPanel.Children.Add(pointsContainer);
                    dockPanel.Children.Add(labelAnswer);

                    DockPanel.SetDock(dockPanel, Dock.Top);
                    dockPanelAnswers.Children.Add(dockPanel);
                }
            });
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
