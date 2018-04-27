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
        static bool _running = true;
        static public bool logres;
        static public string miracleTimes;
        static public string initialFollowers;
        static public string DQTime;
        static public string DQLevel;
        static public bool DQResult;
        static public int[] DQlineup;
        static public string PVPTime;

        static public string[] nearbyPlayersIDs = new string[10];
        static public int PVPEnemyIndex;
        static public string username;
        static public int userIndex;
        static public int LeaderboardRange = 10;
        static public int freeChestRecharge;
        
        static public string battleResult;
        static public int chestResult;

        static public int normalChests;
        static public int heroChests;

        static public bool freeChestAvailable = false;

        static public int wbDamageDealt;
        static public int wbMode;
        static public int wbAttacksAvailable;
        static public int WB_ID;
        static public string WBName;
        static public int attacksLeft;

        static public string chestMode = "normal";
        static public int[] WBlineup;
        static public bool WBchanged = false;
        static public JArray auctionData;
        static public int bidHeroID;
        static public int bidPrice;
        public PFStuff(string t, string kid)
        {
            token = t;
            kongID = kid;
        }

        #region Getting Data
        public void LoginKong()
        {
            PlayFabSettings.TitleId = "E3FA";
            var request = new LoginWithKongregateRequest
            {
                AuthTicket = token,
                CreateAccount = false,
                KongregateId = kongID,
            };
            var loginTask = PlayFabClientAPI.LoginWithKongregateAsync(request);
            while (_running)
            {
                if (loginTask.IsCompleted)
                {
                    var apiError = loginTask.Result.Error;
                    var apiResult = loginTask.Result.Result;

                    if (apiError != null)
                    {
                        logres = false;
                        MessageBox.Show("Failed to log in. Error: " + apiError.ErrorMessage);
                        return;
                    }
                    else if (apiResult != null)
                    {
                        logres = true;
                        return;
                    }
                    _running = true;
                }
                Thread.Sleep(1);
            }
            logres = false;
            return;
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
                        miracleTimes = json["data"]["miracles"].ToString();
                        initialFollowers = json["data"]["followers"].ToString();
                        DQTime = json["data"]["city"]["daily"]["timer2"].ToString();
                        DQLevel = json["data"]["city"]["daily"]["lvl"].ToString();
                        PVPTime = json["data"]["city"]["nextfight"].ToString();
                        wbAttacksAvailable = int.Parse(json["data"]["city"]["WB"]["atks"].ToString());
                        return;
                    }
                    _running = false;
                }
                Thread.Sleep(1);
            }
            return;
        }

        public void getLeaderboard()
        {
            nearbyPlayersIDs = new string[LeaderboardRange];
            var request = new GetLeaderboardAroundPlayerRequest
            {
                StatisticName = "Ranking",
                MaxResultsCount = LeaderboardRange
            };
            var leaderboardTask = PlayFabClientAPI.GetLeaderboardAroundPlayerAsync(request);
            bool _running = true;
            while (_running)
            {
                if (leaderboardTask.IsCompleted)
                {
                    var apiError = leaderboardTask.Result.Error;
                    var apiResult = leaderboardTask.Result.Result;

                    if (apiError != null)
                    {
                        return;
                    }
                    else if (apiResult != null)
                    {
                        for (int i = 0; i < LeaderboardRange; i++)
                        {
                            nearbyPlayersIDs[i] = apiResult.Leaderboard[i].PlayFabId;
                            if (apiResult.Leaderboard[i].DisplayName == username)
                            {
                                userIndex = i;
                            }
                        }
                        return;
                    }
                    _running = false;
                }
                Thread.Sleep(1);
            }
            return;
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
            WB_ID = int.Parse(a);
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
        public void sendClaimAll()
        {
            var request = new ExecuteCloudScriptRequest()
            {
                RevisionSelection = CloudScriptRevisionOption.Live,
                FunctionName = "claimall",
                FunctionParameter = new { kid = kongID }
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
                        JObject json = JObject.Parse(apiResult.FunctionResult.ToString());
                        miracleTimes = json["data"]["miracles"].ToString();
                        initialFollowers = json["data"]["followers"].ToString();
                        return;
                    }
                    _running = false;
                }
                Thread.Sleep(1);
            }
            return;
        }
        public void sendPVPFight()
        {
            var request = new ExecuteCloudScriptRequest()
            {
                RevisionSelection = CloudScriptRevisionOption.Live,
                FunctionName = "fight",
                FunctionParameter = new { token = token, kid = kongID, id = nearbyPlayersIDs[PVPEnemyIndex] }                
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
                        battleResult = "";
                        return;
                    }
                    else if (apiResult.FunctionResult != null && apiResult.FunctionResult.ToString().Contains("true"))
                    {
                        JObject json = JObject.Parse(apiResult.FunctionResult.ToString());
                        PVPTime = json["data"]["city"]["nextfight"].ToString();
                        switch (json["data"]["city"]["log"][0]["result"].ToString())
                        {
                            case("-1"):
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
                        return;
                    }
                    _running = false;
                }
                Thread.Sleep(1);
            }
            battleResult = "";
            return;
        }

        public void sendOpen()
        {
            var request = new ExecuteCloudScriptRequest()
            {
                RevisionSelection = CloudScriptRevisionOption.Live,
                FunctionName = "open",                
                FunctionParameter = new { kid = kongID, mode = chestMode }
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
                        chestResult = -1;
                        return;
                    }
                    else if (apiResult.FunctionResult != null && apiResult.FunctionResult.ToString().Contains("true"))
                    {               
                        JObject json = JObject.Parse(apiResult.FunctionResult.ToString());
                        chestResult = int.Parse(json["result"].ToString());
                        freeChestAvailable = false;
                        return;
                    }
                    _running = false;
                }
                Thread.Sleep(1);
            }
            chestResult = -1;
            return;
        }

        public void sendDQSolution()
        {
            var request = new ExecuteCloudScriptRequest()
            {
                RevisionSelection = CloudScriptRevisionOption.Live,
                FunctionName = "pved",
                FunctionParameter = new { setup = DQlineup, kid = kongID, max = true }
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
                        DQResult = false;
                        return;
                    }
                    else if (apiResult.FunctionResult.ToString().Contains("true"))
                    {
                        JObject json = JObject.Parse(apiResult.FunctionResult.ToString());
                        DQLevel = json["data"]["city"]["daily"]["lvl"].ToString();
                        DQResult = true;
                        return;
                    }
                    _running = false;
                }
                Thread.Sleep(1);
            }
            DQResult = false;
            return;
        }

        public void sendBid()
        {
            var request = new ExecuteCloudScriptRequest()
            {
                RevisionSelection = CloudScriptRevisionOption.Live,
                FunctionName = "auction",
                FunctionParameter = new { hid = bidHeroID, kid = kongID, name = username, bid = bidPrice }
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


        public void sendWBFight()
        {
            var request = new ExecuteCloudScriptRequest()
            {
                RevisionSelection = CloudScriptRevisionOption.Live,
                FunctionName = "fightWB",
                FunctionParameter = new { setup = WBlineup, kid = kongID }
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
                    else if (apiResult.FunctionResult != null && apiResult.FunctionResult.ToString().Contains("true"))
                    {
                        JObject json = JObject.Parse(apiResult.FunctionResult.ToString());
                        return;
                    }
                    _running = false;
                }
                Thread.Sleep(1);
            }
            return;
        }

        #endregion
    }
}
