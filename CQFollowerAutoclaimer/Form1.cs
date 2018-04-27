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
using System.Runtime.InteropServices;
using System.Net;
using System.Reflection;
namespace CQFollowerAutoclaimer
{
    public partial class Form1 : Form
    {
        static internal AppSettings appSettings = new AppSettings();
        public const string SettingsFilename = "Settings.json";

        internal string token;
        internal string KongregateId;
        PFStuff pf;
        AuctionHouse auctionHouse;
        int claimCount = 0;
        static Label[] timeLabels;
        static int chestsToOpen;
        static int attacksToPerform;
        static int DQFailedAttempts;
        long initialFollowers;
        static int currentDQ;
        static string calcOut;
        static string calcErrorOut;
        static bool notAskedYet = true;

        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        DateTime start = DateTime.Now;
        DateTime nextClaim;
        DateTime nextDQTime;
        DateTime nextPVP;
        DateTime nextFreeChest;
        DateTime nextWBRefresh;

        string WBLogString = "";
        static WBLog wbl;
        List<List<ComboBox>> WBlineups;
        List<ComboBox> auctionComboBoxes;

        System.Timers.Timer tmr = new System.Timers.Timer();
        System.Timers.Timer countdownsTimer = new System.Timers.Timer();
        System.Timers.Timer logoutTimer = new System.Timers.Timer();
        System.Timers.Timer DQTimer = new System.Timers.Timer();
        System.Timers.Timer DQFightTimer = new System.Timers.Timer();
        System.Timers.Timer PVPTimer = new System.Timers.Timer();
        System.Timers.Timer FreeChestTimer = new System.Timers.Timer();
        System.Timers.Timer chestTimer = new System.Timers.Timer();
        System.Timers.Timer WBTimer = new System.Timers.Timer();
        System.Timers.Timer WBAttackTimer = new System.Timers.Timer();

        List<CheckBox> enableBoxes;
        List<NumericUpDown> wbSettingsCounts;



        public Form1()
        {
            InitializeComponent();
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += currentDomain_UnhandledException;
            auctionHouse = new AuctionHouse(this);           
            timeLabels = new Label[] { claimtime1, claimtime2, claimtime3, claimtime4, claimtime5, claimtime6, claimtime7, claimtime8, claimtime9 };
            enableBoxes = new List<CheckBox> { DQCalcBox, freeChestBox, autoPvPCheckbox, autoWBCheckbox };
            wbSettingsCounts = new List<NumericUpDown> { 
                LOCHARequirementCount, LOCHAAttacksCount, superLOCHAReqCount, superLOCHAAtkCount,
                LOCNHRequirementCount, LOCNHAttacksCount, superLOCNHReqCount, superLOCNHAtkCount,
                MOAKHARequirementCount, MOAKHAAttacksCount, superMOAKHAReqCount, superMOAKHAAtkCount,
                MOAKNHRequirementCount, MOAKNHAttacksCount, superMOAKNHReqCount, superMOAKNHAtkCount
            };
            toolTip1.SetToolTip(safeModeWB, "Asks for confirmation before attacking. Won't ask again for the same boss");
            safeModeWB.Enabled = false;
            WBlineups = new List<List<ComboBox>> {
                    new List<ComboBox> {LOCHA1, LOCHA2, LOCHA3, LOCHA4, LOCHA5, LOCHA6},
                    new List<ComboBox> {MOAKHA1, MOAKHA2, MOAKHA3, MOAKHA4, MOAKHA5, MOAKHA6},
                    new List<ComboBox> {DQLineup1, DQLineup2, DQLineup3, DQLineup4, DQLineup5, DQLineup6}
                };
            auctionComboBoxes = new List<ComboBox> {
                auctionHero1Combo, auctionHero2Combo, auctionHero3Combo,
            };
            AutoCompleteStringCollection acsc = new AutoCompleteStringCollection();
            foreach (List<ComboBox> l in WBlineups)
            {
                foreach (ComboBox c in l)
                {
                    foreach (string n in Constants.names.ToList().OrderBy(s => s))
                    {
                        c.Items.Add(n);
                    }

                }
            }

            init();

            if (pf != null)
            {
                PFStuff.getUsername(KongregateId);
                startTimers();
                countdownsTimer.Interval = 1000;
                countdownsTimer.Elapsed += countdownsTimer_Elapsed;
                countdownsTimer.Start();
                logoutTimer.Interval = 24 * 3600 * 1000;
                logoutTimer.Elapsed += logoutTimer_Elapsed;
                logoutTimer.Start();
                WBTimer.Interval = 1000 * 60 * 1; //1 minute
                WBTimer.Elapsed += WBTimer_Elapsed;
                WBTimer.Start();
                nextWBRefresh = DateTime.Now.AddMilliseconds(WBTimer.Interval);
            }
        }

        void currentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ue = (Exception)e.ExceptionObject;
            using (StreamWriter sw = new StreamWriter("ExceptionLog.txt", true))
            {
                sw.WriteLine(DateTime.Now);
                sw.WriteLine(ue);
            }
        }


        #region START
        private void init()
        {
            if (!File.Exists("Newtonsoft.Json.dll"))
            {
                MessageBox.Show("Newtonsoft file not found. Please download it from this project's github");
            }
            if (File.Exists(SettingsFilename))
            {
                appSettings = AppSettings.loadSettings();
                token = appSettings.token;
                KongregateId = appSettings.KongregateId;
                autoPvPCheckbox.Checked = appSettings.autoPvPEnabled ?? false;
                autoWBCheckbox.Checked = appSettings.autoWBEnabled ?? false;
                DQCalcBox.Checked = appSettings.autoDQEnabled ?? false;
                DQSoundBox.Checked = appSettings.DQSoundEnabled ?? true;
                DQBestBox.Checked = appSettings.autoBestDQEnabled ?? false;
                freeChestBox.Checked = appSettings.autoChestEnabled ?? false;
                safeModeWB.Checked = appSettings.safeModeWBEnabled ?? false;
                chestToOpenCount.Value = appSettings.chestsToOpen ?? 0;                
                playersBelowCount.Value = appSettings.pvpLowerLimit ?? 4;
                playersAboveCount.Value = appSettings.pvpUpperLimit ?? 5;
                if (appSettings.WBsettings != null)
                {
                    for (int i = 0; i < appSettings.WBsettings.Count; i++)
                    {
                        wbSettingsCounts[i].Value = appSettings.WBsettings[i];
                    }
                }
                if (appSettings.LoCLineup != null)
                {
                    for (int i = 0; i < appSettings.LoCLineup.Count; i++)
                    {
                        WBlineups[0][i].Text = appSettings.LoCLineup[i];
                    }
                }
                if (appSettings.MOAKLineup != null)
                {
                    for (int i = 0; i < appSettings.MOAKLineup.Count; i++)
                    {
                        WBlineups[1][i].Text = appSettings.MOAKLineup[i];
                    }
                }
                if (appSettings.defaultDQLineup != null)
                {
                    for (int i = 0; i < appSettings.defaultDQLineup.Count; i++)
                    {
                        WBlineups[2][i].Text = appSettings.defaultDQLineup[i];
                    }
                }

            }
            else if (File.Exists("MacroSettings.txt"))
            {
                System.IO.StreamReader sr = new System.IO.StreamReader("MacroSettings.txt");
                appSettings.actionOnStart = int.Parse(getSetting(sr.ReadLine()));
                token = appSettings.token = getSetting(sr.ReadLine());
                KongregateId = appSettings.KongregateId = getSetting(sr.ReadLine());
                appSettings.defaultLowerLimit = sr.ReadLine();
                appSettings.defaultUpperLimit = sr.ReadLine();
                sr.Close();

                if (token == "1111111111111111111111111111111111111111111111111111111111111111" || KongregateId == "000000")
                {
                    token = KongregateId = null;
                }
                appSettings.saveSettings();
            }
            else
            {
                token = null;
                KongregateId = null;
                DialogResult dr = MessageBox.Show("Settings file not found. Do you want help with creating one?", "Settings Question",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if (dr == DialogResult.Yes)
                {
                    MacroSettingsHelper msh = new MacroSettingsHelper(appSettings);
                    msh.Show();
                    msh.BringToFront();
                }
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
                DialogResult dr = MessageBox.Show("Failed to log in.\nYour kong ID: " + KongregateId + "\nYour auth ticket: " + token +
                    "\nLenght of token should be 64, yours is: " + token.Length + "\nDo you want help with creating MacroSettings file?",
                    "Settings Question", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if (dr == DialogResult.Yes)
                {
                    MacroSettingsHelper msh = new MacroSettingsHelper(appSettings);
                    msh.Show();
                    msh.BringToFront();
                }
            }
        }

        void logoutTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            PlayFabClientAPI.Logout();
        }

        public void startTimers()
        {
            DateTime[] times = new DateTime[] { DateTime.Now };
            if (!PlayFab.PlayFabClientAPI.IsClientLoggedIn())
            {
                login();
            }
            if (!PFStuff.logres)
            {
                return;
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
            if (Int32.Parse(PFStuff.DQLevel) < 2)
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
                //DQTimer.Interval = Math.Max(3000, (DQRunTime - DateTime.Now).TotalMilliseconds);
                DQTimer.Interval = (nextDQTime < DateTime.Now && DQCalcBox.Checked) ? 4000 : Math.Max(4000, (nextDQTime - DateTime.Now).TotalMilliseconds);
                DQTimer.Elapsed += DQTimer_Elapsed;
                DQTimer.Start();
            }

            nextPVP = getTime(PFStuff.PVPTime);
            PvPTimeLabel.SynchronizedInvoke(() => PvPTimeLabel.Text = nextPVP.ToString());
            PVPTimer.Interval = Math.Max(3000, (nextPVP - DateTime.Now).TotalMilliseconds);
            PVPTimer.Elapsed += PVPTimer_Elapsed;
            PVPTimer.Start();

            getCurr();
            nextFreeChest = DateTime.Now.AddSeconds(PFStuff.freeChestRecharge);
            FreeChestTimeLabel.SynchronizedInvoke(() => FreeChestTimeLabel.Text = nextFreeChest.ToString());
            FreeChestTimer.Interval = PFStuff.freeChestRecharge * 1000;
            FreeChestTimer.Elapsed += FreeChestTimer_Elapsed;
            FreeChestTimer.Start();

            getWebsiteData();
            foreach (ComboBox c in auctionComboBoxes)
            {
                c.Items.AddRange(auctionHouse.getAvailableHeroes());
            }
            auctionHouse.loadSettings();
        }


        void countdownsTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            time.SynchronizedInvoke(() => time.Text = (DateTime.Now - start).ToString("dd\\.hh\\:mm\\:ss"));
            if (nextClaim != null)
            {
                countdown.SynchronizedInvoke(() => countdown.Text = (nextClaim - DateTime.Now).ToString("mm\\:ss"));
                notifyIcon1.Text = "CQ Autoclaimer\nNext claim in " + countdown.Text;
            }
            if (nextDQTime != null)
            {
                DQCountdownLabel.SynchronizedInvoke(() => DQCountdownLabel.Text = (nextDQTime < DateTime.Now ? "-" : "") + (nextDQTime - DateTime.Now).ToString("hh\\:mm\\:ss"));
            }
            if (nextFreeChest != null)
            {
                FreeChestCountdownLabel.SynchronizedInvoke(() => FreeChestCountdownLabel.Text = (nextFreeChest < DateTime.Now ? "-" : "") + (nextFreeChest - DateTime.Now).ToString("hh\\:mm\\:ss"));
            }
            if (nextPVP != null)
            {
                PvPCountdownLabel.SynchronizedInvoke(() => PvPCountdownLabel.Text = (nextPVP < DateTime.Now ? "-" : "") + (nextPVP - DateTime.Now).ToString("hh\\:mm\\:ss"));
            }
            if (nextWBRefresh != null)
            {
                WBCountdownLabel.SynchronizedInvoke(() => WBCountdownLabel.Text = (nextWBRefresh < DateTime.Now ? "-" : "") + (nextWBRefresh - DateTime.Now).ToString("hh\\:mm\\:ss"));
                AHCountdownLabel.SynchronizedInvoke(() => AHCountdownLabel.Text = (nextWBRefresh < DateTime.Now ? "-" : "") + (nextWBRefresh - DateTime.Now).ToString("hh\\:mm\\:ss"));
            }

        }

        #endregion

        #region DATA RETRIEVE
        private void getData()
        {
            Thread mt;
            mt = new Thread(pf.GetGameData);
            mt.Start();
            mt.Join();
        }

        private void getCurr()
        {
            Thread mt;
            mt = new Thread(pf.getCurrencies);
            mt.Start();
            mt.Join();
            if (PFStuff.freeChestAvailable && freeChestBox.Checked)
            {
                PFStuff.chestMode = "normal";
                openChest();
            }
            nextFreeChest = DateTime.Now.AddSeconds(PFStuff.freeChestRecharge);
            FreeChestTimeLabel.SynchronizedInvoke(() => FreeChestTimeLabel.Text = nextFreeChest.ToString());
            FreeChestTimer.Interval = PFStuff.freeChestAvailable == true ? 6000 : PFStuff.freeChestRecharge * 1000;

            NormalChestLabel.SynchronizedInvoke(() => NormalChestLabel.Text = PFStuff.normalChests.ToString());
            HeroChestLabel.SynchronizedInvoke(() => HeroChestLabel.Text = PFStuff.heroChests.ToString());
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

        #endregion

        #region CHESTS
        void FreeChestTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!PlayFab.PlayFabClientAPI.IsClientLoggedIn())
            {
                login();
            }
            getCurr();
           
        }


        private void openNormalButton_Click(object sender, EventArgs e)
        {
            PFStuff.chestMode = "normal";
            chestsToOpen = (int)chestToOpenCount.Value;
            openNormalButton.Enabled = false;
            openHeroButton.Enabled = false;
            chestTimer.Interval = 4000;
            chestTimer.Elapsed += chestTimer_Elapsed;
            chestTimer.Start();
        }

        private void openHeroButton_Click(object sender, EventArgs e)
        {
            PFStuff.chestMode = "hero";
            chestsToOpen = (int)chestToOpenCount.Value;
            openNormalButton.Enabled = false;
            openHeroButton.Enabled = false;
            chestTimer.Interval = 4000;
            chestTimer.Elapsed += chestTimer_Elapsed;
            chestTimer.Start();
        }

        void chestTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (chestsToOpen == 0)
            {
                openNormalButton.SynchronizedInvoke(() => openNormalButton.Enabled = true);
                openHeroButton.SynchronizedInvoke(() => openHeroButton.Enabled = true);
                chestTimer.Stop();
            }
            else
            {
                openChest();
                chestsToOpen--;
            }
        }
        private void refreshChestsButton_Click(object sender, EventArgs e)
        {
            getCurr();
        }


        private void freeChestBox_CheckedChanged(object sender, EventArgs e)
        {
            if (freeChestBox.Checked)
            {
                chestIndicator.BackColor = Color.Green;
                if ((DateTime.Now - start).TotalSeconds > 3)
                {
                    getCurr();
                    if (PFStuff.freeChestAvailable)
                    {
                        PFStuff.chestMode = "normal";
                        openChest();
                    }
                }
            }
            else
            {
                chestIndicator.BackColor = Color.Red;
            }
        }


        void openChest()
        {
            Thread mt;
            mt = new Thread(pf.sendOpen);
            mt.Start();
            mt.Join();
            string rew = "Got ";
            rew += PFStuff.chestResult < 0 ? Constants.heroNames[-PFStuff.chestResult] : Constants.rewardNames[PFStuff.chestResult];
            ChestLog.SynchronizedInvoke(() => ChestLog.AppendText(rew + "\n"));
        }

        #endregion

        #region PVP
        void PVPTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (autoPvPCheckbox.Checked)
            {
                if (!PlayFab.PlayFabClientAPI.IsClientLoggedIn())
                {
                    login();
                }
                Thread mt;
                PFStuff.LeaderboardRange = Math.Max(3, 2 * (int)Math.Max(playersAboveCount.Value, playersBelowCount.Value + 1));
                mt = new Thread(pf.getLeaderboard);
                mt.Start();
                mt.Join();
                Random r = new Random();
                do
                {
                    PFStuff.PVPEnemyIndex = r.Next(0, PFStuff.nearbyPlayersIDs.Length);
                } while (PFStuff.PVPEnemyIndex == PFStuff.userIndex ||
                        PFStuff.PVPEnemyIndex > PFStuff.userIndex + (int)playersBelowCount.Value ||
                        PFStuff.PVPEnemyIndex < PFStuff.userIndex - (int)playersAboveCount.Value);

                mt = new Thread(pf.sendPVPFight);
                mt.Start();
                mt.Join();
                nextPVP = getTime(PFStuff.PVPTime);
                PVPTimer.Interval = Math.Max(5000, (nextPVP - DateTime.Now).TotalMilliseconds);
                PvPLog.SynchronizedInvoke(() => PvPLog.AppendText(PFStuff.battleResult));
                PvPTimeLabel.SynchronizedInvoke(() => PvPTimeLabel.Text = nextPVP.ToString());
            }
        }

        private void autoPvPCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (autoPvPCheckbox.Checked)
            {
                PVPIndicator.BackColor = Color.Green;
                if ((DateTime.Now - start).TotalSeconds > 3)
                {
                    getData();
                    nextPVP = getTime(PFStuff.PVPTime);
                    PvPTimeLabel.SynchronizedInvoke(() => PvPTimeLabel.Text = nextPVP.ToString());
                    PVPTimer.Interval = Math.Max(8000, (nextPVP - DateTime.Now).TotalMilliseconds);
                }
            }
            else
            {
                PVPIndicator.BackColor = Color.Red;
            }
        }
        #endregion

        #region DQ
        void DQTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (DQSoundBox.Checked)
            {
                using (var soundPlayer = new SoundPlayer(@"c:\Windows\Media\Windows Notify.wav"))
                {
                    soundPlayer.Play();
                }
            }
            if (DQCalcBox.Checked || DQBestBox.Checked)
            {
                fightDQWithPresetLineup();
                //RunCalc();
            }
            else
            {
                getData();
                nextDQTime = getTime(PFStuff.DQTime);
                DQTimer.Interval = (nextDQTime < DateTime.Now && DQCalcBox.Checked) ? 4000 : Math.Max(4000, (nextDQTime - DateTime.Now).TotalMilliseconds);
                DQLevelLabel.SynchronizedInvoke(() => DQLevelLabel.Text = PFStuff.DQLevel);
                DQTimeLabel.SynchronizedInvoke(() => DQTimeLabel.Text = nextDQTime.ToString());
            }
        }

        void fightDQWithPresetLineup()
        {
            List<string> DQl = new List<string>();
            string s = "";
            for (int i = 0; i < WBlineups[2].Count; i++)
            {
                WBlineups[2][i].SynchronizedInvoke(() => s = WBlineups[2][i].Text);
                DQl.Add(s);
            }

            if (DQl.Any(x => x != ""))
            {
                PFStuff.DQlineup = getLineup(4, 0);
                currentDQ = int.Parse(PFStuff.DQLevel);
                calcStatus.SynchronizedInvoke(() => calcStatus.Text = "Using best lineup.");
                DQFightTimer.Interval = 5000;
                DQFightTimer.Elapsed += DQFightTimer_Elapsed;
                DQFightTimer.Start();
            }
            else if (DQCalcBox.Checked)
            {
                RunCalc();
            }
            else
            {
                calcStatus.SynchronizedInvoke(() => calcStatus.Text = "Done");
            }
        }

        void DQFightTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!PlayFab.PlayFabClientAPI.IsClientLoggedIn())
            {
                login();
            }
            Thread mt;
            mt = new Thread(pf.sendDQSolution);
            mt.Start();
            mt.Join();
            if (DQFailedAttempts >= 3)
            {
                DQFightTimer.Stop();
                bool b = false;
                DQCalcBox.SynchronizedInvoke(() => b = DQCalcBox.Checked);
                if (b)
                    RunCalc();
            }
            else if (PFStuff.DQResult)
            {
                DQFailedAttempts = 0;
                DQLevelLabel.SynchronizedInvoke(() => DQLevelLabel.Text = PFStuff.DQLevel);
                if (currentDQ == int.Parse(PFStuff.DQLevel))
                {
                    DQFightTimer.Stop();
                    bool b = false;
                    DQCalcBox.SynchronizedInvoke(() => b = DQCalcBox.Checked);
                    if (b)
                        RunCalc();
                }
                else
                {
                    currentDQ = int.Parse(PFStuff.DQLevel);
                }
            }
            else
            {
                DQFailedAttempts++;
            }
        }

        private void DQRefreshButton_Click(object sender, EventArgs e)
        {
            getData();
            nextDQTime = getTime(PFStuff.DQTime);
            DQTimer.Interval = (nextDQTime < DateTime.Now && DQCalcBox.Checked) ? 4000 : Math.Max(4000, (nextDQTime - DateTime.Now).TotalMilliseconds);
            DQLevelLabel.SynchronizedInvoke(() => DQLevelLabel.Text = PFStuff.DQLevel);
            DQTimeLabel.SynchronizedInvoke(() => DQTimeLabel.Text = nextDQTime.ToString());
        }

        private void DQCalcBox_CheckedChanged(object sender, EventArgs e)
        {

            if (DQCalcBox.Checked)
            {
                DQIndicator.BackColor = Color.Green;
            }
            else
            {
                DQIndicator.BackColor = Color.Red;
            }
            if (DQCalcBox.Checked && (DateTime.Now - start).TotalSeconds > 3)
            {
                DialogResult dr = MessageBox.Show("Do you want to run the auto-solve now?", "Calc Question", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if (dr == DialogResult.Yes)
                {
                    fightDQWithPresetLineup();
                }
            }
        }


        void RunCalc()
        {
            if (File.Exists("CQMacroCreator.exe") && File.Exists("CosmosQuest.exe"))
            {
                DQTimer.Stop();
                calcStatus.SynchronizedInvoke(() => calcStatus.Text = "Calc is running");
                calcOut = "";
                var proc = new Process();
                proc.StartInfo.FileName = "CQMacroCreator";
                proc.StartInfo.Arguments = "quick";

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

        void proc_Exited(object sender, EventArgs e)
        {
            getData();
            calcStatus.SynchronizedInvoke(() => calcStatus.Text = "Calc finished");
            nextDQTime = getTime(PFStuff.DQTime);
            //DQTimer.Interval = Math.Max(3000, (nextDQTime - DateTime.Now).TotalMilliseconds);
            DQTimer.Interval = (nextDQTime < DateTime.Now && DQCalcBox.Checked) ? 4000 : Math.Max(4000, (nextDQTime - DateTime.Now).TotalMilliseconds);
            DQLevelLabel.SynchronizedInvoke(() => DQLevelLabel.Text = PFStuff.DQLevel);
            DQTimeLabel.SynchronizedInvoke(() => DQTimeLabel.Text = nextDQTime.ToString());
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
            for (int i = 0; i < WBlineups[2].Count; i++)
            {
                WBlineups[2][i].SynchronizedInvoke(() => s = WBlineups[2][i].Text);
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
                    WBlineups[2][5 - i].SynchronizedInvoke(() => WBlineups[2][5 - i].Text = Constants.names[int.Parse(mon[i]["id"].ToString()) + Constants.heroesInGame]);
                }
                appSettings = AppSettings.loadSettings();
                appSettings.defaultDQLineup = DQLineup;
                appSettings.saveSettings();
            }
        }

        void proc_DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                calcOut += e.Data + "\n";
            }
        }

        private void runCalcButton_Click(object sender, EventArgs e)
        {
            fightDQWithPresetLineup();
        }
        #endregion

        #region MIRACLES
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
            claimAmount.SynchronizedInvoke(() => claimAmount.Text = (++claimCount).ToString());
            followersClaimed.SynchronizedInvoke(() => followersClaimed.Text = (Int64.Parse(PFStuff.initialFollowers) - initialFollowers).ToString());
            tmr.Interval = Math.Max(1000, (times.Min() - DateTime.Now).TotalMilliseconds);
        }

        #endregion

        #region UI

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon1.Visible = true;
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
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
        private void openMSHButton_Click(object sender, EventArgs e)
        {
            MacroSettingsHelper msh = new MacroSettingsHelper(appSettings);
            msh.Show();
        }

        private void automaterGithubButton_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Wiedmolol/CQAutomater");
        }

        private void macroCreatorGithubButton_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Wiedmolol/CQMacroCreator");
        }

        private void saveDQSettingsButton_Click(object sender, EventArgs e)
        {
            appSettings = AppSettings.loadSettings();
            appSettings.DQSoundEnabled = DQSoundBox.Checked;
            appSettings.autoDQEnabled = DQCalcBox.Checked;
            appSettings.autoBestDQEnabled = DQBestBox.Checked;
            var DQLinuep = WBlineups[2].Select(x => x.Text).ToList();
            appSettings.defaultDQLineup = DQLinuep;
            appSettings.saveSettings();
        }

        private void savePvPSettingsButton_Click(object sender, EventArgs e)
        {
            appSettings = AppSettings.loadSettings();
            appSettings.autoPvPEnabled = autoPvPCheckbox.Checked;
            appSettings.pvpLowerLimit = (int)playersBelowCount.Value;
            appSettings.pvpUpperLimit = (int)playersAboveCount.Value;
            appSettings.saveSettings();
        }

        private void saveChestSettingsButton_Click(object sender, EventArgs e)
        {
            appSettings = AppSettings.loadSettings();
            appSettings.autoChestEnabled = freeChestBox.Checked;
            appSettings.chestsToOpen = (int)chestToOpenCount.Value;
            appSettings.saveSettings();
        }
        #endregion

        #region WB

        int[] getLineup(int ID, uint followers)
        {
            int[] lineup;
            List<int> temp = new List<int>();
            switch (ID)
            {
                case 0:
                    var l1 = PeblLineups.LOCNHLineups.Last(x => x.Item1 < followers);
                    lineup = l1.Item3;
                    break;
                case 1:
                    foreach (ComboBox cb in WBlineups[0])
                    {
                        string s = "";
                        cb.SynchronizedInvoke(() => s = cb.Text);
                        temp.Add(Array.IndexOf(Constants.names, s) - Constants.heroesInGame);
                    }
                    lineup = temp.ToArray();
                    break;
                case 2:
                    var l2 = PeblLineups.MOAKNHLineups.Last(x => x.Item1 < followers);
                    lineup = l2.Item3;
                    break;
                case 3:
                    foreach (ComboBox cb in WBlineups[1])
                    {
                        string s = "";
                        cb.SynchronizedInvoke(() => s = cb.Text);
                        temp.Add(Array.IndexOf(Constants.names, s) - Constants.heroesInGame);
                    }
                    lineup = temp.ToArray();
                    break;
                case 4:
                    foreach (ComboBox cb in WBlineups[2])
                    {
                        string s = "";
                        cb.SynchronizedInvoke(() => s = cb.Text);
                        temp.Add(Array.IndexOf(Constants.names, s) - Constants.heroesInGame);
                    }
                    lineup = temp.ToArray();
                    break;
                default:
                    lineup = new int[1];
                    break;
            }
            Array.Reverse(lineup);
            return lineup;
        }

        string shortBossName(string longName)
        {
            switch (longName)
            {
                case ("LORD OF CHAOS"):
                    return "LoC";
                case ("MOTHER OF ALL KODAMAS"):
                    return "MOAK";
                default:
                    return "Unknowm";
            }
        }

        public void getWebsiteData()
        {
            PFStuff.getWebsiteData(KongregateId);
            currentBossLabel.SynchronizedInvoke(() => currentBossLabel.Text = shortBossName(PFStuff.WBName)
                + (PFStuff.wbMode == 0 ? " NH" : " HA") + ", Attacks left: " + PFStuff.attacksLeft);
            auctionHouse.loadAuctions(false);
            
            WBTimer.Interval = Math.Min(Math.Max(PFStuff.attacksLeft * 5000, 20000), auctionHouse.getAuctionInterval());
            nextWBRefresh = DateTime.Now.AddMilliseconds(WBTimer.Interval);
        }

        void WBTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            
            getWebsiteData();
            //currentBossLabel.Text = shortBossName(PFStuff.WBName) + (PFStuff.wbMode == 0 ? " NH" : " HA");
            if (autoWBCheckbox.Checked)
            {
                decimal attacksToDo = 0;
                decimal requirement = 99;
                int[] lineup = new int[2];
                int r = PFStuff.getWBData((PFStuff.WB_ID).ToString());
                if (r == -1)
                {
                    MessageBox.Show("You haven't enabled your username on website. Auto-WB won't work without enabled username.");
                }
                else
                {
                    if (PFStuff.WBName == "LORD OF CHAOS" && PFStuff.wbMode == 0) //loc no heroes
                    {
                        attacksToDo = LOCNHAttacksCount.Value;
                        requirement = LOCNHRequirementCount.Value;
                        lineup = getLineup(0, uint.Parse(PFStuff.initialFollowers));
                    }
                    else if (PFStuff.WBName == "LORD OF CHAOS" && PFStuff.wbMode == 1) //loc heroes allowed
                    {
                        attacksToDo = LOCHAAttacksCount.Value;
                        requirement = LOCHARequirementCount.Value;
                        lineup = getLineup(1, uint.Parse(PFStuff.initialFollowers));
                    }
                    else if (PFStuff.WBName == "MOTHER OF ALL KODAMAS" && PFStuff.wbMode == 0) //moak no heroes
                    {
                        attacksToDo = MOAKNHAttacksCount.Value;
                        requirement = MOAKNHRequirementCount.Value;
                        lineup = getLineup(2, uint.Parse(PFStuff.initialFollowers));
                    }
                    else if (PFStuff.WBName == "MOTHER OF ALL KODAMAS" && PFStuff.wbMode == 1) //moak heroes allowed
                    {
                        attacksToDo = MOAKHAAttacksCount.Value;
                        requirement = MOAKHARequirementCount.Value;
                        lineup = getLineup(3, uint.Parse(PFStuff.initialFollowers));
                    }

                    if (lineup.Contains(-1))
                    {
                        MessageBox.Show("You have empty slots in your lineup. You must use all 6 slots in your lineup. Auto-WB disabled.");
                        autoWBCheckbox.Checked = false;
                        return;
                    }
                    attacksToDo -= r;
                    if (attacksToDo <= 0)
                        return;
                    getData();
                    if (PFStuff.WBchanged)
                        notAskedYet = true;
                    if (PFStuff.wbAttacksAvailable >= requirement - r)
                    {
                        DialogResult dr = DialogResult.No;
                        if (safeModeWB.Checked)
                        {
                            if (notAskedYet)
                            {
                                string lineupNames = "";
                                foreach (int id in lineup)
                                {
                                    lineupNames += " " + Constants.names[id + Constants.heroesInGame];
                                }
                                dr = MessageBox.Show("Automater wants to attack " + attacksToDo + " times with: " + lineupNames +". Continue?" , "WB Attack Confirmation",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                                notAskedYet = false;
                            }
                        }
                        else
                        {
                            dr = DialogResult.Yes;
                        }
                        if (dr == DialogResult.Yes)
                        {
                            PFStuff.WBlineup = lineup;
                            attacksToPerform = (int)attacksToDo;
                            WBAttackTimer.Interval = 10000; //10s
                            WBAttackTimer.Elapsed += WBAttackTimer_Elapsed;
                            WBAttackTimer.Start();
                        }
                    }
                }
            }
        }

        void WBAttackTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (attacksToPerform == 0)
            {
                WBAttackTimer.Stop();
            }
            else
            {
                fightWB();
                attacksToPerform--;
            }
        }

        void fightWB()
        {
            WBLogString += DateTime.Now.ToString() + "\n\t" + PFStuff.WBName + (PFStuff.wbMode == 1 ? " Heroes Allowed" : " No Heroes") + " fought with:";
            foreach (int i in PFStuff.WBlineup)
            {
                WBLogString += " " + Constants.names[i + Constants.heroesInGame];
            }
            WBLogString += "\n";
            if (wbl != null)
            {
                wbl.richTextBox1.Text = WBLogString;
            }
            Thread mt;
            mt = new Thread(pf.sendWBFight);
            mt.Start();
            mt.Join();
        }

        private void autoWBCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (autoWBCheckbox.Checked)
            {
                if ((DateTime.Now - start).TotalSeconds > 3)
                {
                    DialogResult dr = MessageBox.Show("Warning: auto-WB will work correctly only if you enabled your username on webiste. Are you sure you enabled your username?", "WB Name Question",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                    if (dr == DialogResult.Yes)
                    {
                        WBIndicator.BackColor = Color.Green;
                        safeModeWB.Enabled = true;
                    }
                    else
                    {
                        autoWBCheckbox.Checked = false;
                        WBIndicator.BackColor = Color.Red;
                        safeModeWB.Enabled = false;
                    }
                }
                else
                {
                    WBIndicator.BackColor = Color.Green;
                    safeModeWB.Enabled = true;
                }
            }
            else
            {
                WBIndicator.BackColor = Color.Red;
                safeModeWB.Enabled = false;
            }
        }
        private void saveWBDataButton_Click(object sender, EventArgs e)
        {
            var values = wbSettingsCounts.Select(x => (int)x.Value);
            var LOC = WBlineups[0].Select(x => x.Text);
            var MOAK = WBlineups[1].Select(x => x.Text);
            appSettings = AppSettings.loadSettings();
            appSettings.safeModeWBEnabled = safeModeWB.Checked;
            appSettings.autoWBEnabled = autoWBCheckbox.Checked;
            appSettings.WBsettings = values.ToList();
            appSettings.LoCLineup = LOC.ToList();
            appSettings.MOAKLineup = MOAK.ToList();
            appSettings.saveSettings();
        }


        private void WBLogButton_Click(object sender, EventArgs e)
        {
            if (wbl == null)
            {
                wbl = new WBLog(ref WBLogString);
            }
            wbl.Show();
        }
        #endregion

        private void saveAHSettingsButton_Click(object sender, EventArgs e)
        {
            auctionHouse.saveSettings();
        }





    }
}
