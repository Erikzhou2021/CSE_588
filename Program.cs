using System.Net.Http;
using System.Text.Json;
using RobloxFiles;
using RobloxFiles.DataTypes;
using System.ComponentModel;
using System;
using System.IO;
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
        public int code { get; set; }
        public string message { get; set; }
    }
    public struct DownloadLink
    {
        public string? location { get; set; }
        public List<DownloadLinkError>? errors { get; set; }
    }
    public class Scraper
    {
        HttpClient client;
        int s = 0;
        Dictionary<string, int> malwareTypeCount = new Dictionary<string, int>();
        public Scraper(){
            var clientHandler = new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
            client = new HttpClient(clientHandler);
            malwareTypeCount.Add("require", 0);
            malwareTypeCount.Add("fakeOfficial", 0);
            malwareTypeCount.Add("isStudio", 0);
            malwareTypeCount.Add("longFile", 0);
            malwareTypeCount.Add("longLine", 0);
            malwareTypeCount.Add("hardCodedName", 0);
        }
        public async Task Main()
        {
            int total = 0;
            string[] keywords = {
                "patrick", "easter", "christmas", "halloween", "fortnite", "tuah",
                "sword", "gun", "car", "tank", "zombie", "pet", "obby", "simulator", "tycoon",
                "admin", "morph", "house", "castle", "city", "forest", "lava", "portal", 
                "police", "fire", "dragon", "spaceship", "boat", "plane", "helicopter", 
                "explosion", "magic", "dance", "animation", "script", "kit", "ui", "gui", 
                "button", "teleport", "tool", "swordfight", "camera", "ragdoll", "vehicle", 
                "trap", "badge", "checkpoint", "money", "gamepass", "boss", "npc", "alien", 
                "ghost", "haunted", "prison"
            };
            foreach (string keyword in keywords) {
                Console.WriteLine(keyword.ToUpper());
                int p = 0;
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
            foreach (KeyValuePair<string, int> kvp in malwareTypeCount)
            {
                Console.WriteLine("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
            }
        }

        public async Task checkAsset(long assetId){

            DownloadLink response;
            try {
                string api_key = "0EGOJlVXckqHCdPk2N1yiD835QGOArfgjtMd8oXFW7m0P0bqZXlKaGJHY2lPaUpTVXpJMU5pSXNJbXRwWkNJNkluTnBaeTB5TURJeExUQTNMVEV6VkRFNE9qVXhPalE1V2lJc0luUjVjQ0k2SWtwWFZDSjkuZXlKaVlYTmxRWEJwUzJWNUlqb2lNRVZIVDBwc1ZsaGphM0ZJUTJSUWF6Sk9NWGxwUkRnek5WRkhUMEZ5Wm1kcWRFMWtPRzlZUmxjM2JUQlFNR0p4SWl3aWIzZHVaWEpKWkNJNklqUTVNakV3T0RnME9EVWlMQ0poZFdRaU9pSlNiMkpzYjNoSmJuUmxjbTVoYkNJc0ltbHpjeUk2SWtOc2IzVmtRWFYwYUdWdWRHbGpZWFJwYjI1VFpYSjJhV05sSWl3aVpYaHdJam94TnpRME5ERTBOemMxTENKcFlYUWlPakUzTkRRME1URXhOelVzSW01aVppSTZNVGMwTkRReE1URTNOWDAuRC1TaGNRdUJCdUFFczdQd3FyZE1Ia2ZRTnlvaDZXY1pqcTNNMXdKeENPNGF5TTBOLVkwVDZLTV9JYlhxR2VPTFpjeWRWR05PTTB0XzBSc2o2eldSUXp0Ui1DcEgwMklzdlNWZzdYR3FYTFlaLU8xci04Qkx6X1NVRDhHQmNId1RJOFlZMlB4NmpmNGRzX2dlLWZMUDFZNEczelhrbGtHME13N3Zfd1pxVlFXLU5fbVFSNzJ6eTFYYXhSTHpYNXpaMGU3RWI4TEN4ZnNPeTFmSFNTRTRNQVNKX1VDREs1T1pkYldXZUgzVlgyaGwza0p5RFhHMG1EMmhsZkhTd09GMzVUckZXSlotVXlMX3dvV2hTdExiMDhnRzhYLVZDejViVlhjaDIzYVFhajdXT3pKRFhwWF8yYWszY0FkU1RuQ0hFVFBPNWgzczg1ajhnQnF5X2FKSmZR";
                var req = new HttpRequestMessage(HttpMethod.Get, "https://apis.roblox.com/asset-delivery-api/v1/assetId/" + assetId.ToString());
                req.Headers.Add("x-api-key", api_key);
                var responseMessage = await client.SendAsync(req);
                string downloadLink = await responseMessage.Content.ReadAsStringAsync();
                response = JsonSerializer.Deserialize<DownloadLink>(downloadLink);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
                return;
            }
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
            byte[] byteArray = await client.GetByteArrayAsync(response.location);
            RobloxFile file;
            try
            {
                file = RobloxFile.Open(byteArray);
            }
            catch (Exception e){
                Console.WriteLine($"stream failed: {e.Message}");
                return;
            }
            s++;
            bool susted = false;
            HashSet<string> malwareTypes = new HashSet<string>();
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
                        if(susted){
                            malwareTypes.Add("require");
                        }
                    }

                    // roblox faker check
                    if (sourceString.Contains("OFFICIAL ROBLOX")) {
                        susted = true;
                        Console.WriteLine($"Model {assetId} with script {obj.Name}:");
                        Console.WriteLine($"has OFFICIAL ROBLOX in it");
                        malwareTypes.Add("fakeOfficial");
                    }

                    // circumvention check
                    if (sourceString.Contains("game[\"Run Service\"]:IsStudio()") || sourceString.Contains(":IsStudio()")) {
                        susted = true;
                        Console.WriteLine($"Model {assetId} with script {obj.Name}:");
                        Console.WriteLine($"Checks for IsStudio");
                        malwareTypes.Add("isStudio");
                    }

                    // lines check
                    if (lines.Length > 8000) {
                        susted = true;
                        Console.WriteLine($"Model {assetId} with script {obj.Name}:");
                        Console.WriteLine("Has a suspicious number of lines");
                        malwareTypes.Add("longFile");
                    }

                    for (int i = 0; i < lines.Length; i++) {
                        if (lines[i].Length > 500) {
                            susted = true;
                            Console.WriteLine($"Model {assetId} with script {obj.Name}:");
                            Console.WriteLine($"Has a suspiciously long line on line {i + 1}: {lines[i]}");
                            malwareTypes.Add("longLine");
                            break;
                        } 
                    }

                    if(Scraper.checkHardCodedPlayerName(sourceString)){
                        susted = true;
                        Console.WriteLine($"Model {assetId} with script {obj.Name}:");
                        Console.WriteLine($"Has hard coded player name search");
                        malwareTypes.Add("hardCodedName");
                    }
                }
            }
            foreach (string name in malwareTypes){
                malwareTypeCount[name]++;
            }
            if (susted) {
                Console.WriteLine("\n");
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

