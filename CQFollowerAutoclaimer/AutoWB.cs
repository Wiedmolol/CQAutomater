using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.IO;
using System.Windows.Forms;

namespace CQFollowerAutoclaimer
{
    class AutoWB
    {
        Form1 main;
        public System.Timers.Timer WBTimer = new System.Timers.Timer();
        internal DateTime nextWBRefresh;
        static bool notAskedYet = true;
        internal string WBLogString = "";

        public AutoWB(Form1 m)
        {
            main = m;
            WBTimer.Interval = 10 * 1000;
            WBTimer.Elapsed += WBTimer_Elapsed;
            WBTimer.Start();
            nextWBRefresh = DateTime.Now.AddMilliseconds(WBTimer.Interval);
        }

        internal void loadWBSettings()
        {
            main.autoWBCheckbox.Checked = main.appSettings.autoWBEnabled ?? false;
            main.safeModeWB.Checked = main.appSettings.safeModeWBEnabled ?? false;
            main.waitAutoLevelBox.Checked = main.appSettings.waitAutoLevel ?? false;
            if (main.appSettings.WBsettings != null)
            {
                for (int i = 0; i < main.appSettings.WBsettings.Count; i++)
                {
                    main.wbSettingsCounts[i].Value = main.appSettings.WBsettings[i];
                }
            }
            if (main.appSettings.LoCLineup != null)
            {
                for (int i = 0; i < main.appSettings.LoCLineup.Count; i++)
                {
                    main.WBlineups[0][i].Text = main.appSettings.LoCLineup[i];
                }
            }
            if (main.appSettings.MOAKLineup != null)
            {
                for (int i = 0; i < main.appSettings.MOAKLineup.Count; i++)
                {
                    main.WBlineups[1][i].Text = main.appSettings.MOAKLineup[i];
                }
            }
            if (main.appSettings.KrytonLineup != null)
            {
                for (int i = 0; i < main.appSettings.KrytonLineup.Count; i++)
                {
                    main.WBlineups[3][i].Text = main.appSettings.KrytonLineup[i];
                }
            }
        }

        async void WBTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            getWebsiteData();
            if (main.autoWBCheckbox.Checked)
            {
                decimal attacksToDo = 0;
                decimal requirement = 99;
                int[] lineup = new int[2];
                int r = await PFStuff.getWBData((PFStuff.WB_ID).ToString());
                main.userWBInfo.setText("Your current damage: " + PFStuff.wbDamageDealt + " with " + r + " attacks");
                if (r == -2)
                {
                    using (StreamWriter sw = new StreamWriter("ActionLog.txt", true))
                    {
                        sw.WriteLine(DateTime.Now + "\n\tError when downloading the WB data");
                    }
                   
                }
                else if (r == -1)
                {
                    MessageBox.Show("You haven't enabled your username on website. Auto-WB won't work without enabled username.");
                }
                else
                {
                    if (PFStuff.WBName.Contains("LORD OF CHAOS") && PFStuff.wbMode == 0) //loc no heroes
                    {
                        if (PFStuff.WBName.Contains("SUPER"))
                        {                           
                            attacksToDo = main.superLOCNHAtkCount.Value;
                            requirement = main.superLOCNHReqCount.Value;                            
                        }
                        else
                        {
                            attacksToDo = main.LOCNHAttacksCount.Value;
                            requirement = main.LOCNHRequirementCount.Value;
                        }
                        lineup = main.getLineup(0, uint.Parse(PFStuff.followers));
                    }
                    else if (PFStuff.WBName.Contains("LORD OF CHAOS") && PFStuff.wbMode == 1) //loc heroes allowed
                    {
                        if (PFStuff.WBName.Contains("SUPER"))
                        {
                            attacksToDo = main.superLOCHAAtkCount.Value;
                            requirement = main.superLOCHAReqCount.Value;
                        }
                        else
                        {
                            attacksToDo = main.LOCHAAttacksCount.Value;
                            requirement = main.LOCHARequirementCount.Value;
                        }
                        lineup = main.getLineup(1, uint.Parse(PFStuff.followers));
                    }
                    else if (PFStuff.WBName.Contains("MOTHER OF ALL KODAMAS") && PFStuff.wbMode == 0) //moak no heroes
                    {
                        if (PFStuff.WBName.Contains("SUPER"))
                        {
                            attacksToDo = main.superMOAKNHAtkCount.Value;
                            requirement = main.superMOAKNHReqCount.Value;
                        }
                        else
                        {
                            attacksToDo = main.MOAKNHAttacksCount.Value;
                            requirement = main.MOAKNHRequirementCount.Value;
                        }
                        lineup = main.getLineup(2, uint.Parse(PFStuff.followers));
                    }
                    else if (PFStuff.WBName.Contains("MOTHER OF ALL KODAMAS") && PFStuff.wbMode == 1) //moak heroes allowed
                    {
                        if (PFStuff.WBName.Contains("SUPER"))
                        {
                            attacksToDo = main.superMOAKHAAtkCount.Value;
                            requirement = main.superMOAKHAReqCount.Value;
                        }
                        else
                        {
                            attacksToDo = main.MOAKHAAttacksCount.Value;
                            requirement = main.MOAKHARequirementCount.Value;
                        }
                        lineup = main.getLineup(3, uint.Parse(PFStuff.followers));
                    }
                    else if (PFStuff.WBName.Contains("KRYTON") && PFStuff.wbMode == 0) //kryton no heroes
                    {
                        if (PFStuff.WBName.Contains("SUPER"))
                        {
                            attacksToDo = main.SuperKrytonNHAttacksCount.Value;
                            requirement = main.SuperKrytonNHReqCount.Value;
                        }
                        else
                        {
                            attacksToDo = main.KrytonNHAttacksCount.Value;
                            requirement = main.KrytonNHReqCount.Value;
                        }
                        lineup = main.getLineup(5, uint.Parse(PFStuff.followers));
                    }
                    else if (PFStuff.WBName.Contains("KRYTON") && PFStuff.wbMode == 1) //kryton heroes allowed
                    {
                        if (PFStuff.WBName.Contains("SUPER"))
                        {
                            attacksToDo = main.SuperKrytonHAAttacksCount.Value;
                            requirement = main.SuperKrytonHAReqCount.Value;
                        }
                        else
                        {
                            attacksToDo = main.KrytonHAAttacksCount.Value;
                            requirement = main.KrytonHAReqCount.Value;
                        }
                        lineup = main.getLineup(6, uint.Parse(PFStuff.followers));
                    }
                    else
                    {
                        return;
                    }

                    if (lineup.Contains(-1))
                    {
                        MessageBox.Show("You have empty slots in your lineup. You must use all 6 slots in your lineup. Auto-WB disabled.");
                        main.autoWBCheckbox.Checked = false;
                        return;
                    }
                    attacksToDo -= r;
                    if (attacksToDo <= 0)
                        return;
                    await main.getData();
                    if (PFStuff.WBchanged)
                    {
                        notAskedYet = true;
                        main.autoLevel.levelTimer.Interval = 4 * 60 * 1000;
                        main.autoLevel.nextLevelCheck = DateTime.Now.AddMilliseconds(main.autoLevel.levelTimer.Interval);
                        if (main.waitAutoLevelBox.Checked && PFStuff.wbMode == 1) {
                            WBTimer.Interval = 5 * 60 * 1000;
                            return;
                        }
                    }
                    int attacksAvailable = PFStuff.wbAttacksAvailable + ((PFStuff.wbAttacksAvailable == 7 && PFStuff.wbAttackNext < DateTime.Now) ? 1 : 0);
                    if ((attacksAvailable >= requirement - r && attacksToDo < (PFStuff.attacksLeft - 5)) && !(r == 0 && PFStuff.wbDamageDealt != 0 ) && !(r != 0 && PFStuff.wbDamageDealt == 0))
                    {
                        DialogResult dr = DialogResult.No;
                        if (main.safeModeWB.Checked)
                        {
                            if (notAskedYet)
                            {
                                string lineupNames = "";
                                foreach (int id in lineup)
                                {
                                    lineupNames += " " + Constants.names[id + Constants.heroesInGame];
                                }
                                dr = MessageBox.Show("Automater wants to attack " + attacksToDo + " times with: " + lineupNames + ". Continue?", "WB Attack Confirmation",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                                notAskedYet = false;
                            }
                        }
                        else
                        {
                            dr = DialogResult.Yes;
                        }
                        if (dr == DialogResult.Yes && !main.taskQueue.Contains("WB")) //enqueue new attacks only if there are no attacks in queue already
                        {
                            for (int i = 0; i < Math.Min(attacksToDo, attacksAvailable); i++)
                            {
                                main.taskQueue.Enqueue(() => fightWB(lineup), "WB");
                            }
                        }
                    }
                }
            }
        }


        internal async Task<bool> fightWB(int[] lineup)
        {
            bool b = await main.pf.sendWBFight(lineup);
            string s = "";
            if (b)
            {
                s = DateTime.Now.ToString() + "\n\t" + PFStuff.WBName + (PFStuff.wbMode == 1 ? " Heroes Allowed" : " No Heroes") + " fought with:";
                foreach (int i in lineup)
                {
                    s += " " + Constants.names[i + Constants.heroesInGame];
                    if(i<0)
                    {
                        s += ":" + PFStuff.heroLevels[-i-2];
                    }
                }
                WBLogString += s + "\n";
            }
            else
            {
                s = DateTime.Now.ToString() + "\n\tFailed to attack\n";
                WBLogString += s;
            }
            if (Form1.wbl != null)
            {
                Form1.wbl.richTextBox1.setText(WBLogString);
            }
            using (StreamWriter sw = new StreamWriter("ActionLog.txt", true))
            {
                sw.WriteLine(s);
            }
            return b;
        }

        public async void getWebsiteData()
        {
            PFStuff.getWebsiteData(main.KongregateId);
            main.currentBossLabel.setText(shortBossName(PFStuff.WBName) + (PFStuff.wbMode == 0 ? " NH" : " HA") + ", Attacks left: " + PFStuff.attacksLeft);
            main.auctionHouse.loadAuctions(false);
            double x = await main.auctionHouse.getAuctionInterval();
            WBTimer.Interval = Math.Min(Math.Max(PFStuff.attacksLeft * 2500, 5000), x);
            nextWBRefresh = DateTime.Now.AddMilliseconds(WBTimer.Interval);
            main.currentDungLevelLabel.Text = PFStuff.DungLevel;
        }


        string shortBossName(string longName)
        {
            switch (longName)
            {
                case ("LORD OF CHAOS"):
                    return "LoC";
                case ("MOTHER OF ALL KODAMAS"):
                    return "MOAK";
                case("KRYTON"):
                    return "KRYTON";
                default:
                    return "Unknowm";
            }
        }
    }
}
