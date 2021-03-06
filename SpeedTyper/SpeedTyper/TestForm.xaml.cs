﻿using SpeedTyperDataObjects;
using SpeedTyperLogicLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace SpeedTyper
{
    /// <summary>
    /// Interaction logic for TestForm.xaml
    /// </summary>
    public partial class TestForm : Window
    {
        private const int StartTimerCountdown = 5; // Test will start after 5 seconds
        private const int EndTimerCountdown = 120; // Test will end after 2 minutes.

        private DispatcherTimer beginTestTimer;
        private DispatcherTimer timeElapsedTimer; // This timer tracks how long the test has been.

        private int timeLeftUntilTestStart;
        private int secondsLeft; // int of seconds the user has left to complete test
        private int secondsElapsed; // int of seconds the user has been taking test
        private bool testInProgress = false;

        private List<String> lstTestDataText;
        private List<String> correctWords;

        private User _user;
        private TestData testData;
        UserManager _userManager;
        TestManager _testManager;
        LevelManager _levelManager;
        RankManager _rankManager;
        public TestForm(User user, UserManager userManager, TestManager testManager, LevelManager levelManager, RankManager rankManager)
        {
            _user = user;
            _testManager = testManager;
            _userManager = userManager;
            _levelManager = levelManager;
            _rankManager = rankManager;
            InitializeComponent();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            btnStart.IsEnabled = false;
            timeLeftUntilTestStart = StartTimerCountdown; // Start a new timer based off constant
            beginTestTimer = new DispatcherTimer();
            beginTestTimer.Tick += new EventHandler(beginTestTimer_Tick);
            beginTestTimer.Interval = new TimeSpan(0, 0, 1); // Ticks every 1 second.
            beginTestTimer.Start();
        }



        private void beginTestTimer_Tick(object sender, EventArgs e)
        {
            if (timeLeftUntilTestStart == 0)
            {
                beginTestTimer.Stop();
                StartTest();
            }
            else
            {
                txtTextEntryBox.Text = "Test Starting In..." + timeLeftUntilTestStart;
            }
            timeLeftUntilTestStart--;
        }

        private void StartTest()
        {
            // Start the test
            testInProgress = true;
            txtTextEntryBox.Clear();
            txtTextEntryBox.IsEnabled = true;
            txtTextEntryBox.Focus();
            try
            {
                testData = _testManager.RetrieveRandomTest();
            }
            catch (Exception ex)
            {
                MessageWindow.Show(this, "Error:", ex.Message);
                return;
            }

            if(testData.TestDataText != null)
            {
                lstTestDataText = Regex.Split(testData.TestDataText, @"(?<=[ ])").ToList();
            } else
            {
                MessageWindow.Show(this, "Error:", "Can't load Test Data!");
                return;
            }

            lstTestDataText = Regex.Split(testData.TestDataText, @"(?<=[ ])").ToList();
            correctWords = new List<String>();

            txtTestData.Clear();
            foreach (String s in lstTestDataText)
            {
                txtTestData.Text += s;
            }
            lblDataSource.Content = testData.DataSource;
            timeElapsedTimer = new DispatcherTimer();
            timeElapsedTimer.Tick += new EventHandler(timeElapsedTimer_Tick);
            timeElapsedTimer.Interval = new TimeSpan(0, 0, 1); // Ticks every 1 ms.
            timeElapsedTimer.Start();
            secondsLeft = EndTimerCountdown;
            secondsElapsed = 0;
            lblTimeLeft.Content = _testManager.SecondsToTimeSpanFormatter(secondsLeft);
        }

        private void EndTest()
        {
            testInProgress = false;
            txtTextEntryBox.Clear();
            txtTextEntryBox.IsEnabled = false;
            btnStart.IsEnabled = true;
            timeElapsedTimer.Stop();
            SubmitTestConfirmation();
        }

        private void SubmitTestConfirmation()
        {
            if (_user.IsGuest)
            {
                var acctResult = MessageWindow.Show(this, "Alert:", "You must have an account to submit a test. Would you like to create an account?", MessageBoxType.YESNO);
                if (acctResult == true)
                {
                    AccountCreateForm accountCreateForm = new AccountCreateForm();
                    accountCreateForm.Owner = this;
                    accountCreateForm.ShowDialog();
                    if (accountCreateForm.DialogResult == true)
                    {
                        _user = accountCreateForm.ReturnUser;
                    }
                }
                else
                {
                    return;
                }
            }
            var result = MessageWindow.Show(this, "Alert:", "You typed " + GetWPM() + " WPM in " + secondsElapsed + " seconds.\n" +
                                                            "Would you like to submit your results?\nNote: You must submit test to earn experience!", MessageBoxType.YESNO);
            if (result == true)
            {
                SubmitTest();
            }
        }

        public void SubmitTest()
        {
            var testResult = _testManager.SaveTestResults(_user.UserID, testData.TestID, GetWPM(), secondsElapsed);
            var wpmXPModifier = _levelManager.GetWPMXPModifier(testResult.WPM);
            var timeXPModifier = _levelManager.GetTimeXPModifier((decimal)testResult.SecondsElapsed);

            var earnedXP = _levelManager.CalculateXP(testResult.WPM, wpmXPModifier, timeXPModifier);

            var outputString = "You have earned " + earnedXP + " XP!\n" +
                                "WPM = " + testResult.WPM + "\n" +
                                "WPM Modifier = " + wpmXPModifier + "\n" +
                                "Time Modifier = " + timeXPModifier + "\n" +
                                testResult.WPM + " x (" + wpmXPModifier + " + " + timeXPModifier + ") = " + earnedXP;
            MessageWindow.Show(this, "Congratulations!", outputString);
            try
            {
                var appliedXPTuple = _userManager.UserLevelingHandler(_user, earnedXP);
                _user = appliedXPTuple.Item1;
                int levelsGained = appliedXPTuple.Item2;
                bool titlesEarned = appliedXPTuple.Item3;

                if (levelsGained > 0 || titlesEarned == true)
                {
                    if (levelsGained > 1)
                    {
                        outputString = "Wow! Somehow, you managed to earn more than 1 level. Congrats!";
                    }
                    else if (levelsGained == 1)
                    {
                        outputString = "You have leveled up!\nYou are now level " + _user.Level +
                                       "\nYou have earned the rank: " + _rankManager.RetrieveUserRankName(_user.RankID);
                    }
                    else
                    {
                        outputString = "You have earned the rank: " + _rankManager.RetrieveUserRankName(_user.RankID);
                    }
                    ToastWindow.Show(this, "Congratulations!", outputString, _user.RankID, _rankManager);
                }
            }
            catch (Exception)
            {
                MessageWindow.Show(this, "Error:", "Unable to apply experience.");
            }



        }

        private void timeElapsedTimer_Tick(object sender, EventArgs e)
        {
            secondsLeft--;
            secondsElapsed++;
            lblTimeLeft.Content = _testManager.SecondsToTimeSpanFormatter(secondsLeft);
            if (secondsLeft == 0)
            {
                EndTest();
            }
        }


        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Sets location of owner window so it reopens where this window closes.
            MainForm mainForm = new MainForm(_user);
            mainForm.Top = this.Top;
            mainForm.Left = this.Left;
            mainForm.Show();
        }

        private void txtTextEntryBox_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (!txtTextEntryBox.Text.Equals("") && testInProgress == true)
            {
                var userEnteredText = Regex.Split(txtTextEntryBox.Text, @"(?<=[ ])").ToList();
                if (userEnteredText[0].Equals(lstTestDataText[0]))
                {
                    correctWords.Add(lstTestDataText[0]);
                    txtTestData.Text = txtTestData.Text.Remove(0, lstTestDataText[0].Length);
                    lstTestDataText.RemoveAt(0);
                    txtTextEntryBox.Clear();
                    txtTextEntryBox.Background = Brushes.White;
                    if (txtTestData.Text.Equals(""))
                    {
                        lblYourSpeed.Content = GetWPM() + " WPM";
                        EndTest();
                    }
                }
                else if (txtTextEntryBox.Text.Length >= lstTestDataText[0].Length)
                {
                    txtTextEntryBox.Background = Brushes.Red;

                }
                lblYourSpeed.Content = GetWPM() + " WPM";
            }
        }

        private decimal GetWPM()
        {
            return _testManager.CalculateWPM(correctWords, (decimal)secondsElapsed);
        }
    }
}
