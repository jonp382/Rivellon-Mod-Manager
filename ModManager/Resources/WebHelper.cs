using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using ModManager.Resources;
using System.Threading;

using SteamAPI;

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

        // TODO add button to force-update workshop metadata.
        // TOOD add check to see if any mods are missing from the metadata, and if so, fetch new results.
        if(File.Exists(Path.Combine(IOHelper.CommonPaths.GetResourcesFolderPath(), "steam_api.json"))) return;

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
            var parsed = JsonSerializer.Deserialize<SteamAPIResponse>(jsonResponse);

            if(parsed?.Response?.PublishedFileDetails != null)
            {
                allResults.AddRange(parsed.Response.PublishedFileDetails);
            }

            // don't match here. match elsewhere. keep this function only for web request.
            // foreach(ModInfo mod in workshopItems)
            // {
            //     var matchingResult = allResults.FirstOrDefault(x => x.PublishedFileId == mod.WorkshopID);
            //     mod.WorkshopDetails = matchingResult;
            // }

            Debug.WriteLine($"Successfully queried steam API and obtained {allResults.Count} results.");

            File.WriteAllText(
                Path.Combine(IOHelper.CommonPaths.GetResourcesFolderPath(), "steam_api.json"), 
                jsonResponse
            );
            

            
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"Request failed: {ex.Message}");
        }
        return;
    }


    public static async Task DownloadAllPreviewImages(List<ModInfo> Mods)
    {
        var outputDirectory = Path.Combine(IOHelper.CommonPaths.GetResourcesFolderPath(), "preview-images");

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