using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;
using System.IO;
using System.Net.Http;

namespace CQFollowerAutoclaimer
{
    class AutoLevel
    {
        Form1 main;
        List<NumericUpDown> bank;
        List<ComboBox> heroToLevel;
        List<NumericUpDown> levels;
        internal System.Timers.Timer levelTimer = new System.Timers.Timer();
        internal DateTime nextLevelCheck = new DateTime();
        public AutoLevel(Form1 m)
        {
            main = m;

            m.pranaHeroCombo.Items.Add("");
            m.coinsHeroCombo.Items.Add("");
            m.spheresHeroCombo.Items.Add("");
            m.pranaHeroCombo.Items.AddRange(Constants.pranaHeroes);
            m.coinsHeroCombo.Items.AddRange(Constants.cosmicCoinHeroes);
            m.spheresHeroCombo.Items.AddRange(Constants.ascensionHeroes);

            bank = new List<NumericUpDown>() { m.pranaBankCount, m.coinsBankCount, m.spheresBankCount };
            heroToLevel = new List<ComboBox>() { m.pranaHeroCombo, m.coinsHeroCombo, m.spheresHeroCombo };
            levels = new List<NumericUpDown>() { m.pranaLevelCount, m.coinsLevelCount, m.spheresLevelCount };
            nextLevelCheck = DateTime.Now.AddMinutes(10);
            levelTimer.Interval = 10 * 60 * 1000;
            levelTimer.Elapsed += levelTimer_Elapsed;
            levelTimer.Start();
        }

        async void levelTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            levelTimer.Stop();            
            if (main.autoLevelCheckbox.getCheckState())
            {
                await main.getCurr();
                await main.getData();
                int toSpend = (int)(main.pf.ascensionSpheres - main.spheresBankCount.getValue());
                string onWhat = main.spheresHeroCombo.getText();
                if (toSpend > 0 && !string.IsNullOrEmpty(onWhat))
                {
                    if (onWhat == "Convert to Prana")
                    {
                        for (int i = 0; i < toSpend / 10; i++)
                        {
                            main.taskQueue.Enqueue(() => main.pf.sendConvert(true), "convert");
                        }
                        for (int i = 0; i < toSpend % 10; i++)
                        {
                            main.taskQueue.Enqueue(() => main.pf.sendConvert(false), "convert");
                        }
                    }
                    else
                    {
                        int heroIndex = Array.IndexOf(Constants.heroNames, onWhat) - 2;
                        if (heroIndex != -1 && Constants.heroPrices[heroIndex + 2] == Constants.prices.ASCEND)
                        {                            
                            int heroLevel = PFStuff.heroLevels[heroIndex];
                            if (heroLevel == 99)
                            {
                                MessageBox.Show("Hero " + onWhat + " not owned or already maxed. Please choose different hero");
                            }
                            else
                            {
                                if (heroLevel == 0)
                                {
                                    if (toSpend >= 100)
                                    {
                                        string baseHeroName = onWhat.Substring(1);
                                        baseHeroName = char.ToUpper(baseHeroName[0]) + baseHeroName.Substring(1);
                                        int baseHeroIndex = Array.IndexOf(Constants.heroNames, baseHeroName) - 2;
                                        main.taskQueue.Enqueue(() => main.pf.sendAscendHero(baseHeroIndex), "ascend");
                                        toSpend -= 100;
                                        heroLevel = 1;
                                    }
                                }
                                if (heroLevel > 0)
                                {
                                    while (toSpend > heroLevel && heroLevel < main.spheresLevelCount.Value)
                                    {
                                        main.taskQueue.Enqueue(() => main.pf.sendLevelSuper(heroIndex), "levelAscend");
                                        toSpend -= heroLevel;
                                        heroLevel++;
                                    }
                                }
                            }
                        }
                    }
                }
                toSpend = (int)(main.pf.cosmicCoins - main.coinsBankCount.getValue());
                onWhat = main.coinsHeroCombo.getText();
                if (toSpend > 0 && !string.IsNullOrEmpty(onWhat))
                {
                    int heroIndex = Array.IndexOf(Constants.heroNames, onWhat) - 2;
                    if (heroIndex != -1)
                    {
                        int heroLevel = PFStuff.heroLevels[heroIndex];
                        int pricePerLevel = (int)Constants.heroPrices[heroIndex + 2];
                        if (heroLevel == 0 || heroLevel == 99)
                        {
                            MessageBox.Show("Hero " + onWhat + " not owned or already maxed. Please choose different hero");
                        }
                        else
                        {
                            while (toSpend >= pricePerLevel && heroLevel < main.coinsLevelCount.Value)
                            {
                                if (toSpend >= (pricePerLevel * 10) && heroLevel + 10 <= main.coinsLevelCount.Value)
                                {
                                    main.taskQueue.Enqueue(() => main.pf.sendLevelUp10(heroIndex, "CC") , "levelCC");
                                    toSpend -= 10 * pricePerLevel;
                                    heroLevel += 10;
                                }
                                else
                                {
                                    main.taskQueue.Enqueue(() => main.pf.sendLevelUp(heroIndex, "CC"), "levelCC");
                                    toSpend -= pricePerLevel;
                                    heroLevel++;
                                }
                            }
                        }
                    }
                }
                toSpend = (int)(main.pf.pranaGems - main.pranaBankCount.getValue());
                onWhat = main.pranaHeroCombo.getText();
                if (toSpend > 0 && !string.IsNullOrEmpty(onWhat))
                {
                    int heroIndex = Array.IndexOf(Constants.heroNames, onWhat) - 2;
                    if (heroIndex != -1)
                    {
                        int heroLevel = PFStuff.heroLevels[heroIndex];
                        int pricePerLevel = (int)Constants.heroPrices[heroIndex + 2];
                        if (heroLevel == 0 || heroLevel == 99)
                        {
                            MessageBox.Show("Hero " + onWhat + " not owned or already maxed. Please choose another hero");
                        }
                        else
                        {
                            while (toSpend >= pricePerLevel && heroLevel < main.pranaLevelCount.Value)
                            {
                                if (toSpend >= (pricePerLevel * 10) && heroLevel + 10 <= main.pranaLevelCount.Value)
                                {
                                    main.taskQueue.Enqueue(() => main.pf.sendLevelUp10(heroIndex, "PG"), "levelPG");
                                    toSpend -= 10 * pricePerLevel;
                                    heroLevel += 10;
                                }
                                else
                                {
                                    main.taskQueue.Enqueue(() => main.pf.sendLevelUp(heroIndex, "PG"), "levelPG");
                                    toSpend -= pricePerLevel;
                                    heroLevel++;
                                }
                            }
                        }
                    }
                }
            }
            levelTimer.Interval = 4 * 60 * 60 * 1000;
            nextLevelCheck = DateTime.Now.AddMilliseconds(levelTimer.Interval);
            levelTimer.Start();
        }

        public void loadALSettings()
        {
            main.autoLevelCheckbox.Checked = main.appSettings.autoLevelEnabled ?? false;

            if (main.appSettings.bankedCurrencies != null && main.appSettings.bankedCurrencies.Length == 3)
            {
                int i = 0;
                foreach (int x in main.appSettings.bankedCurrencies)
                {
                    bank[i++].Value = x;
                }
            }

            if (main.appSettings.herosToLevel != null && main.appSettings.herosToLevel.Length == 3)
            {
                int i = 0;
                foreach (string x in main.appSettings.herosToLevel)
                {
                    heroToLevel[i++].Text = x;
                }
            }

            if (main.appSettings.levelLimits != null && main.appSettings.levelLimits.Length == 3)
            {
                int i = 0;
                foreach (int x in main.appSettings.levelLimits)
                {
                    levels[i++].Value = x;
                }
            }
        }

        public void saveALSettings()
        {
            AppSettings apps = AppSettings.loadSettings();
            apps.autoLevelEnabled = main.autoLevelCheckbox.Checked;
            apps.herosToLevel = heroToLevel.Select(x => x.Text).ToArray();
            apps.bankedCurrencies = bank.Select(x => (int)x.Value).ToArray();
            apps.levelLimits = levels.Select(x => (int)x.Value).ToArray();
            apps.saveSettings();
        }


        public static class InternetTime
        {
            public static DateTimeOffset? GetCurrentTime()
            {
                using (var client = new HttpClient())
                {
                    try
                    {
                        var result = client.GetAsync("https://google.com",
                              HttpCompletionOption.ResponseHeadersRead).Result;
                        return result.Headers.Date;
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
        }
    }
}
