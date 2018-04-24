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

namespace CQFollowerAutoclaimer
{
    public partial class Form1 : Form
    {
        static AppSettings appSettings = new AppSettings();
        public const string SettingsFilename = "Settings.json";
        static int currentDQ;
        static string calcOut;
        string token;
        string KongregateId;
        int claimCount = 0;
        static Label[] timeLabels;
        static int chestsToOpen;
        static int attacksToPerform;
        static int DQFailedAttempts;
        long initialFollowers;
        List<List<ComboBox>> WBlineups;
        PFStuff pf;
        string WBLogString = "";
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        DateTime start = DateTime.Now;
        DateTime nextClaim;
        DateTime nextDQTime;
        DateTime nextPVP;
        DateTime nextFreeChest;
        DateTime nextWBRefresh;

        static WBLog wbl;

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

        string[] rewardNames = new string[] {"20 Disasters(not rewarded in game)", "50 Disasters(not rewarded in game)", "200 Disasters(not rewarded in game)", 
            "1H Energy Boost(not rewarded in game)", "4H Energy Boost(not rewarded in game)", "12H Energy Boost(not rewarded in game)", 
            "Common Followers", "Rare Followers", "Legendary Followers",
            "20 UM", "50 UM", "200 UM"};

         string[] names = { "flynn", "leaf", "sparks", "leprechaun", "bavah", "boor", "bylar", "adagda", "hattori", "hirate", "takeda", "hosokawa", "moak", "arigr", "dorth", 
            "rua", "arshen", "aatzar", "apontus",  "bubbles",  "dagda",  "ganah", "toth",  "sexysanta", "santaclaus", "reindeer", "christmaself", "lordofchaos", "ageror", 
            "ageum", "atr0n1x", "aauri", "arei", "aathos", "aalpha", "rigr", "hallinskidi", "hama", "alvitr", "koldis", "sigrun", "neptunius", "lordkirk", "thert", "shygu", 
            "ladyodelith", "dullahan", "jackoknight", "werewolf", "gurth", "koth", "zeth", "atzar", "xarth", "oymos", "gaiabyte", "aoyuki", "spyke", "zaytus", "petry", 
            "chroma", "pontus", "erebus", "ourea", "groth", "brynhildr", "veildur", "geror", "aural", "rudean", "undine", "ignitor", "forestdruid", "geum", "aeris", 
            "aquortis", "tronix", "taurus", "kairy", "james", "nicte", "auri", "faefyr", "ailen", "rei", "geron", "jet", "athos", "nimue", "carl", "alpha", "shaman", 
            "hunter", "bewat", "pyromancer", "rokka", "valor", "nebra", "tiny", "ladyoftwilight", "", 
            "A1", "E1", "F1", "W1", "A2", "E2", "F2", "W2", "A3", "E3", "F3", "W3", "A4", "E4", "F4", "W4", "A5", "E5", "F5", "W5", "A6", "E6", "F6", "W6", 
            "A7", "E7", "F7", "W7", "A8", "E8", "F8", "W8", "A9", "E9", "F9", "W9", "A10", "E10", "F10", "W10", "A11", "E11", "F11", "W11", "A12", "E12", "F12", "W12", 
            "A13", "E13", "F13", "W13", "A14", "E14", "F14", "W14", "A15", "E15", "F15", "W15", "A16","E16","F16","W16","A17","E17","F17","W17","A18","E18","F18","W18",
            "A19","E19","F19","W19","A20","E20","F20","W20","A21","E21","F21","W21", "A22","E22","F22","W22","A23","E23","F23","W23","A24","E24","F24","W24",
            "A25","E25","F25","W25","A26","E26","F26","W26","A27","E27","F27","W27","A28","E28","F28","W28","A29","E29","F29","W29","A30","E30","F30","W30",};

        int heroesInGame;
        string[] heroNames = new string[] { "NULL", "NULL", "Ladyoftwilight", "Tiny", "Nebra", "Valor", "Rokka", "Pyromancer", "Bewat", "Hunter", "Shaman", "Alpha", "Carl", 
            "Nimue", "Athos", "Jet", "Geron", "Rei", "Ailen", "Faefyr", "Auri", "Nicte", "James", "Kairy", "Taurus", "Tronix", "Aquortis", "Aeris", "Geum", "Forestdruid", 
            "Ignitor", "Undine", "Rudean", "Aural", "Geror", "Veildur", "Brynhildr", "Groth", "Ourea", "Erebus", "Pontus", "Chroma", "Petry", "Zaytus", "Spyke", "Aoyuki",
            "Gaiabyte", "Oymos", "Xarth", "Atzar", "Zeth", "Koth", "Gurth", "Werewolf", "Jackoknight", "Dullahan", "Ladyodelith", "Shygu", "Thert", "Lordkirk", "Neptunius", 
            "Sigrun", "Koldis", "Alvitr", "Hama", "Hallinskidi", "Rigr", "Aalpha", "Aathos", "Arei", "Aauri", "Atr0n1x", "Ageum", "Ageror", "Lordofchaos", "Christmaself", 
            "Reindeer", "Santaclaus", "Sexysanta", "Toth", "Ganah", "Dagda", "Bubbles", "Apontus", "Aatzar", "Arshen", "Rua", "Dorth", "Arigr", "Moak", "Hosokawa", "Takeda", 
            "Hirate", "Hattori", "Adagda", "Bylar", "Boor", "Bavah", "Leprechaun", };

        public Form1()
        {
            InitializeComponent();
            timeLabels = new Label[] { claimtime1, claimtime2, claimtime3, claimtime4, claimtime5, claimtime6, claimtime7, claimtime8, claimtime9 };
            enableBoxes = new List<CheckBox> { DQCalcBox, freeChestBox, autoPvPCheckbox, autoWBCheckbox };
            wbSettingsCounts = new List<NumericUpDown> { 
                LOCHARequirementCount, LOCHAAttacksCount, superLOCHAReqCount, superLOCHAAtkCount,
                LOCNHRequirementCount, LOCNHAttacksCount, superLOCNHReqCount, superLOCNHAtkCount,
                MOAKHARequirementCount, MOAKHAAttacksCount, superMOAKHAReqCount, superMOAKHAAtkCount,
                MOAKNHRequirementCount, MOAKNHAttacksCount, superMOAKNHReqCount, superMOAKNHAtkCount
            };
            WBlineups = new List<List<ComboBox>> {
                    new List<ComboBox> {LOCHA1, LOCHA2, LOCHA3, LOCHA4, LOCHA5, LOCHA6},
                    new List<ComboBox> {MOAKHA1, MOAKHA2, MOAKHA3, MOAKHA4, MOAKHA5, MOAKHA6},
                    new List<ComboBox> {DQLineup1, DQLineup2, DQLineup3, DQLineup4, DQLineup5, DQLineup6}
                };
            AutoCompleteStringCollection acsc = new AutoCompleteStringCollection();
            //var namesSorted = names.ToList().Sort();
            //var namesSorted = Array.Sort(names, (x, y) => String.Compare(x, y));
            foreach (List<ComboBox> l in WBlineups)
            {
                foreach (ComboBox c in l)
                {
                    foreach (string n in names.ToList().OrderBy(s=> s))
                    {
                        c.Items.Add(n);
                    }

                }
            }
            heroesInGame = Array.IndexOf(names, "ladyoftwilight") + 2;
            init2();
            if (pf != null)
            {
                PFStuff.getWBData("189");
                PFStuff.getUsername(KongregateId);
                startTimers();
                countdownsTimer.Interval = 1000;
                countdownsTimer.Elapsed += countdownsTimer_Elapsed;
                countdownsTimer.Start();
                logoutTimer.Interval = 24 * 3600 * 1000;
                logoutTimer.Elapsed += logoutTimer_Elapsed;
                logoutTimer.Start();
                WBTimer.Interval = 1000 * 60 * 5; //5 minutes
                WBTimer.Elapsed += WBTimer_Elapsed;
                WBTimer.Start();
                nextWBRefresh = DateTime.Now.AddMilliseconds(WBTimer.Interval);
            }

        }



        private void init2()
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
                freeChestBox.Checked = appSettings.autoChestEnabled ?? false;
                DQBestBox.Checked = appSettings.autoBestDQEnabled ?? false;
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



        #region START

        private void init()
        {
            string boxes = "";
            if (!File.Exists("Newtonsoft.Json.dll"))
            {
                MessageBox.Show("Newtonsoft file not found. Please download it from this project's github");
            }
            if (File.Exists("MacroSettings.txt"))
            {
                System.IO.StreamReader sr = new System.IO.StreamReader("MacroSettings.txt");
                getSetting(sr.ReadLine());
                token = getSetting(sr.ReadLine());
                KongregateId = getSetting(sr.ReadLine());
                sr.ReadLine(); sr.ReadLine();
                boxes = getSetting(sr.ReadLine());
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
                DialogResult dr = MessageBox.Show("MacroSettings file not found. Do you want help with creating one?", "Settings Question",
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
            if (!String.IsNullOrEmpty(boxes))
            {
                string[] states = boxes.Split(',');
                for (int i = 0; i < enableBoxes.Count; i++)
                {
                    enableBoxes[i].Checked = states[i] == "1" ? true : false;
                }
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
            if (PFStuff.freeChestAvailable && freeChestBox.Checked)
            {
                PFStuff.chestMode = "normal";
                openChest();
            }
            nextFreeChest = DateTime.Now.AddSeconds(PFStuff.freeChestRecharge);
            FreeChestTimeLabel.SynchronizedInvoke(() => FreeChestTimeLabel.Text = nextFreeChest.ToString());
            FreeChestTimer.Interval = PFStuff.freeChestAvailable == true ? 6000 : PFStuff.freeChestRecharge * 1000;
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
            rew += PFStuff.chestResult < 0 ? heroNames[-PFStuff.chestResult] : rewardNames[PFStuff.chestResult];
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

                //proc.ErrorDataReceived += proc_DataReceived;
                proc.OutputDataReceived += proc_DataReceived;
                proc.Exited += proc_Exited;
                proc.Start();

                //proc.BeginErrorReadLine();
                proc.BeginOutputReadLine();

                proc.WaitForExit();
            }
            else
            {
                MessageBox.Show("CQMacroCreator.exe or CosmosQuest.exe file not found");
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
                    DQLineup.Add(names[int.Parse(mon[i]["id"].ToString()) + heroesInGame]);
                    WBlineups[2][5 - i].SynchronizedInvoke(() => WBlineups[2][5 - i].Text = names[int.Parse(mon[i]["id"].ToString()) + heroesInGame]);
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
                        temp.Add(Array.IndexOf(names, s) - heroesInGame);
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
                        temp.Add(Array.IndexOf(names, s) - heroesInGame);
                    }
                    lineup = temp.ToArray();
                    break;
                case 4:
                    foreach (ComboBox cb in WBlineups[2])
                    {
                        string s = "";
                        cb.SynchronizedInvoke(() => s = cb.Text);
                        temp.Add(Array.IndexOf(names, s) - heroesInGame);
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
                case("LORD OF CHAOS"):
                    return "LoC";
                case("MOTHER OF ALL KODAMAS"):
                    return "MOAK";
                default:
                    return "Unknowm";
            }
        }

        void WBTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            nextWBRefresh = DateTime.Now.AddMilliseconds(WBTimer.Interval);
            PFStuff.getWebsiteData(KongregateId);
            currentBossLabel.SynchronizedInvoke(() => currentBossLabel.Text = shortBossName(PFStuff.WBName) 
                + (PFStuff.wbMode == 0 ? " NH" : " HA") + ", Attacks left: " + PFStuff.attacksLeft);
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

                    if (PFStuff.wbAttacksAvailable >= requirement - r)
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
            WBLogString += DateTime.Now.ToString() + " " + PFStuff.WBName + (PFStuff.wbMode == 1 ? " Heroes Allowed" : " No Heroes") + " fought with";
            foreach (int i in PFStuff.WBlineup)
            {
                WBLogString += " " + names[i + heroesInGame];
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
                    }
                    else
                    {
                        autoWBCheckbox.Checked = false;
                        WBIndicator.BackColor = Color.Red;
                    }
                }
                else
                {
                    WBIndicator.BackColor = Color.Green;
                }
            }
            else
            {
                WBIndicator.BackColor = Color.Red;
            }
        }
        private void saveWBDataButton_Click(object sender, EventArgs e)
        {
            var values = wbSettingsCounts.Select(x => (int)x.Value);
            var LOC = WBlineups[0].Select(x => x.Text);
            var MOAK = WBlineups[1].Select(x => x.Text);
            appSettings = AppSettings.loadSettings();
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
            appSettings.saveSettings();
        }







    }
}
