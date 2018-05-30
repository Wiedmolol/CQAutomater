using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Timers;
using System.Media;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CQFollowerAutoclaimer
{
    class AutoDQ
    {
        Form1 main;
        internal static int currentDQ;
        internal static string calcOut;
        internal static string calcErrorOut;
        internal static int DQFailedAttempts;
        internal System.Timers.Timer DQTimer = new System.Timers.Timer();
        internal DateTime nextDQTime = new DateTime();
        public AutoDQ(Form1 m)
        {
            main = m;
            DQTimer.Elapsed += DQTimer_Elapsed;
            loadDQSettings();
        }

        public enum CalcMode { DQ, DUNG};

        void loadDQSettings()
        {
            main.DQCalcBox.Checked = main.appSettings.autoDQEnabled ?? false;
            main.DQSoundBox.Checked = main.appSettings.DQSoundEnabled ?? true;
            main.DQBestBox.Checked = main.appSettings.autoBestDQEnabled ?? false;
            if (main.appSettings.defaultDQLineup != null)
            {
                for (int i = 0; i < main.appSettings.defaultDQLineup.Count; i++)
                {
                    main.WBlineups[2][i].Text = main.appSettings.defaultDQLineup[i];
                }
            }
        }

        async void DQTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (main.DQSoundBox.Checked)
            {
                using (var soundPlayer = new SoundPlayer(@"c:\Windows\Media\Windows Notify.wav"))
                {
                    soundPlayer.Play();
                }
            }
            if (main.DQCalcBox.Checked || main.DQBestBox.Checked)
            {
                fightWithPresetLineup(CalcMode.DQ);
            }
            else
            {
                await main.getData();
            }
        }
        private async Task<bool> sendSolution(int[] lineup, CalcMode mode)
        {
            if (DQFailedAttempts >= 3)
            {
                if (main.DQCalcBox.getCheckState())
                {
                    RunCalc(mode);
                }
            }
            else
            {
                bool b = false;
                if(mode == CalcMode.DQ)
                     b = await main.pf.sendDQSolution(lineup);
                if(mode == CalcMode.DUNG)
                    b = await main.pf.sendDungSolution(lineup);
                if (!b)
                {
                    DQFailedAttempts++;
                    main.taskQueue.Enqueue(() => sendSolution(lineup, mode), "DQ");
                }
                else
                {
                    if (currentDQ == int.Parse(PFStuff.DQLevel))
                    {
                        if (main.DQCalcBox.getCheckState())
                        {
                            RunCalc(mode);
                        }
                    }
                    else
                    {
                        DQFailedAttempts = 0;
                        currentDQ = int.Parse(PFStuff.DQLevel);
                        main.taskQueue.Enqueue(() => sendSolution(lineup, mode), "DQ");
                        main.autoLevel.levelTimer.Interval = 60 * 1000;
                    }
                }
            }
            return true;
        }

        internal void fightWithPresetLineup(CalcMode mode)
        {
            List<string> DQl = new List<string>();
            string s = "";
            for (int i = 0; i < main.WBlineups[2].Count; i++)
            {
                main.WBlineups[2][i].SynchronizedInvoke(() => s = main.WBlineups[2][i].Text);
                DQl.Add(s);
            }
            if (DQl.Any(x => x != ""))
            {
                int[] Lineup = main.getLineup(4, 0);
                currentDQ = int.Parse(PFStuff.DQLevel);
                main.calcStatus.SynchronizedInvoke(() => main.calcStatus.Text = "Using best lineup.");
                main.taskQueue.Enqueue(() => sendSolution(Lineup, mode), "DQ");
            }
            else if (main.DQCalcBox.Checked)
            {
                RunCalc(mode);
            }
            else
            {
                main.calcStatus.SynchronizedInvoke(() => main.calcStatus.Text = "Done");
            }
           
        }

        internal void RunCalc(CalcMode mode)
        {
            if (File.Exists("CQMacroCreator.exe") && File.Exists("CosmosQuest.exe"))
            {
                DQTimer.Stop();
                main.calcStatus.SynchronizedInvoke(() => main.calcStatus.Text = "Calc is running");
                calcOut = "";
                var proc = new Process();
                proc.StartInfo.FileName = "CQMacroCreator";
                proc.StartInfo.Arguments = mode == CalcMode.DQ ? "quick" : "quickdung";

                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.EnableRaisingEvents = true;
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.UseShellExecute = false;

                proc.ErrorDataReceived += proc_ErrorDataReceived;
                proc.OutputDataReceived += proc_DataReceived;
                proc.Exited += proc_Exited;
                proc.Start();

                proc.BeginErrorReadLine();
                proc.BeginOutputReadLine();

                proc.WaitForExit();

            }
            else
            {
                MessageBox.Show("CQMacroCreator.exe or CosmosQuest.exe file not found");
            }
        }

        void proc_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                calcErrorOut += e.Data + "\n";
            }
        }
        void proc_DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                calcOut += e.Data + "\n";
            }
        }

       async void proc_Exited(object sender, EventArgs e)
        {
            await main.getData();
            await PFStuff.getWBData(main.KongregateId);
            main.calcStatus.SynchronizedInvoke(() => main.calcStatus.Text = "Calc finished");
            nextDQTime = Form1.getTime(PFStuff.DQTime);
            DQTimer.Interval = (nextDQTime < DateTime.Now && main.DQCalcBox.Checked) ? 4000 : Math.Max(4000, (nextDQTime - DateTime.Now).TotalMilliseconds);
            main.DQLevelLabel.SynchronizedInvoke(() => main.DQLevelLabel.Text = PFStuff.DQLevel);
            main.DQTimeLabel.SynchronizedInvoke(() => main.DQTimeLabel.Text = nextDQTime.ToString());

            main.currentDungLevelLabel.setText(PFStuff.DungLevel);
            main.autoLevel.levelTimer.Interval = 1.5 * 60 * 1000;
            DQTimer.Start();
            if (!string.IsNullOrEmpty(calcErrorOut))
            {
                using (StreamWriter sw = new StreamWriter("CQMCErrors.txt"))
                {
                    sw.WriteLine(DateTime.Now);
                    sw.WriteLine(calcErrorOut);
                }
            }
            List<string> DQl = new List<string>();
            string s = "";
            for (int i = 0; i < main.WBlineups[2].Count; i++)
            {
                main.WBlineups[2][i].SynchronizedInvoke(() => s = main.WBlineups[2][i].Text);
                DQl.Add(s);
            }

            if (DQl.All(x => x == "") && calcOut != "")
            {
                JObject solution = JObject.Parse(calcOut);
                var mon = solution["validSolution"]["solution"]["monsters"];
                List<string> DQLineup = new List<string>();

                for (int i = 0; i < mon.Count(); i++)
                {
                    DQLineup.Add(Constants.names[int.Parse(mon[i]["id"].ToString()) + Constants.heroesInGame]);
                    main.WBlineups[2][5 - i].SynchronizedInvoke(() => main.WBlineups[2][5 - i].Text = Constants.names[int.Parse(mon[i]["id"].ToString()) + Constants.heroesInGame]);
                }
                main.appSettings = AppSettings.loadSettings();
                main.appSettings.defaultDQLineup = DQLineup;
                main.appSettings.saveSettings();
            }
        }
    }
}
