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
        public const string SettingsFilename = "Settings.json";

        internal string token;
        internal string KongregateId;

        internal PFStuff pf;
        internal AppSettings appSettings = new AppSettings();
        internal AuctionHouse auctionHouse;
        internal AutoLevel autoLevel;
        internal AutoChests autoChests;
        internal AutoDQ autoDQ;
        internal AutoPvP autopvp;
        internal AutoWB autoWB;
        internal TaskQueue taskQueue = new TaskQueue();

        int claimCount = 0;
        static Label[] timeLabels;
        long initialFollowers;

        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        DateTime start = DateTime.Now;
        DateTime nextClaim;

        internal static WBLog wbl;
        internal List<List<ComboBox>> WBlineups;
        List<ComboBox> auctionComboBoxes;
        List<Label> auctionCountdowns;
        System.Timers.Timer tmr = new System.Timers.Timer();
        System.Timers.Timer countdownsTimer = new System.Timers.Timer();
        System.Timers.Timer logoutTimer = new System.Timers.Timer();  

        List<CheckBox> enableBoxes;
        internal List<NumericUpDown> wbSettingsCounts;

        public Form1()
        {
            InitializeComponent();
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += currentDomain_UnhandledException;

            timeLabels = new Label[] { claimtime1, claimtime2, claimtime3, claimtime4, claimtime5, claimtime6, claimtime7, claimtime8, claimtime9 };
            enableBoxes = new List<CheckBox> { DQCalcBox, freeChestBox, autoPvPCheckbox, autoWBCheckbox };
            wbSettingsCounts = new List<NumericUpDown> { 
                LOCHARequirementCount, LOCHAAttacksCount, superLOCHAReqCount, superLOCHAAtkCount,
                LOCNHRequirementCount, LOCNHAttacksCount, superLOCNHReqCount, superLOCNHAtkCount,
                MOAKHARequirementCount, MOAKHAAttacksCount, superMOAKHAReqCount, superMOAKHAAtkCount,
                MOAKNHRequirementCount, MOAKNHAttacksCount, superMOAKNHReqCount, superMOAKNHAtkCount
            };
            auctionCountdowns = new List<Label> { ahCountdown1, ahCountdown2, ahCountdown3 };
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
                auctionHouse = new AuctionHouse(this);
                autoLevel = new AutoLevel(this);
                autoChests = new AutoChests(this);
                autoDQ = new AutoDQ(this);
                autopvp = new AutoPvP(this);
                autoWB = new AutoWB(this);
                PFStuff.getUsername(KongregateId);
                startTimers();
                countdownsTimer.Interval = 1000;
                countdownsTimer.Elapsed += countdownsTimer_Elapsed;
                countdownsTimer.Start();
                logoutTimer.Interval = 24 * 3600 * 1000;
                logoutTimer.Elapsed += logoutTimer_Elapsed;
                logoutTimer.Start();
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

        internal async Task<bool> login()
        {
            if (!await pf.LoginKong())
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
                return false;
            }
            return true;
        }

        void logoutTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            PlayFabClientAPI.Logout();
        }

        public async void startTimers()
        {
            DateTime[] times = new DateTime[] { DateTime.Now };
            if (!PlayFab.PlayFabClientAPI.IsClientLoggedIn())
            {
                if (!await login())
                    return;
            }
            await getData();
            autoWB.loadWBSettings();
            autoLevel.loadALSettings();
            autopvp.loadPVPSettings();

            times = getTimes(PFStuff.miracleTimes);
            int i = 0;
            foreach (Label l in timeLabels)
            {
                l.SynchronizedInvoke(() => l.Text = times[i++].ToString());
            }
            nextClaim = times.Min();
            nextclaimtime.setText(times.Min().ToString());
            initialFollowers = Int64.Parse(PFStuff.followers);
            tmr.Interval = Math.Max(3000, (times.Min() - DateTime.Now).TotalMilliseconds);
            tmr.Elapsed += OnTmrElapsed;
            tmr.Start();
            
            DQLevelLabel.Text = PFStuff.DQLevel;
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
                    autoDQ.RunCalc();
                }
            }
            else
            {
                DateTime DQRunTime = getTime(PFStuff.DQTime);
                autoDQ.DQTimer.Interval = (autoDQ.nextDQTime < DateTime.Now && DQCalcBox.Checked) ? 8000 : Math.Max(8000, (autoDQ.nextDQTime - DateTime.Now).TotalMilliseconds);
                autoDQ.DQTimer.Start();
            }
            autopvp.nextPVP = getTime(PFStuff.PVPTime);
            PvPTimeLabel.setText(autopvp.nextPVP.ToString());
            autopvp.PVPTimer.Interval = Math.Max(3000, (autopvp.nextPVP - DateTime.Now).TotalMilliseconds);
            autopvp.PVPTimer.Start();
            await getCurr();
            autoChests.nextFreeChest = DateTime.Now.AddSeconds(PFStuff.freeChestRecharge);
            FreeChestTimeLabel.setText(autoChests.nextFreeChest.ToString());
            autoChests.FreeChestTimer.Interval = PFStuff.freeChestRecharge * 1000;
            autoChests.FreeChestTimer.Start();
            autoWB.getWebsiteData();
            foreach (ComboBox c in auctionComboBoxes)
            {
                c.Items.AddRange(auctionHouse.getAvailableHeroes());
            }
            auctionHouse.loadSettings();
        }


        void countdownsTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            time.setText((DateTime.Now - start).ToString("dd\\.hh\\:mm\\:ss"));
            if (nextClaim != null)
            {
                countdown.setText((nextClaim - DateTime.Now).ToString("mm\\:ss"));
                notifyIcon1.Text = "CQ Autoclaimer\nNext claim in " + countdown.Text;
            }
            if (autoDQ.nextDQTime != null)
            {
                DQCountdownLabel.setText((autoDQ.nextDQTime < DateTime.Now ? "-" : "") + (autoDQ.nextDQTime - DateTime.Now).ToString("hh\\:mm\\:ss"));
            }
            if (autoChests.nextFreeChest != null)
            {
                FreeChestCountdownLabel.setText((autoChests.nextFreeChest < DateTime.Now ? "-" : "") + (autoChests.nextFreeChest - DateTime.Now).ToString("hh\\:mm\\:ss"));
            }
            if (autopvp.nextPVP != null)
            {
                PvPCountdownLabel.setText((autopvp.nextPVP < DateTime.Now ? "-" : "") + (autopvp.nextPVP - DateTime.Now).ToString("hh\\:mm\\:ss"));
            }
            if (autoWB.nextWBRefresh != null)
            {
                WBCountdownLabel.setText((autoWB.nextWBRefresh < DateTime.Now ? "-" : "") + (autoWB.nextWBRefresh - DateTime.Now).ToString("hh\\:mm\\:ss"));                
                AHCountdownLabel.setText((autoWB.nextWBRefresh < DateTime.Now ? "-" : "") + (autoWB.nextWBRefresh - DateTime.Now).ToString("hh\\:mm\\:ss"));
            }
            if (autoLevel.nextLevelCheck != null)
            {
                ALCountdownLabel.setText((autoLevel.nextLevelCheck < DateTime.Now ? "-" : "") + (autoLevel.nextLevelCheck - DateTime.Now).ToString("hh\\:mm\\:ss"));                
            }
            for (int i = 0; i < 3; i++)
            {
                if (auctionHouse.auctionDates[i] != null)
                {
                    auctionCountdowns[i].setText((auctionHouse.auctionDates[i] - DateTime.Now).ToString("hh\\:mm\\:ss"));
                }
            }

        }

        #endregion

        #region DATA RETRIEVE

        internal async Task getData()
        {
            await pf.GetGameData2();
            autoDQ.nextDQTime = getTime(PFStuff.DQTime);
            autoDQ.DQTimer.Interval = (autoDQ.nextDQTime < DateTime.Now && DQCalcBox.Checked) ? 4000 : Math.Max(4000, (autoDQ.nextDQTime - DateTime.Now).TotalMilliseconds);
            DQLevelLabel.SynchronizedInvoke(() => DQLevelLabel.Text = PFStuff.DQLevel);
            DQTimeLabel.SynchronizedInvoke(() => DQTimeLabel.Text = autoDQ.nextDQTime.ToString());
        }
        internal void getData3()
        {
            //return await pf.GetGameData();

            Thread mt;
            mt = new Thread(pf.GetGameData);
            mt.Start();
            mt.Join();
            autoDQ.nextDQTime = getTime(PFStuff.DQTime);
            autoDQ.DQTimer.Interval = (autoDQ.nextDQTime < DateTime.Now && DQCalcBox.Checked) ? 4000 : Math.Max(4000, (autoDQ.nextDQTime - DateTime.Now).TotalMilliseconds);
            DQLevelLabel.SynchronizedInvoke(() => DQLevelLabel.Text = PFStuff.DQLevel);
            DQTimeLabel.SynchronizedInvoke(() => DQTimeLabel.Text = autoDQ.nextDQTime.ToString());
        }

        internal async Task<bool> getCurr()
        {
            bool b = false;
            while(!(b = await pf.getCurrencies2())) {}

            if (PFStuff.freeChestAvailable && freeChestBox.Checked)
            {
                taskQueue.Enqueue(() => pf.sendOpen("normal"), "chest");
            }
            autoChests.nextFreeChest = DateTime.Now.AddSeconds(PFStuff.freeChestRecharge);

            FreeChestTimeLabel.setText(autoChests.nextFreeChest.ToString());            
            NormalChestLabel.setText(PFStuff.normalChests.ToString());
            HeroChestLabel.setText(PFStuff.heroChests.ToString());

            autoChests.FreeChestTimer.Interval = PFStuff.freeChestAvailable == true ? 6000 : Math.Max(4000, PFStuff.freeChestRecharge * 1000);
            return b;
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

        static public DateTime getTime(string t)
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
        async void FreeChestTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!PlayFab.PlayFabClientAPI.IsClientLoggedIn())
            {
                await login();
            }
            await getCurr();
        }

        private void openNormalButton_Click(object sender, EventArgs e)
        {
            openNormalButton.SynchronizedInvoke(() => openNormalButton.Enabled = false);
            openHeroButton.SynchronizedInvoke(() => openHeroButton.Enabled = false);
            for (int i = 0; i < chestToOpenCount.Value; i++)
            {
                taskQueue.Enqueue(() => autoChests.openChest("normal"), "chest");
            }
        }

        private void openHeroButton_Click(object sender, EventArgs e)
        {
            openNormalButton.SynchronizedInvoke(() => openNormalButton.Enabled = false);
            openHeroButton.SynchronizedInvoke(() => openHeroButton.Enabled = false);
            for (int i = 0; i < chestToOpenCount.Value; i++)
            {
                taskQueue.Enqueue(() => autoChests.openChest("hero"), "chest");
            }
        }

        private async void refreshChestsButton_Click(object sender, EventArgs e)
        {
            await getCurr();
        }

        private async void freeChestBox_CheckedChanged(object sender, EventArgs e)
        {
            if (freeChestBox.Checked)
            {
                chestIndicator.BackColor = Color.Green;
                if ((DateTime.Now - start).TotalSeconds > 3)
                {
                    await getCurr();
                }
            }
            else
            {
                chestIndicator.BackColor = Color.Red;
            }
        }



        #endregion

        #region PVP

        private async void autoPvPCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (autoPvPCheckbox.Checked)
            {
                PVPIndicator.BackColor = Color.Green;
                if ((DateTime.Now - start).TotalSeconds > 3)
                {
                    await getData();
                    autopvp.nextPVP = getTime(PFStuff.PVPTime);
                    PvPTimeLabel.setText(autopvp.nextPVP.ToString());
                    autopvp.PVPTimer.Interval = Math.Max(8000, (autopvp.nextPVP - DateTime.Now).TotalMilliseconds);
                }
            }
            else
            {
                PVPIndicator.BackColor = Color.Red;
            }
        }
        #endregion

        #region DQ
        
        private async void DQRefreshButton_Click(object sender, EventArgs e)
        {
            await getData();
        }

        private void DQCalcBox_CheckedChanged(object sender, EventArgs e)
        {

            if (DQCalcBox.Checked)
            {
                DQIndicator.BackColor = Color.Green;
            }
            else
            {
                if (DQBestBox.Checked)
                {
                    DQIndicator.BackColor = Color.Yellow;
                }
                else
                {
                    DQIndicator.BackColor = Color.Red;
                }
            }
            if (DQCalcBox.Checked && (DateTime.Now - start).TotalSeconds > 3)
            {
                DialogResult dr = MessageBox.Show("Do you want to run the auto-solve now?", "Calc Question", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if (dr == DialogResult.Yes)
                {
                    autoDQ.fightDQWithPresetLineup();
                }
            }
        }       
              
        private void runCalcButton_Click(object sender, EventArgs e)
        {
            autoDQ.fightDQWithPresetLineup();
        }
        #endregion
        
        #region MIRACLES
        async private void OnTmrElapsed(Object source, System.Timers.ElapsedEventArgs e)
        {
            if (!PlayFab.PlayFabClientAPI.IsClientLoggedIn())
            {
                await login();
            }
            taskQueue.Enqueue(() => claimMiracles(), "miracle");
        }
        public async Task<bool> claimMiracles()
        {
            if (!PlayFab.PlayFabClientAPI.IsClientLoggedIn())
            {
                await login();
            }
            bool b = await pf.sendClaimAll();
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
            followersClaimed.SynchronizedInvoke(() => followersClaimed.Text = (Int64.Parse(PFStuff.followers) - initialFollowers).ToString());
            tmr.Interval = Math.Max(1000, (times.Min() - DateTime.Now).TotalMilliseconds);
            return b;
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

        internal int[] getLineup(int ID, uint followers)
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

        private void autoWBCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (autoWBCheckbox.Checked)
            {
                if ((DateTime.Now - start).TotalSeconds > 10)
                {
                    DialogResult dr = MessageBox.Show("Warning: auto-WB will work correctly only if you enabled your username on website. Are you sure you've enabled your username?", "WB Name Question",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                    if (dr == DialogResult.Yes)
                    {
                        WBIndicator.BackColor = safeModeWB.Checked ? Color.Yellow : Color.Green;
                        safeModeWB.Enabled = true;
                    }
                    else
                    {
                        autoWBCheckbox.Checked = false;
                        WBIndicator.BackColor = Color.Red;
                        //safeModeWB.Enabled = false;
                    }
                }
                else
                {
                    WBIndicator.BackColor = safeModeWB.Checked ? Color.Yellow : Color.Green;
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
                wbl = new WBLog(ref autoWB.WBLogString);
            }
            wbl.Show();
        }

        #endregion

        private void saveAHSettingsButton_Click(object sender, EventArgs e)
        {
            auctionHouse.saveSettings();
        }

        private async void auctionHero1Combo_SelectedIndexChanged(object sender, EventArgs e)
        {
            await auctionHouse.getAuctionInterval();
        }

        private void auctionHero1Box_CheckedChanged(object sender, EventArgs e)
        {
            if (auctionHero1Box.Checked)
            {
                ah1Indicator.BackColor = Color.Green;
            }
            else
            {
                ah1Indicator.BackColor = Color.Red;
            }
        }

        private void auctionHero2Box_CheckedChanged(object sender, EventArgs e)
        {
            if (auctionHero2Box.Checked)
            {
                ah2Indicator.BackColor = Color.Green;
            }
            else
            {
                ah2Indicator.BackColor = Color.Red;
            }
        }

        private void auctionHero3Box_CheckedChanged(object sender, EventArgs e)
        {
            if (auctionHero3Box.Checked)
            {
                ah3Indicator.BackColor = Color.Green;
            }
            else
            {
                ah3Indicator.BackColor = Color.Red;
            }
        }

        private void DQBestBox_CheckedChanged(object sender, EventArgs e)
        {
            if (DQCalcBox.Checked)
            {
                DQIndicator.BackColor = Color.Green;
            }
            else if (DQBestBox.Checked)
            {
                DQIndicator.BackColor = Color.Yellow;
            }
            else
            {
                DQIndicator.BackColor = Color.Red;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            autoLevel.saveALSettings();
        }

        private void autoLevelCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if(autoLevelCheckbox.Checked)
            {
                ALIndicator.BackColor = Color.Green;
                autoLevel.levelTimer.Interval = 60 * 1000;
                autoLevel.nextLevelCheck = DateTime.Now.AddMilliseconds(autoLevel.levelTimer.Interval);
            }
            else
            {
                ALIndicator.BackColor = Color.Red;
            }
        }

        private void safeModeWB_CheckedChanged(object sender, EventArgs e)
        {
            if (safeModeWB.Checked && autoWBCheckbox.Checked)
            {
                WBIndicator.BackColor = Color.Orange;
            }
            else if (autoWBCheckbox.Checked)
            {
                WBIndicator.BackColor = Color.Green;
            }
            else
            {
                WBIndicator.BackColor = Color.Red;
            }
        }
    }
}
