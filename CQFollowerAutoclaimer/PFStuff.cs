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
        static bool _running=true;
        static public bool logres;
        static public string miracleTimes;
        static public string initialFollowers;
        static public string DQTime;
        static public string DQLevel;

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
                        DQTime = json["data"]["city"]["daily"]["timer"].ToString();
                        DQLevel = json["data"]["city"]["daily"]["lvl"].ToString();
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

     
    }
}
