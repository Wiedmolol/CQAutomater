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

namespace CQFollowerAutoclaimer
{
    class PFStuff
    {
        string token;
        string kongID;
        static bool _running = true;
        static public bool logres;
        static public string miracleTimes;
        static public string initialFollowers;
        static public string DQTime;
        static public string DQLevel;
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

        static public string chestMode = "normal";
        public PFStuff(string t, string kid)
        {
            token = t;
            kongID = kid;
        }

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
                        heroChests = int.Parse(apiResult.VirtualCurrency["KU"].ToString())/10;
                        freeChestAvailable = apiResult.VirtualCurrency["BK"].ToString() == "1" ? true : false;
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
                        battleResult += " vs " + json["data"]["city"]["log"][0]["enemy"].ToString() + ", ELO " + json["data"]["city"]["log"][0]["rankd"].ToString() +
                            ", Followers +" + json["data"]["city"]["log"][0]["earn"].ToString() + "\n";
                        return;
                    }
                    _running = false;
                }
                Thread.Sleep(1);
            }
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
                        chestResult = 0;
                        return;
                    }
                    else if (apiResult.FunctionResult != null && apiResult.FunctionResult.ToString().Contains("true"))
                    {               
                        JObject json = JObject.Parse(apiResult.FunctionResult.ToString());
                        chestResult = int.Parse(json["result"].ToString());
                        return;
                    }
                    _running = false;
                }
                Thread.Sleep(1);
            }
            chestResult = 0;
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
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(@"https://cosmosquest.net/event.php?kid=" + id);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            string content = new StreamReader(response.GetResponseStream()).ReadToEnd();

            JObject json = JObject.Parse(content);
            var WBData = json["WB"];
        }

    }
}
