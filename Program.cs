using System.Net.Http;
using System.Text.Json;
using RobloxFiles;
using RobloxFiles.DataTypes;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime;
using System.Net;
using robloxTest;

Scraper scraper = new Scraper();
// await scraper.checkAsset(3044376909);
await scraper.Main();

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
        HttpClient client;
        int p = 0;
        int s = 0;
        public Scraper(){
            var clientHandler = new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
            client = new HttpClient(clientHandler);
        }
        public async Task Main()
        {
            int total = 0;
            // string [] keywords = {"patrick", "easter", "christmas", "halloween", "fortnite", "tuah"};
            string [] keywords = {"chaotic"};
            foreach (string keyword in keywords) {
                Console.WriteLine(keyword.ToUpper());
                try
            {
                while (p < 10) {
                    string responseBody = await client.GetStringAsync($"https://apis.roblox.com/toolbox-service/v1/marketplace/10?limit=100&pageNumber={p}&keyword={keyword}");

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
                            await checkAsset(asset.asset.id);
                        }
                    }
                    p++;
                }
            }
            catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.TooManyRequests)
            {
                Console.WriteLine($"429 Too Many Requests - Waiting {30} seconds...");
                await Task.Delay(30_000);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
                
            }
            }
            

            Console.WriteLine($"Successes: {s}, Total: {total}");
        }

        public async Task checkAsset(long assetId){
            string downloadLink = await client.GetStringAsync("https://assetdelivery.roblox.com/v2/assetId/" + assetId.ToString());

            var response = JsonSerializer.Deserialize<DownloadLink>(downloadLink);
            // need to error check in case we get paywalled
            if (response.errors != null)
            {
                foreach (var error in response.errors)
                {
                    Console.WriteLine($"Error for asset {assetId}: {error.message}");
                }
                return;
            }
            
            // Stream fileStream = await client.GetStreamAsync(downloadLink);
            byte[] byteArray = await client.GetByteArrayAsync(response.locations[0].location);
            try
            {
                RobloxFile file = RobloxFile.Open(byteArray);
                s++;
                bool susted = false;

            foreach (var obj in file.GetDescendants()) {
                if (obj.ClassName == "Script" || obj.ClassName == "ModuleScript") {
                    Property source = obj.GetProperty("Source");
                    ProtectedString sourceValue = source.Value as ProtectedString;
                    string sourceString = sourceValue.ToString();

                    string[] lines = sourceString.Split('\n');
                    List<(int, string)> reqLines = new List<(int, string)>();
                    
                    // Could multi-thread this but we're already getting rate limited so..
                    // requires check
                    if (sourceString.Contains("require")) {

                        // weld script check
                        if (obj.Name.ToLower().Contains("weld")) {
                            susted = true;
                            Console.WriteLine($"Model {assetId} with script {obj.Name}:");
                            Console.WriteLine($"has requires in weld script");
                        }

                        for (int i = 0; i < lines.Length; i++) {
                            if (lines[i].Contains("require")) {
                                reqLines.Add((i + 1, lines[i])); 
                            }
                        }

                        foreach (var x in reqLines) {
                            if (x.Item2.Contains("MaterialService") || x.Item2.Contains("JointsService") || x.Item2.Contains("nil") 
                                || x.Item2.Contains("+") || x.Item2.Contains("tonumber")) {
                                susted = true;
                                Console.WriteLine($"Model {assetId} with script {obj.Name}:");
                                Console.WriteLine($"on line {x.Item1} Contains very sus 'require': {x.Item2}");
                            }
                        }
                    }

                    // roblox faker check
                    if (sourceString.Contains("OFFICIAL ROBLOX")) {
                        susted = true;
                        Console.WriteLine($"Model {assetId} with script {obj.Name}:");
                        Console.WriteLine($"has OFFICIAL ROBLOX in it");
                    }

                    // circumvention check
                    if (sourceString.Contains("game[\"Run Service\"]:IsStudio()") || sourceString.Contains(":IsStudio()")) {
                        susted = true;
                        Console.WriteLine($"Model {assetId} with script {obj.Name}:");
                        Console.WriteLine($"Checks for IsStudio");
                    }

                    // lines check
                    if (lines.Length > 8000) {
                        susted = true;
                        Console.WriteLine($"Model {assetId} with script {obj.Name}:");
                        Console.WriteLine("Has a suspicious number of lines");
                    }



                    for (int i = 0; i < lines.Length; i++) {
                        if (lines[i].Length > 500) {
                            susted = true;
                            Console.WriteLine($"Model {assetId} with script {obj.Name}:");
                            Console.WriteLine($"Has a suspiciously long line on line {i + 1}: {lines[i]}");
                            break;
                        } 
                    }

                    if(Scraper.checkHardCodedPlayerName(sourceString)){
                        susted = true;
                        Console.WriteLine($"Model {assetId} with script {obj.Name}:");
                        Console.WriteLine($"Has hard coded player name search");
                    }
                }
            }
            if (susted) {
                Console.WriteLine("\n");
            }

            }
            catch (Exception e){
                Console.WriteLine($"stream failed: {e.Message}");
            }
        }

        public static bool checkHardCodedPlayerName(string sourceCode){
            if(sourceCode.Contains("game:GetService(\"Players\"):FindFirstChild(") || 
                sourceCode.Contains("game:GetService('Players'):FindFirstChild(")){
                return true;
            }

            // checks if they assign a variable that they use later
            string assignmentPattern = @"\b(\w+)\s*=\s*game:GetService\([""']Players[""']\)";
            var variableMatches = Regex.Matches(sourceCode, assignmentPattern);
            if(variableMatches.Count == 0){
                return false;
            }
            HashSet<string> playerVariables = new HashSet<string>();
            foreach (Match match in variableMatches)
            {
                playerVariables.Add(match.Groups[1].Value);
            }

            // checks when they later use the variable
            // we might have false positives if they reassign the variable before using it
            string findPlayerPattern = $@"\b({string.Join("|", playerVariables)})\:FindFirstChild\(";
            var findPlayerMatches = Regex.Matches(sourceCode, findPlayerPattern);
            if(findPlayerMatches.Count > 0){
                return true;
            }

            // string findUserIdPattern = $@"\b({string.Join("|", playerVariables)})\:GetUserIdFromNameAsync\(";
            return false;
        }
    }
}

