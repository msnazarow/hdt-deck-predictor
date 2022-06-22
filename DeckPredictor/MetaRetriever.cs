using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HearthDb.Deckstrings;
using Hearthstone_Deck_Tracker;
using Hearthstone_Deck_Tracker.Hearthstone;
using HtmlAgilityPack;
using Deck = Hearthstone_Deck_Tracker.Hearthstone.Deck;

namespace DeckPredictor
{
	class MetaRetriever
	{
		// How many days we wait before updating the meta since the last download.
		private static readonly double RecentDownloadTimeoutDays = 1;
		private static readonly string MetaVersionUrl = "http://metastats.net/metadetector/metaversion.php";
		private static readonly string MetaFilePath = Path.Combine(DeckPredictorPlugin.DataDirectory, @"metaDecks.xml");
		private static readonly string[] ClassList = { "DemonHunter", "Druid", "Hunter", "Mage", "Paladin", "Priest", "Rogue", "Shaman", "Warlock", "Warrior" };

		public async Task<List<Deck>> RetrieveMetaDecks(PluginConfig config)
		{
			string newMetaVersion = "";
			bool needDownload = false;
			using (WebClient client = new WebClient())
			{
				newMetaVersion = await client.DownloadStringTaskAsync(MetaVersionUrl);
			}
			// Download new deck list if we find a new meta.
			if (newMetaVersion != config.CurrentMetaFileVersion)
			{
				Log.Info("New version detected: " + newMetaVersion +
						", old version: " + config.CurrentMetaFileVersion);
				needDownload = true;
			} else
			// Download new deck list if last download is one day old.
            {
				// First check if we need to download the meta file.
				double daysSinceLastDownload = (DateTime.Now - config.CurrentMetaFileDownloadTime).TotalDays;
				if (daysSinceLastDownload > RecentDownloadTimeoutDays)
				{
					Log.Info(daysSinceLastDownload +
							" days since meta file has been updated, checking for new version.");
					needDownload = true;
				}
            }

			List<Deck> metaDecks = new List<Deck>();
			if (needDownload)
            {
				Log.Info("Downloading new meta file.");
                HtmlWeb web = new HtmlWeb();

				Regex rx = new Regex(@".*?#\d+\s+(.*)", RegexOptions.Compiled);
                foreach (string clas in ClassList)
                {
					Log.Info("Fetch data for class " + clas);
                    HtmlDocument htmlDoc = web.Load(string.Format("http://metastats.net/hearthstone/class/decks/{0}/", clas));
					HtmlNodeCollection nodes = htmlDoc.DocumentNode.SelectNodes("//div[@class='decklist']//button[contains(@class,'copytoclipboard')]");
                    if (nodes == null)
                    {
						continue;
                    }
					foreach (HtmlNode node in nodes)
                    {
						string text = node.GetAttributeValue("data-clipboard-text", "");

						if (!string.IsNullOrEmpty(text))
                        {
							Match match = rx.Match(text);
							if (match.Success)
                            {
								string deckStr = match.Groups[1].Value;
								Log.Info("Import deck string " + deckStr);
								HearthDb.Deckstrings.Deck hearthDbDeck = DeckSerializer.Deserialize(deckStr);
								metaDecks.Add(HearthDbConverter.FromHearthDbDeck(hearthDbDeck));
                            }
                        }
                    }

                }

                config.CurrentMetaFileVersion = newMetaVersion;
				config.CurrentMetaFileDownloadTime = DateTime.Now;
				config.Save();
				XmlManager<List<Deck>>.Save(MetaFilePath, metaDecks);
			} else
            {
				Log.Info("Load existing meta file.");
				metaDecks = XmlManager<List<Deck>>.Load(MetaFilePath);
            }

			Log.Info("Meta retrieved, " + metaDecks.Count + " decks loaded.");
			return metaDecks;
		}
    }
}
