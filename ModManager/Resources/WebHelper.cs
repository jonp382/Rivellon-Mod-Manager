using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.IO;
using ModManager.Resources;
using System.Threading;

namespace WebHelper;

public class WebRequest()
{
    private static readonly HttpClient client = new HttpClient();
    
    public static async Task GetWorkshopDetails(List<ModInfo> workshopItems)
    {
        if(workshopItems is null || workshopItems.Count == 0 || workshopItems.Any(n => string.IsNullOrEmpty(n.WorkshopID)))
        {
            Debug.WriteLine($"Null input in GetWorkshopDetails. Exiting.");
            return;
        }

        var allResults = new List<PublishedFileDetail>();

        string url = "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/";
        var parameters = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("itemcount", $"{workshopItems.Count}"),
        };
        for(int i = 0; i < workshopItems.Count; i++)
        {
            parameters.Add(new KeyValuePair<string, string>($"publishedfileids[{i}]", workshopItems[i].WorkshopID));
        }

        var content = new FormUrlEncodedContent(parameters);

        try
        {
            HttpResponseMessage response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            string jsonResponse = await response.Content.ReadAsStringAsync();
            var parsed = JsonSerializer.Deserialize<SteamApiResponse>(jsonResponse);

            if(parsed?.Response?.PublishedFileDetails != null)
            {
                allResults.AddRange(parsed.Response.PublishedFileDetails);
            }

            foreach(ModInfo mod in workshopItems)
            {
                var matchingResult = allResults.FirstOrDefault(x => x.PublishedFileId == mod.WorkshopID);
                mod.WorkshopDetails = matchingResult;
            }

            Debug.WriteLine($"Successfully queried steam API and obtained {allResults.Count} results.");

            
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"Request failed: {ex.Message}");
        }
        return;
    }

    public class SteamApiResponse
    {
        [JsonPropertyName("response")]
        public PublishedFileResponse Response { get; set; }
    }

    public class PublishedFileResponse
    {
        [JsonPropertyName("result")]
        public int Result { get; set; }

        [JsonPropertyName("resultcount")]
        public int ResultCount { get; set; }

        [JsonPropertyName("publishedfiledetails")]
        public List<PublishedFileDetail> PublishedFileDetails { get; set; }
    
    }

    public class PublishedFileDetail
    {
        [JsonPropertyName("publishedfileid")]
        public string PublishedFileId { get; set; }

        [JsonPropertyName("result")]
        public int Result { get; set; } // 1 = Success, 9 = Not Found / Deleted, 15 = Access Denied

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("file_size")]
        public string FileSize { get; set; } // Note: Steam API returns file_size as a string

        [JsonPropertyName("preview_url")]
        public string PreviewUrl { get; set; }

        [JsonPropertyName("time_created")]
        public long TimeCreated { get; set; }

        [JsonPropertyName("time_updated")]
        public long TimeUpdated { get; set; }
    }

    public static async Task DownloadPreviewImage(List<ModInfo> Mods)
    {
        var outputDirectory = Path.Combine(IOHelper.SharedPaths.GetResourcesFolderPath(), "preview-images");

        if(!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);

        var validItems = Mods.Where(
            n => n.WorkshopDetails != null 
            && 
            !File.Exists(Path.Combine(outputDirectory, $"{n.Folder}.png"))
            &&
            !string.IsNullOrWhiteSpace(n.WorkshopDetails.PreviewUrl)
        );

        var options = new ParallelOptions { MaxDegreeOfParallelism = 8 };

        await Parallel.ForEachAsync(validItems, options, async (item, cancellationToken) =>
        {
            await DownloadSingleImage(item, outputDirectory, cancellationToken);
        }
        );
    }

    private static async Task DownloadSingleImage(ModInfo mod, string outputDirectory, CancellationToken cancellationToken)
    {
        string extension = Path.GetExtension(new System.Uri(mod.WorkshopDetails!.PreviewUrl).AbsolutePath);
        if (string.IsNullOrEmpty(extension)) extension = ".png";

        string fileName = $"{mod.Folder}{extension}";
        string filePath = Path.Combine(outputDirectory, fileName);

        try
        {
            using var response = await client.GetAsync(mod.WorkshopDetails.PreviewUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            
            response.EnsureSuccessStatusCode();

            await using var streamToReadFrom = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var steamToWriteTo = File.Create(filePath);
            await streamToReadFrom.CopyToAsync(steamToWriteTo, cancellationToken);

            Debug.WriteLine($"Downloaded preview image {fileName}");
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"[✗] Failed to download preview image {mod.Folder}: {ex.Message}");
        }
    }
}