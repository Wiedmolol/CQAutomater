using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;
using System.Threading;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.IO;
using System.Net.Http;
namespace CQFollowerAutoclaimer
{
    class PFStuff
    {
        string token;
        static string kongID;
        static int requestsSent = 0;
        static public string miracleTimes;
        static public string followers;
        static public string DQTime;
        static public string DQLevel;
        static public bool DQResult;
        static public string PVPTime;
        static public int[] heroLevels;

        static public string[] nearbyPlayersIDs;
        static public string username;
        static public int userIndex;
        static public int freeChestRecharge;

        static public string battleResult;
        static public int chestResult;

        static public int normalChests;
        static public int heroChests;
        public int ascensionSpheres;
        public int pranaGems;
        public int cosmicCoins;

        static public bool freeChestAvailable = false;

        static public int wbDamageDealt;
        static public int wbMode;
        static public int wbAttacksAvailable;
        static public DateTime wbAttackNext;
        static public int WB_ID = 1;
        static public string WBName;
        static public int attacksLeft;

        static public bool WBchanged = false;
        static public JArray auctionData;

        public PFStuff(string t, string kid)
        {
            token = t;
            kongID = kid;
        }

        private int[] getArray(string s)
        {
            s = Regex.Replace(s, @"\s+", "");
            s = s.Substring(1, s.Length - 2);
            int[] result = s.Split(',').Select(n => Convert.ToInt32(n)).ToArray();
            return result;
        }

        private void logError(string err, string msg)
        {
            using (StreamWriter sw = new StreamWriter("ErrorLog", true))
            {
                sw.WriteLine(DateTime.Now);
                sw.WriteLine("\tError " + err + " " + msg);
            }
        }

        #region Getting Data
        public async Task<bool> LoginKong()
        {
            PlayFabSettings.TitleId = "E3FA";
            var request = new LoginWithKongregateRequest
            {
                AuthTicket = token,
                CreateAccount = false,
                KongregateId = kongID,
            };
            var loginTask = await PlayFabClientAPI.LoginWithKongregateAsync(request);
            var apiError = loginTask.Error;
            var apiResult = loginTask.Result;
            if (apiError != null)
            {
                MessageBox.Show("Failed to log in. Error: " + apiError.ErrorMessage);
                return false;
            }
            else if (apiResult != null)
            {
                return true;
            }
            return false;
        }

        public async Task<bool> GetGameData2()
        {
            var request = new ExecuteCloudScriptRequest()
            {
                RevisionSelection = CloudScriptRevisionOption.Live,
                FunctionName = "status",
                FunctionParameter = new { token = token, kid = kongID }
            };
            var statusTask = await PlayFabClientAPI.ExecuteCloudScriptAsync(request);
            if (statusTask.Error != null)
            {
                logError(statusTask.Error.Error.ToString(), statusTask.Error.ErrorMessage);
                return false;
            }
            if (statusTask.Result == null || statusTask.Result.FunctionResult == null || !statusTask.Result.FunctionResult.ToString().Contains("true"))
            {
                logError("Cloud Script Error", statusTask.Result.ToString());
                return false;
            }
            else
            {
                JObject json = JObject.Parse(statusTask.Result.FunctionResult.ToString());
                string hLevels = json["data"]["city"]["hero"].ToString();
                heroLevels = getArray(hLevels);
                miracleTimes = json["data"]["miracles"].ToString();
                followers = json["data"]["followers"].ToString();
                DQTime = json["data"]["city"]["daily"]["timer2"].ToString();
                DQLevel = json["data"]["city"]["daily"]["lvl"].ToString();
                PVPTime = json["data"]["city"]["nextfight"].ToString();
                wbAttacksAvailable = int.Parse(json["data"]["city"]["WB"]["atks"].ToString());
                wbAttackNext = Form1.getTime(json["data"]["city"]["WB"]["next"].ToString());
                return true;
            }
        }

        public void GetGameData()
        {
            var request = new ExecuteCloudScriptRequest()
            {
                RevisionSelection = CloudScriptRevisionOption.Live,
                FunctionName = "status",
                FunctionParameter = new { token = token, kid = kongID }
            };
            var statusTask = PlayFabClientAPI.ExecuteCloudScriptAsync(request);
            bool _running = true;
            while (_running)
            {
                if (statusTask.IsCompleted)
                {
                    var apiError = statusTask.Result.Error;
                    var apiResult = statusTask.Result.Result;

                    if (apiError != null)
                    {
                        return;
                    }
                    else if (apiResult.FunctionResult != null)
                    {
                        JObject json = JObject.Parse(apiResult.FunctionResult.ToString());
                        string hLevels = json["data"]["city"]["hero"].ToString();
                        heroLevels = getArray(hLevels);
                        miracleTimes = json["data"]["miracles"].ToString();
                        followers = json["data"]["followers"].ToString();
                        DQTime = json["data"]["city"]["daily"]["timer2"].ToString();
                        DQLevel = json["data"]["city"]["daily"]["lvl"].ToString();
                        PVPTime = json["data"]["city"]["nextfight"].ToString();
                        wbAttacksAvailable = int.Parse(json["data"]["city"]["WB"]["atks"].ToString());
                        wbAttackNext = Form1.getTime(json["data"]["city"]["WB"]["next"].ToString());
                        return;
                    }
                    _running = false;
                }
                Thread.Sleep(1);
            }
            return;
        }

        public async Task<bool> getLeaderboard(int size)
        {
            Console.Write("\n\nLeaderboard get");
            nearbyPlayersIDs = new string[size];
            var request = new GetLeaderboardAroundPlayerRequest
            {
                StatisticName = "Ranking",
                MaxResultsCount = size
            };
            var leaderboardTask = await PlayFabClientAPI.GetLeaderboardAroundPlayerAsync(request);
            if (leaderboardTask.Error != null)
            {
                logError(leaderboardTask.Error.Error.ToString(), leaderboardTask.Error.ErrorMessage);
                return false;
            }
            if (leaderboardTask == null || leaderboardTask.Result == null)
            {
                logError("Leaderboard Error", leaderboardTask.Result.ToString());
                return false;
            }
            else
            {
                for (int i = 0; i < size; i++)
                {
                    nearbyPlayersIDs[i] = leaderboardTask.Result.Leaderboard[i].PlayFabId;
                    if (leaderboardTask.Result.Leaderboard[i].DisplayName == username)
                    {
                        userIndex = i;
                    }
                }
                return true;
            }
        }

        //public void getLeaderboard()
        //{
        //    nearbyPlayersIDs = new string[LeaderboardRange];
        //    var request = new GetLeaderboardAroundPlayerRequest
        //    {
        //        StatisticName = "Ranking",
        //        MaxResultsCount = LeaderboardRange
        //    };
        //    var leaderboardTask = PlayFabClientAPI.GetLeaderboardAroundPlayerAsync(request);
        //    bool _running = true;
        //    while (_running)
        //    {
        //        if (leaderboardTask.IsCompleted)
        //        {
        //            var apiError = leaderboardTask.Result.Error;
        //            var apiResult = leaderboardTask.Result.Result;

        //            if (apiError != null)
        //            {
        //                return;
        //            }
        //            else if (apiResult != null)
        //            {
        //                for (int i = 0; i < LeaderboardRange; i++)
        //                {
        //                    nearbyPlayersIDs[i] = apiResult.Leaderboard[i].PlayFabId;
        //                    if (apiResult.Leaderboard[i].DisplayName == username)
        //                    {
        //                        userIndex = i;
        //                    }
        //                }
        //                return;
        //            }
        //            _running = false;
        //        }
        //        Thread.Sleep(1);
        //    }
        //    return;
        //}

        public async Task<bool> getCurrencies2()
        {
            var request = new GetUserInventoryRequest();
            var currenciesTask = await PlayFabClientAPI.GetUserInventoryAsync(request);
            if (currenciesTask.Error != null)
            {
                logError(currenciesTask.Error.Error.ToString(), currenciesTask.Error.ErrorMessage);
                return false;
            }
            else if (currenciesTask.Result != null)
            {
                freeChestRecharge = currenciesTask.Result.VirtualCurrencyRechargeTimes["BK"].SecondsToRecharge;
                normalChests = int.Parse(currenciesTask.Result.VirtualCurrency["PK"].ToString());
                heroChests = int.Parse(currenciesTask.Result.VirtualCurrency["KU"].ToString()) / 10;
                freeChestAvailable = currenciesTask.Result.VirtualCurrency["BK"].ToString() == "1" ? true : false;
                return true;
            }
            return false;
        }

        public void getCurrencies()
        {
            var request = new GetUserInventoryRequest();
            var currenciesTask = PlayFabClientAPI.GetUserInventoryAsync(request);
            bool _running = true;
            do
            {
                while (_running)
                {
                    if (currenciesTask.IsCompleted)
                    {
                        var apiError = currenciesTask.Result.Error;
                        var apiResult = currenciesTask.Result.Result;

                        if (apiError != null)
                        {
                            return;
                        }
                        else if (apiResult != null)
                        {
                            freeChestRecharge = apiResult.VirtualCurrencyRechargeTimes["BK"].SecondsToRecharge;
                            normalChests = int.Parse(apiResult.VirtualCurrency["PK"].ToString());
                            pranaGems = int.Parse(apiResult.VirtualCurrency["PG"].ToString());
                            cosmicCoins = int.Parse(apiResult.VirtualCurrency["CC"].ToString());
                            ascensionSpheres = int.Parse(apiResult.VirtualCurrency["AS"].ToString());
                            heroChests = int.Parse(apiResult.VirtualCurrency["KU"].ToString()) / 10;
                            freeChestAvailable = apiResult.VirtualCurrency["BK"].ToString() == "1" ? true : false;
                            return;
                        }
                        _running = false;
                    }
                    Thread.Sleep(1);
                }
            } while (currenciesTask.Status != TaskStatus.RanToCompletion);
            return;
        }

        internal static void getUsername(string id)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(@"http://api.kongregate.com/api/user_info.json?user_id=" + id);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            string content = new StreamReader(response.GetResponseStream()).ReadToEnd();

            JObject json = JObject.Parse(content);
            username = json["username"].ToString();
        }

        internal static void getWebsiteData(string id)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(@"https://cosmosquest.net/public.php?kid=" + id);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                string content = new StreamReader(response.GetResponseStream()).ReadToEnd();

                JObject json = JObject.Parse(content);
                var WBData = json["WB"];
                auctionData = (JArray)json["auction"];
                wbDamageDealt = int.Parse(WBData["dealt"].ToString());
                wbMode = int.Parse(WBData["mode"].ToString());
                WBName = WBData["name"].ToString();
                attacksLeft = int.Parse(WBData["atk"].ToString());


                HttpWebRequest request2 = (HttpWebRequest)WebRequest.Create(@"https://cosmosquest.net/wb.php");
                HttpWebResponse response2 = (HttpWebResponse)request2.GetResponse();
                string content2 = new StreamReader(response2.GetResponseStream()).ReadToEnd();
                string a = Regex.Match(content2, "(?<=<a href.*>).*?(?=</a>)").ToString();
                WBchanged = (WB_ID != int.Parse(a)) ? true : false;
                if (requestsSent++ == 1)
                {
                    WBchanged = true;
                }
                WB_ID = int.Parse(a);
            }
            catch (WebException webex)
            {
                Console.Write(webex.Message);
            }
        }

        internal async Task<int> getWBData2(string id)
        {
            int retryCount = 4;
            while (retryCount > 0)
            {
                try
                {
                    if (username == null)
                    {
                        getUsername(kongID);
                    }
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(@"https://cosmosquest.net/wb.php?id=" + id);
                    var response = await request.GetResponseAsync();

                    string content = new StreamReader(response.GetResponseStream()).ReadToEnd();
                    string a = Regex.Match(content, username + ".*?</tr>").ToString();
                    var b = Regex.Matches(a, "(?<=\"small\">).*?(?=</td>)");
                    if (string.IsNullOrEmpty(a) || b.Count == 0)
                    {
                        retryCount--;
                    }
                    else
                    {
                        return int.Parse(b[1].ToString().Replace(".", ""));
                    }
                }
                catch (Exception wbDataException)
                {
                    retryCount--;
                    Console.Write(wbDataException.Message);
                }
            }
            return 0;
        }

        internal static int getWBData(string id)
        {
            if (username == null)
            {
                getUsername(kongID);
            }

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(@"https://cosmosquest.net/wb.php?id=" + id);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            string content = new StreamReader(response.GetResponseStream()).ReadToEnd();
            string a = Regex.Match(content, username + ".*?</tr>").ToString();
            var b = Regex.Matches(a, "(?<=\"small\">).*?(?=</td>)");
            if (string.IsNullOrEmpty(a) || b.Count == 0)
            {
                return 0;
            }
            return int.Parse(b[1].ToString().Replace(".", ""));
        }
        #endregion

        #region Sending requests
        public void sendBuyWC()
        {
            var request = new ExecuteCloudScriptRequest()
            {
                RevisionSelection = CloudScriptRevisionOption.Live,
                FunctionName = "buywc",
                FunctionParameter = new { wc = 0 }
            };
            var statusTask = PlayFabClientAPI.ExecuteCloudScriptAsync(request);
            bool _running = true;
            while (_running)
            {
                if (statusTask.IsCompleted)
                {
                    var apiError = statusTask.Result.Error;
                    var apiResult = statusTask.Result.Result;

                    if (apiError != null)
                    {
                        return;
                    }
                    else if (apiResult != null)
                    {
                        return;
                    }
                    _running = false;
                }
                Thread.Sleep(1);
            }
            return;
        }

        public async Task<bool> sendClaimAll()
        {
            var request = new ExecuteCloudScriptRequest()
            {
                RevisionSelection = CloudScriptRevisionOption.Live,
                FunctionName = "claimall",
                FunctionParameter = new { kid = kongID }
            };
            var statusTask = await PlayFabClientAPI.ExecuteCloudScriptAsync(request);
            if (statusTask.Error != null)
            {
                logError(statusTask.Error.Error.ToString(), statusTask.Error.ErrorMessage);
                return false;
            }
            if (statusTask == null || statusTask.Result.FunctionResult == null || !statusTask.Result.FunctionResult.ToString().Contains("true"))
            {
                logError("Cloud Script Error", statusTask.Result.FunctionResult.ToString());
                return false;
            }
            else
            {
                JObject json = JObject.Parse(statusTask.Result.FunctionResult.ToString());
                miracleTimes = json["data"]["miracles"].ToString();
                followers = json["data"]["followers"].ToString();
                return true;
            }
        }

        public async Task<bool> sendPVPFight(int index)
        {
            battleResult = "";
            var request = new ExecuteCloudScriptRequest()
            {
                RevisionSelection = CloudScriptRevisionOption.Live,
                FunctionName = "fight",
                FunctionParameter = new { token = token, kid = kongID, id = nearbyPlayersIDs[index] }
            };
            var statusTask = await PlayFabClientAPI.ExecuteCloudScriptAsync(request);
            if (statusTask.Error != null)
            {
                battleResult = "";
                logError(statusTask.Error.Error.ToString(), statusTask.Error.ErrorMessage);
                return false;
            }
            if (statusTask == null || statusTask.Result.FunctionResult == null || !statusTask.Result.FunctionResult.ToString().Contains("true"))
            {
                battleResult = "";
                logError("Cloud Script Error", statusTask.ToString());
                return true;
            }
            else
            {
                JObject json = JObject.Parse(statusTask.Result.FunctionResult.ToString());
                PVPTime = json["data"]["city"]["nextfight"].ToString();
                switch (json["data"]["city"]["log"][0]["result"].ToString())
                {
                    case ("-1"):
                        battleResult = "Lose";
                        break;
                    case ("0"):
                        battleResult = "Draw";
                        break;
                    case ("1"):
                        battleResult = "Win";
                        break;
                }
                battleResult += " vs " + json["data"]["city"]["log"][0]["enemy"].ToString() + ", ELO " + int.Parse(json["data"]["city"]["log"][0]["rankd"].ToString()).ToString("+0;-#") +
                    ", Followers +" + json["data"]["city"]["log"][0]["earn"].ToString() + "\n";
                return true;
            }
        }
        public async Task<bool> sendOpen(string chestMode)
        {
            if (chestMode != "normal" && chestMode != "hero")
            {
                throw new ArgumentException("Wrong chest mode");
            }
            var request = new ExecuteCloudScriptRequest()
            {
                RevisionSelection = CloudScriptRevisionOption.Live,
                FunctionName = "open",
                FunctionParameter = new { kid = kongID, mode = chestMode }
            };
            var statusTask = await PlayFabClientAPI.ExecuteCloudScriptAsync(request);
            if (statusTask.Error != null)
            {
                logError(statusTask.Error.Error.ToString(), statusTask.Error.ErrorMessage);
                chestResult = 801;
                return false;
            }
            if (statusTask == null || statusTask.Result.FunctionResult == null || !statusTask.Result.FunctionResult.ToString().Contains("true"))
            {
                logError("Cloud Script Error", statusTask.Result.FunctionResult.ToString());
                JObject json = JObject.Parse(statusTask.Result.FunctionResult.ToString());
                chestResult = 800;
                return true;
            }
            else
            {
                JObject json = JObject.Parse(statusTask.Result.FunctionResult.ToString());
                chestResult = int.Parse(json["result"].ToString());
                freeChestAvailable = false;
                return true;
            }
        }

        public async Task<bool> sendDQSolution(int[] DQLineup)
        {
            var request = new ExecuteCloudScriptRequest()
            {
                RevisionSelection = CloudScriptRevisionOption.Live,
                FunctionName = "pved",
                FunctionParameter = new { setup = DQLineup, kid = kongID, max = true }
            };
            var statusTask = await PlayFabClientAPI.ExecuteCloudScriptAsync(request);
            if (statusTask.Error != null)
            {
                logError(statusTask.Error.Error.ToString(), statusTask.Error.ErrorMessage);
                return false;
            }
            if (statusTask == null || statusTask.Result.FunctionResult == null || !statusTask.Result.FunctionResult.ToString().Contains("true"))
            {
                logError("Cloud Script Error", statusTask.Result.FunctionResult.ToString());
                return false;
            }
            else
            {
                JObject json = JObject.Parse(statusTask.Result.FunctionResult.ToString());
                DQLevel = json["data"]["city"]["daily"]["lvl"].ToString();
                DQResult = true;
                return true;
            }
        }

        public async Task<bool> sendBid(int bidHeroID, int bidPrice)
        {
            var request = new ExecuteCloudScriptRequest()
            {
                RevisionSelection = CloudScriptRevisionOption.Live,
                FunctionName = "auction",
                FunctionParameter = new { hid = bidHeroID, kid = kongID, name = username, bid = bidPrice }
            };
            var statusTask = await PlayFabClientAPI.ExecuteCloudScriptAsync(request);
            if (statusTask.Error != null)
            {
                logError(statusTask.Error.Error.ToString(), statusTask.Error.ErrorMessage);
                using (StreamWriter sw = new StreamWriter("ActionLog.txt", true))
                {
                    sw.WriteLine(DateTime.Now);
                    sw.WriteLine("\tFAILED Bid on hero " + (Constants.heroNames.Length > bidHeroID + 2 ? Constants.heroNames[bidHeroID + 2] : ("Unknown, ID: " + bidHeroID))
                        + " for: " + bidPrice + "UM.");
                    sw.WriteLine(statusTask.Error.ErrorMessage);
                }
                return false;
            }
            if (statusTask == null || statusTask.Result.FunctionResult == null || !statusTask.Result.FunctionResult.ToString().Contains("true"))
            {
                logError("Cloud Script Error", statusTask.Result.FunctionResult.ToString());
                using (StreamWriter sw = new StreamWriter("ActionLog.txt", true))
                {
                    sw.WriteLine(DateTime.Now);
                    sw.WriteLine("\tFAILED Bid on hero " + (Constants.heroNames.Length > bidHeroID + 2 ? Constants.heroNames[bidHeroID + 2] : ("Unknown, ID: " + bidHeroID))
                        + " for: " + bidPrice + "UM.");
                    sw.WriteLine(statusTask.Result.FunctionResult.ToString());
                }
                return true;
            }
            else
            {
                using (StreamWriter sw = new StreamWriter("ActionLog.txt", true))
                {
                    sw.WriteLine(DateTime.Now);
                    sw.WriteLine("\tBid on hero " + (Constants.heroNames.Length > bidHeroID + 2 ? Constants.heroNames[bidHeroID + 2] : ("Unknown, ID: " + bidHeroID))
                        + " for: " + bidPrice + "UM.");
                }
                return true;
            }
        }

        public async Task<bool> sendWBFight(int[] WBLineup)
        {
            var request = new ExecuteCloudScriptRequest()
            {
                RevisionSelection = CloudScriptRevisionOption.Live,
                FunctionName = "fightWB",
                FunctionParameter = new { setup = WBLineup, kid = kongID }
            };
            var statusTask = await PlayFabClientAPI.ExecuteCloudScriptAsync(request);
            if (statusTask.Error != null)
            {
                logError(statusTask.Error.Error.ToString(), statusTask.Error.ErrorMessage);
                return false;
            }
            if (statusTask == null || statusTask.Result.FunctionResult == null || !statusTask.Result.FunctionResult.ToString().Contains("true"))
            {
                logError("Cloud Script Error", statusTask.Result.FunctionResult.ToString());
                return true;
            }
            else
            {
                return true;
            }
        }

        public async Task<bool> sendLevelUp(int heroID, string mode)
        {
            var request = new ExecuteCloudScriptRequest()
            {
                RevisionSelection = CloudScriptRevisionOption.Live,
                FunctionName = "levelUp",
                FunctionParameter = new { id = heroID, mode = mode }
            };
            var statusTask = await PlayFabClientAPI.ExecuteCloudScriptAsync(request);
            if (statusTask.Error != null)
            {
                logError(statusTask.Error.Error.ToString(), statusTask.Error.ErrorMessage);
                return false;
            }
            if (statusTask == null || statusTask.Result.FunctionResult == null || !statusTask.Result.FunctionResult.ToString().Contains("true"))
            {
                logError("Cloud Script Error", statusTask.Result.FunctionResult.ToString());
                return true;
            }
            else
            {
                using (StreamWriter sw = new StreamWriter("ActionLog.txt", true))
                {
                    sw.WriteLine(DateTime.Now);
                    sw.WriteLine("\tLeveled up hero: " + (Constants.heroNames.Length > heroID + 2 ? Constants.heroNames[heroID + 2] : ("Unknown, ID: " + heroID))
                        + " with: " + mode);
                }
                return true;
            }
        }


        public async Task<bool> sendLevelUp10(int heroID, string mode)
        {
            var request = new ExecuteCloudScriptRequest()
            {
                RevisionSelection = CloudScriptRevisionOption.Live,
                FunctionName = "levelUp10",
                FunctionParameter = new { id = heroID, mode = mode }
            };
            var statusTask = await PlayFabClientAPI.ExecuteCloudScriptAsync(request);
            if (statusTask.Error != null)
            {
                logError(statusTask.Error.Error.ToString(), statusTask.Error.ErrorMessage);
                return false;
            }
            if (statusTask == null || statusTask.Result.FunctionResult == null || !statusTask.Result.FunctionResult.ToString().Contains("true"))
            {
                logError("Cloud Script Error", statusTask.Result.FunctionResult.ToString());
                return true;
            }
            else
            {
                using (StreamWriter sw = new StreamWriter("ActionLog.txt", true))
                {
                    sw.WriteLine(DateTime.Now);
                    sw.WriteLine("\tLeveled up hero: " + (Constants.heroNames.Length > heroID + 2 ? Constants.heroNames[heroID + 2] : ("Unknown, ID: " + heroID))
                        + "10 times with: " + mode);
                }
                return true;
            }
        }

        public async Task<bool> sendLevelSuper(int heroID)
        {
            var request = new ExecuteCloudScriptRequest()
            {
                RevisionSelection = CloudScriptRevisionOption.Live,
                FunctionName = "levelSuper",
                FunctionParameter = new { id = heroID }
            };
            var statusTask = await PlayFabClientAPI.ExecuteCloudScriptAsync(request);
            if (statusTask.Error != null)
            {
                logError(statusTask.Error.Error.ToString(), statusTask.Error.ErrorMessage);
                return false;
            }
            if (statusTask == null || statusTask.Result.FunctionResult == null || !statusTask.Result.FunctionResult.ToString().Contains("true"))
            {
                logError("Cloud Script Error", statusTask.Result.FunctionResult.ToString());
                return true;
            }
            else
            {
                using (StreamWriter sw = new StreamWriter("ActionLog.txt", true))
                {
                    sw.WriteLine(DateTime.Now);
                    sw.WriteLine("\tLeveled up hero: " + (Constants.heroNames.Length > heroID + 2 ? Constants.heroNames[heroID + 2] : ("Unknown, ID: " + heroID)));
                }
                return true;
            }
        }

        public async Task<bool> sendAscendHero(int heroID)
        {
            var request = new ExecuteCloudScriptRequest()
            {
                RevisionSelection = CloudScriptRevisionOption.Live,
                FunctionName = "ascendHero",
                FunctionParameter = new { id = heroID }
            };
            var statusTask = await PlayFabClientAPI.ExecuteCloudScriptAsync(request);
            if (statusTask.Error != null)
            {
                logError(statusTask.Error.Error.ToString(), statusTask.Error.ErrorMessage);
                return false;
            }
            if (statusTask == null || statusTask.Result.FunctionResult == null || !statusTask.Result.FunctionResult.ToString().Contains("true"))
            {
                logError("Cloud Script Error", statusTask.Result.FunctionResult.ToString());
                return true;
            }
            else
            {
                using (StreamWriter sw = new StreamWriter("ActionLog.txt", true))
                {
                    sw.WriteLine(DateTime.Now);
                    sw.WriteLine("\tAscended hero: " + (Constants.heroNames.Length > heroID + 2 ? Constants.heroNames[heroID + 2] : ("Unknown, ID: " + heroID)));
                }
                return true;
            }
        }

        public async Task<bool> sendConvert(bool mult10)
        {
            var request = new ExecuteCloudScriptRequest()
            {
                RevisionSelection = CloudScriptRevisionOption.Live,
                FunctionName = "toPG",
                FunctionParameter = new { multiple = mult10 }
            };
            var statusTask = await PlayFabClientAPI.ExecuteCloudScriptAsync(request);
            if (statusTask.Error != null)
            {
                logError(statusTask.Error.Error.ToString(), statusTask.Error.ErrorMessage);
                return false;
            }
            if (statusTask == null || statusTask.Result.FunctionResult == null || !statusTask.Result.FunctionResult.ToString().Contains("true"))
            {
                logError("Cloud Script Error", statusTask.Result.FunctionResult.ToString());
                return true;
            }
            else
            {
                using (StreamWriter sw = new StreamWriter("ActionLog.txt", true))
                {
                    sw.WriteLine(DateTime.Now);
                    sw.WriteLine("\tConverted " + (mult10 ? 1 : 10) + " AS to Prana");
                }
                pranaGems += mult10 ? 1 : 10;
                return true;
            }
        }


        #endregion
    }
}
