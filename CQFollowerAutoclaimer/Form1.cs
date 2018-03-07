using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PlayFab;
using PlayFab.ClientModels;
using System.Threading;
using System.IO;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using System.Timers;
using System.Diagnostics;
using System.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace CQFollowerAutoclaimer
{
    public partial class Form1 : Form
    {
        static string calcOut;
        string token;
        int count = 0;
        string KongregateId;
        static Label[] timeLabels;

        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        DateTime start = DateTime.Now;
        DateTime nextClaim;
        DateTime nextDQTime;


        System.Timers.Timer tmr = new System.Timers.Timer();
        System.Timers.Timer timeElapsed = new System.Timers.Timer();
        System.Timers.Timer logoutTimer = new System.Timers.Timer();

        System.Timers.Timer DQTimer = new System.Timers.Timer();

        PFStuff pf;
        long initialFollowers;


        public Form1()
        {
            InitializeComponent();
            timeLabels = new Label[] { claimtime1, claimtime2, claimtime3, claimtime4, claimtime5, claimtime6, claimtime7, claimtime8, claimtime9 };
            init();         
            miracleLoop();
            timeElapsed.Interval = 1000;
            timeElapsed.Elapsed += timeElapsed_Elapsed;
            timeElapsed.Start();
            logoutTimer.Interval = 24 * 3600;
            logoutTimer.Elapsed += logoutTimer_Elapsed;
            logoutTimer.Start();
        }



        void logoutTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            PlayFabClientAPI.Logout();
        }


        void timeElapsed_Elapsed(object sender, ElapsedEventArgs e)
        {
            time.SynchronizedInvoke(() => time.Text = (DateTime.Now - start).ToString("dd\\.hh\\:mm\\:ss"));
            if (nextClaim != null)
            {
                countdown.SynchronizedInvoke(() => countdown.Text = (nextClaim - DateTime.Now).ToString("mm\\:ss"));
                notifyIcon1.Text = "CQ Autoclaimer\nNext claim in " + countdown.Text;
            }

            if (nextDQTime != null)
            {
                DQCountdownLabel.SynchronizedInvoke(() => DQCountdownLabel.Text = (nextDQTime - DateTime.Now).ToString("hh\\:mm\\:ss"));
            }

        }

        public void miracleLoop()
        {

            DateTime[] times = new DateTime[] { DateTime.Now };
            if (!PlayFab.PlayFabClientAPI.IsClientLoggedIn())
            {
                login();
            }
            getData();
            times = getTimes(PFStuff.miracleTimes);
            int i = 0;
            foreach (Label l in timeLabels)
            {
                l.SynchronizedInvoke(() => l.Text = times[i++].ToString());
            }
            nextClaim = times.Min();
            nextclaimtime.SynchronizedInvoke(() => nextclaimtime.Text = times.Min().ToString());
            initialFollowers = Int64.Parse(PFStuff.initialFollowers);
            tmr.Interval = Math.Max(3000, (times.Min() - DateTime.Now).TotalMilliseconds);
            tmr.Elapsed += OnTmrElapsed;
            tmr.Start();
            nextDQTime = getTime(PFStuff.DQTime);
            DQLevelLabel.Text = PFStuff.DQLevel;
            DQTimeLabel.Text = nextDQTime.ToString();
            if (Int32.Parse(PFStuff.DQLevel) < 3)
            {
                if (DQSoundBox.Checked)
                {
                    using (var soundPlayer = new SoundPlayer(@"c:\Windows\Media\Windows Notify.wav"))
                    {
                        soundPlayer.Play(); 
                    }
                }
                if (DQCalcBox.Checked)
                {
                    RunCalc();
                }
            }
            else
            {
                DateTime DQRunTime = getTime(PFStuff.DQTime);
                DQTimer.Interval = Math.Max(3000, (DQRunTime - DateTime.Now).TotalMilliseconds);
                DQTimer.Elapsed += DQTimer_Elapsed;
                DQTimer.Start();
            }
        }

       



        void DQTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (DQSoundBox.Checked)
            {
                using (var soundPlayer = new SoundPlayer(@"c:\Windows\Media\Windows Notify.wav"))
                {
                    soundPlayer.Play();
                }
            }
            if (DQCalcBox.Checked)
            {
                RunCalc();
            }
        }

        void RunCalc()
        {
            calcOut = "";
            var proc = new Process();
            proc.StartInfo.FileName = "CQMacroCreator";
            proc.StartInfo.Arguments = "quick";

            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.EnableRaisingEvents = true;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.UseShellExecute = false;

            proc.ErrorDataReceived += proc_DataReceived;
            proc.OutputDataReceived += proc_DataReceived;

            proc.Start();

            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();

            proc.WaitForExit();
        }

        void proc_DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                calcOut += e.Data + "\n";
            }
        }

        private void OnTmrElapsed(Object source, System.Timers.ElapsedEventArgs e)
        {
            if (!PlayFab.PlayFabClientAPI.IsClientLoggedIn())
            {
                login();
            }
            Thread mt;
            mt = new Thread(pf.sendClaimAll);
            mt.Start();
            mt.Join();
            DateTime[] times;
            times = getTimes(PFStuff.miracleTimes);
            int i = 0;
            foreach (Label l in timeLabels)
            {
                l.SynchronizedInvoke(() => l.Text = times[i++].ToString());
            }
            nextClaim = times.Min();
            nextclaimtime.SynchronizedInvoke(() => nextclaimtime.Text = times.Min().ToString());
            claimAmount.SynchronizedInvoke(() => claimAmount.Text = (++count).ToString());
            followersClaimed.SynchronizedInvoke(() => followersClaimed.Text = (Int64.Parse(PFStuff.initialFollowers) - initialFollowers).ToString());
            tmr.Interval = Math.Max(1000, (times.Min() - DateTime.Now).TotalMilliseconds);
        }


        private void getData()
        {
            Thread mt;
            mt = new Thread(pf.GetGameData);
            mt.Start();
            mt.Join();
        }
        private string getSetting(string s)
        {
            if (s == null || s == String.Empty)
                return null;
            else
            {
                s = s.TrimStart(" ".ToArray());
                int index = s.IndexOfAny("/ \t\n".ToArray());
                if (index > 0)
                {
                    s = s.Substring(0, index);
                }
                return s.Trim();
            }
        }

        private void init()
        {
            if (!File.Exists("Newtonsoft.Json.dll") || !File.Exists("Newtonsoft.Json.xml"))
            {
                MessageBox.Show("Newtonsoft files not found. Please download them from this project's github");
            }
            if (File.Exists("MacroSettings.txt"))
            {
                System.IO.StreamReader sr = new System.IO.StreamReader("MacroSettings.txt");
                getSetting(sr.ReadLine());
                token = getSetting(sr.ReadLine());
                KongregateId = getSetting(sr.ReadLine());
                sr.Close();

                if (token == "1111111111111111111111111111111111111111111111111111111111111111" || KongregateId == "000000")
                {
                    token = KongregateId = null;
                }
            }
            else
            {
                token = null;
                KongregateId = null;
                MessageBox.Show("MacroSettings.txt file not found.");
            }
            if (token != null && KongregateId != null)
            {
                pf = new PFStuff(token, KongregateId);
            }

        }


        private void login()
        {
            Thread mt;
            mt = new Thread(pf.LoginKong);
            mt.Start();
            mt.Join();
            if (PFStuff.logres)
            {
                Console.Write("Successfully logged in\n");
            }
            else
            {
                MessageBox.Show("Failed to log in.\nYour kong ID: " + KongregateId + "\nYour auth ticket: " + token + "\nLenght of token should be 64, yours is: " + token.Length);
            }
        }

        public DateTime getTime(string t)
        {
            DateTime time = epoch.AddMilliseconds(Convert.ToInt64(t)).ToLocalTime();
            return time;
        }

        public DateTime[] getTimes(string t)
        {
            t = Regex.Replace(t, @"\s+", "");
            t = t.Substring(1, t.Length - 2);
            long[] unixes = t.Split(',').Select(n => Convert.ToInt64(n)).ToArray();
            DateTime[] times = new DateTime[9];
            for (int i = 0; i < 9; i++)
            {
                times[i] = epoch.AddMilliseconds(unixes[i]).ToLocalTime();
            }
            return times;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon1.Visible = true;
                //notifyIcon1.ShowBalloonTip(1);
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            //notifyIcon1.ShowBalloonTip(100);
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            Close();
        }




    }
}
