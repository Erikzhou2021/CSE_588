using System.Net.Http;
using System.Text.Json;
using RobloxFiles;
using System.ComponentModel;
using System.Text;

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
    public struct DownloadLinkError
    {
        public string code { get; set; }
        public string message { get; set; }
    }
    public struct DownloadLink
    {
        public List<DownloadLinkData>? locations { get; set; }
        public List<DownloadLinkError>? errors { get; set; }
    }
    public class Scraper
    {
        public static async Task Main()
        {
            HttpClient client = new HttpClient();
            int p = 0;
            int s = 0;
            int total = 0;

            try
            {
                while (p < 10) {
                    string responseBody = await client.GetStringAsync($"https://apis.roblox.com/toolbox-service/v1/marketplace/10?limit=100&pageNumber={p}");
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
                            total++;
                            Console.WriteLine("{0}", asset.asset.id);
                            string downloadLink = await client.GetStringAsync("https://assetdelivery.roblox.com/v2/assetId/" + asset.asset.id.ToString());


                            var response = JsonSerializer.Deserialize<DownloadLink>(downloadLink);
                            // need to error check in case we get paywalled
                            if (response.errors != null)
                            {
                                foreach (var error in response.errors)
                                {
                                    Console.WriteLine($"Error for asset {asset.asset.id}: {error.message}");
                                }
                                continue;
                            }
                            
                            // Stream fileStream = await client.GetStreamAsync(downloadLink);
                            string filestring = await client.GetStringAsync(response.locations[0].location);
                            byte[] byteArray = await client.GetByteArrayAsync(response.locations[0].location);
                            // Console.WriteLine(filestring);
                            //byte[] byteArray = Encoding.UTF7.GetBytes(filestring);
                            // Console.WriteLine();
                            // Console.WriteLine(byteArray);
                            //Console.WriteLine(await client.GetStringAsync(downloadLink));
                            try
                            {
                                RobloxFile file = RobloxFile.Open(byteArray);
                                Console.WriteLine("Stream Success!");
                                if (file is BinaryRobloxFile) {
                                    Console.WriteLine("binary format");
                                }
                                else 
                                    Console.WriteLine("xml format");
                                s++;
                                // foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(file))
                                // {
                                //     string name = descriptor.Name;
                                //     object value = descriptor.GetValue(file);
                                //     //Console.WriteLine("{0}={1}", name, value);
                                //     return;
                                // }
                            }
                            catch (Exception e){
                                Console.WriteLine($"stream failed: {e.Message}");
                            }

                            try {
                                string tempFilePath = Path.Combine(Path.GetTempPath(), "tempFile.rbxm");
                                await File.WriteAllBytesAsync(tempFilePath, byteArray);
                                RobloxFile file = RobloxFile.Open(tempFilePath);
                                Console.WriteLine("File Success!");
                            }
                            catch (Exception e){
                                Console.WriteLine($"file format failed: {e.Message}");
                                continue;
                            }

                            
                        }
                        else {
                            //Console.WriteLine("No scripts");
                        }
                    }
                    p++;
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }

            Console.WriteLine($"Successes: {s}, Total: {total}");
        }
    }
}

