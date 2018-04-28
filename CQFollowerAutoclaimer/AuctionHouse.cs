using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CQFollowerAutoclaimer
{
    struct Auction
    {
        public int heroID;
        public string bidderName;
        public DateTime endTime;
        public int price;
        public Auction(int hid, string bd, DateTime e, int p)
        {
            heroID = hid;
            bidderName = bd;
            endTime = e;
            price = p;
        }
    }
    class AuctionHouse
    {
        System.Timers.Timer AHTimer = new System.Timers.Timer();
        Form1 main;
        List<Auction> auctionList = new List<Auction>();

        public AuctionHouse(Form1 m) { main = m; }

        public void loadAuctions(bool getData)
        {
            if (getData)
            {
                PFStuff.getWebsiteData(Form1.appSettings.KongregateId);
            }
            auctionList = new List<Auction>();
            foreach (JObject jo in PFStuff.auctionData)
            {
                int id = Int32.Parse(jo["hero"].ToString());
                string bidName = jo["bidname"].ToString();
                int price = Int32.Parse(jo["bid"].ToString());
                DateTime dt = DateTime.Now.AddMilliseconds(double.Parse(jo["timer"].ToString()));
                auctionList.Add(new Auction(id, bidName, dt, price));
            }
        }

        public string[] getAvailableHeroes()
        {
            List<String> n = new List<string>();
            foreach (Auction a in auctionList)
            {
                if (a.heroID + 2 > Constants.heroNames.Length)
                {
                    n.Add("ID: " + (a.heroID + 2).ToString());
                }
                else
                {
                    n.Add(Constants.heroNames[a.heroID + 2]);
                }
            }
            return n.ToArray();
        }

        public double getAuctionInterval()
        {
            List<double> times = new List<double>();
            if (main.auctionHero1Box.getCheckState() && main.auctionHero1Combo.getText() != "")
            {
                int index = Array.IndexOf(Constants.heroNames, main.auctionHero1Combo.getText());
                Auction a = auctionList.Find(x => x.heroID == index - 2);
                times.Add((a.endTime - DateTime.Now).TotalMilliseconds - 15000);
                main.auctionHero1CostLabel.setText(a.price.ToString());
                main.auctionHero1BidderLabel.setText(a.bidderName);
            }
            if (main.auctionHero2Box.getCheckState() && main.auctionHero2Combo.getText() != "")
            {
                int index = Array.IndexOf(Constants.heroNames, main.auctionHero2Combo.getText());
                Auction a = auctionList.Find(x => x.heroID == index - 2);
                times.Add((a.endTime - DateTime.Now).TotalMilliseconds - 15000);
                main.auctionHero2CostLabel.setText(a.price.ToString());
                main.auctionHero2BidderLabel.setText(a.bidderName);
            }
            if (main.auctionHero3Box.getCheckState() && main.auctionHero3Combo.getText() != "")
            {
                int index = Array.IndexOf(Constants.heroNames, main.auctionHero3Combo.getText());
                Auction a = auctionList.Find(x => x.heroID == index - 2);
                times.Add((a.endTime - DateTime.Now).TotalMilliseconds - 15000);
                main.auctionHero3CostLabel.setText(a.price.ToString());
                main.auctionHero3BidderLabel.setText(a.bidderName);
            }
            //if (times.Count > 0)
            //    return Math.Max(4000, Math.Min(times.Min(), 5 * 60 * 1000));
            return 5 * 60 * 1000;
        }
        public void loadSettings()
        {
            AppSettings ap = AppSettings.loadSettings();
            if (ap.bids != null)
            {
                main.auctionHero1Combo.Text = ap.bids[0].name ?? "";
                main.auctionHero1Box.Checked = ap.bids[0].biddingEnabled ?? false;
                main.auctionHero1PriceCount.Value = ap.bids[0].maxBid ?? 0;
                main.auctionHero1LevelCount.Value = ap.bids[0].maxLevel ?? 0;
                main.auctionHero2Combo.Text = ap.bids[1].name ?? "";
                main.auctionHero2Box.Checked = ap.bids[1].biddingEnabled ?? false;
                main.auctionHero2PriceCount.Value = ap.bids[1].maxBid ?? 0;
                main.auctionHero2LevelCount.Value = ap.bids[1].maxLevel ?? 0;
                main.auctionHero3Combo.Text = ap.bids[2].name ?? "";
                main.auctionHero3Box.Checked = ap.bids[2].biddingEnabled ?? false;
                main.auctionHero3PriceCount.Value = ap.bids[2].maxBid ?? 0;
                main.auctionHero3LevelCount.Value = ap.bids[2].maxLevel ?? 0;
            }
        }

        public void saveSettings()
        {
            AppSettings ap = AppSettings.loadSettings();
            AuctionBids a1 = new AuctionBids
            {
                name = main.auctionHero1Combo.Text,
                biddingEnabled = main.auctionHero1Box.Checked,
                maxBid = (int)main.auctionHero1PriceCount.Value,
                maxLevel = (int)main.auctionHero1LevelCount.Value
            };
            AuctionBids a2 = new AuctionBids
            {
                name = main.auctionHero2Combo.Text,
                biddingEnabled = main.auctionHero2Box.Checked,
                maxBid = (int)main.auctionHero2PriceCount.Value,
                maxLevel = (int)main.auctionHero2LevelCount.Value
            };
            AuctionBids a3 = new AuctionBids
            {
                name = main.auctionHero3Combo.Text,
                biddingEnabled = main.auctionHero3Box.Checked,
                maxBid = (int)main.auctionHero3PriceCount.Value,
                maxLevel = (int)main.auctionHero3LevelCount.Value
            };
            ap.bids = new List<AuctionBids> { a1, a2, a3 };
            ap.saveSettings();
        }
    }
}
