using System.Net.Http;
using System.Text.Json;
using RobloxFiles;
using System.ComponentModel;
using System;
using System.IO;
using System.Text;
using System.Net;

//robloxTest.Scraper.parseFile();
await robloxTest.Scraper.Main();

namespace robloxTest
{
    public struct MarkplaceData
    {
        public long id { get; set; }
        public string name { get; set; }
        public string searchResultSource { get; set; }
    }
    public struct MarketplacePage
    {
        public long totalResults { get; set; }
        public string? nextPageCursor { get; set; }
        public List<MarkplaceData> data { get; set; }
    };
    public struct MarketplaceDetailsData
    {
        public long id { get; set; }
        public bool hasScripts { get; set; }
    }
    public struct MarketplaceAsset
    {
        public MarketplaceDetailsData asset { get; set; }
    }
    public struct MarketplaceDetails
    {
        public List<MarketplaceAsset> data { get; set; }
    }
    public struct DownloadLinkData
    {
        public string location { get; set; }
    }
    public struct DownloadLink
    {
        public List<DownloadLinkData> locations { get; set; }
    }
    public class Scraper
    {
        public static async Task Main()
        {
            var clientHandler = new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
            HttpClient client = new HttpClient(clientHandler);

            try
            {
                string responseBody = await client.GetStringAsync("https://apis.roblox.com/toolbox-service/v1/marketplace/10?limit=100");
                //await using Stream stream = await client.GetStreamAsync("https://apis.roblox.com/toolbox-service/v1/marketplace/10");
                //MarketplacePage page = await JsonSerializer.DeserializeAsync<MarketplacePage>(stream);

                MarketplacePage page = JsonSerializer.Deserialize<MarketplacePage>(responseBody);
                List<long> ids = new List<long>();
                foreach (MarkplaceData asset in page.data)
                {
                    ids.Add(asset.id);
                }
                string paramList = String.Join(",", ids);
                responseBody = await client.GetStringAsync("https://apis.roblox.com/toolbox-service/v1/items/details?assetIds=" + paramList);
                MarketplaceDetails details = JsonSerializer.Deserialize<MarketplaceDetails>(responseBody);
                foreach (MarketplaceAsset asset in details.data)
                {
                    if (asset.asset.hasScripts)
                    {
                        string downloadLink = await client.GetStringAsync("https://assetdelivery.roblox.com/v2/assetId/" + asset.asset.id.ToString());
                        downloadLink = JsonSerializer.Deserialize<DownloadLink>(downloadLink).locations[0].location;
                        //Stream fileStream = await client.GetStreamAsync(downloadLink);

                        //var bytes = await client.GetByteArrayAsync(downloadLink);
                        //string response = await client.GetStringAsync(downloadLink);

                        byte[] bytes = await client.GetByteArrayAsync("https://assetdelivery.roblox.com/v1/asset/?id="+ asset.asset.id.ToString());
                        //string plaintext = await client.GetStringAsync("https://assetdelivery.roblox.com/v1/asset/?id=" + asset.asset.id.ToString());
                        //Console.WriteLine(plaintext);
                        //File.WriteAllBytes("temp.rbxm", bytes);
                        Scraper.parseFile(bytes);
                    }
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }
        }
        public static void parseFile(byte[] bytes)
        {
            try
            {
                RobloxFile file = RobloxFile.Open(bytes);
                //RobloxFile file = RobloxFile.Open(fileStream);
                //RobloxFile file = RobloxFile.Open("temp.rbxm");
                foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(file))
                {
                    string name = descriptor.Name;
                    object value = descriptor.GetValue(file);
                    Console.WriteLine("{0}={1}", name, value);
                    return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}

