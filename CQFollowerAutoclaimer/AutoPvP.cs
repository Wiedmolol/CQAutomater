using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace CQFollowerAutoclaimer
{
    class AutoPvP
    {
        internal DateTime nextPVP;
        internal System.Timers.Timer PVPTimer = new System.Timers.Timer();
        Form1 main;
        
        public AutoPvP(Form1 m)
        {
            main = m;
            PVPTimer.Elapsed += PVPTimer_Elapsed;
        }

        internal void loadPVPSettings()
        {
            main.autoPvPCheckbox.Checked = main.appSettings.autoPvPEnabled ?? false;
            main.playersBelowCount.Value = main.appSettings.pvpLowerLimit ?? 4;
            main.playersAboveCount.Value = main.appSettings.pvpUpperLimit ?? 5;
        }

        async void PVPTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (main.autoPvPCheckbox.Checked)
            {
                PVPTimer.Stop();
                if (!PlayFab.PlayFabClientAPI.IsClientLoggedIn())
                {
                    await main.login();
                }
                int size = Math.Max(3, 2 * (int)Math.Max(main.playersAboveCount.Value, main.playersBelowCount.Value + 1));
                while (!await main.pf.getLeaderboard(size)) ;
                Random r = new Random();
                int index;
                do
                {
                    index = r.Next(0, PFStuff.nearbyPlayersIDs.Length);
                } while (index == PFStuff.userIndex ||
                        index > PFStuff.userIndex + (int)main.playersBelowCount.Value ||
                        index < PFStuff.userIndex - (int)main.playersAboveCount.Value);

                main.taskQueue.Enqueue(() => sendFight(index), "PVP");
            }
        }

        internal async Task<bool> sendFight(int index)
        {
            bool b = await main.pf.sendPVPFight(index);
            nextPVP = Form1.getTime(PFStuff.PVPTime);
            PVPTimer.Interval = Math.Max(5000, (nextPVP - DateTime.Now).TotalMilliseconds);
            main.PvPLog.SynchronizedInvoke(() => main.PvPLog.AppendText(PFStuff.battleResult));
            main.PvPTimeLabel.setText(nextPVP.ToString());
            PVPTimer.Start();
            return b;
        }
    }
}
