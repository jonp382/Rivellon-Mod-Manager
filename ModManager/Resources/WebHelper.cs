using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using ModManager.Resources;
using System.Threading;

using SteamAPI;
using System;
using Avalonia.Controls;

namespace WebHelper;

public class WebRequest()
{
    private static readonly HttpClient client = CreateClient();

    private static HttpClient CreateClient()
    {
        var httpClient = new HttpClient();
        // github's API rejects requests without a user agent. set it once here, since adding it
        // on every request makes the header grow each time.
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ModManager");
        return httpClient;
    }
    
    public static async Task GetWorkshopDetails(List<ModInfo> workshopItems)
    {
        if(workshopItems is null || workshopItems.Count == 0 || workshopItems.Any(n => string.IsNullOrEmpty(n.WorkshopID)))
        {
            Log.Info($"Null input in GetWorkshopDetails. Exiting.");
            return;
        }

        // TODO add button to force-update workshop metadata.
        // TOOD add check to see if any mods are missing from the metadata, and if so, fetch new results.

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

            Log.Info($"Successfully queried steam API and obtained {allResults.Count} results.");

            MergeIntoCache(allResults);
        }
        catch (System.Exception ex)
        {
            Log.Warn($"Request failed: {ex.Message}");
        }
        return;
    }

    // the request only asks for mods that are missing a preview image, so merge the new
    // results into the existing cache instead of overwriting it, otherwise every other mod
    // loses its workshop details.
    private static void MergeIntoCache(List<PublishedFileDetail> newResults)
    {
        var resourcesFolder = IOHelper.CommonPaths.GetResourcesFolderPath();
        Directory.CreateDirectory(resourcesFolder);
        var cachePath = Path.Combine(resourcesFolder, "steam_api.json");

        var cache = new SteamAPIResponse();
        if(File.Exists(cachePath))
        {
            try
            {
                cache = JsonSerializer.Deserialize<SteamAPIResponse>(File.ReadAllText(cachePath)) ?? new SteamAPIResponse();
            }
            catch (System.Exception ex)
            {
                Log.Info($"Existing steam cache was unreadable, starting a new one: {ex.Message}");
            }
        }

        var merged = cache.Response.PublishedFileDetails
            .Where(n => !string.IsNullOrEmpty(n.PublishedFileId))
            .ToDictionary(n => n.PublishedFileId);

        // new results replace old ones with the same ID
        foreach(var detail in newResults)
        {
            if(string.IsNullOrEmpty(detail.PublishedFileId)) continue;
            merged[detail.PublishedFileId] = detail;
        }

        cache.Response.PublishedFileDetails = merged.Values.ToList();
        cache.Response.ResultCount = cache.Response.PublishedFileDetails.Count;

        File.WriteAllText(cachePath, JsonSerializer.Serialize(cache));
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

            Log.Info($"Downloaded preview image {fileName}");
        }
        catch (System.Exception ex)
        {
            Log.Warn($"Failed to download preview image {mod.Folder}: {ex.Message}");
        }
    }

    public static async Task OpenURL(string URL, TopLevel topLevel)
    {
        if(Uri.TryCreate(URL, UriKind.Absolute, out Uri? uri))
        {
            await topLevel.Launcher.LaunchUriAsync(uri);
        }
    }   

    // returns the path of the downloaded zip, or null if anything went wrong.
    public static async Task<string?> DownloadScriptExtender()
    {
        string URL = "https://api.github.com/repos/Norbyte/ositools/releases/latest";

        try
        {
            var response = await client.GetAsync(URL);
            response.EnsureSuccessStatusCode();

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            if(!json.RootElement.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
            {
                Log.Info($"Script extender release had no assets list");
                return null;
            }

            JsonElement? asset = null;
            foreach(var a in assets.EnumerateArray())
            {
                if(a.TryGetProperty("name", out var assetName) && (assetName.GetString()?.StartsWith("ScriptExtender-Updater-v") ?? false))
                {
                    asset = a;
                    break;
                }
            }

            if(asset == null)
            {
                Log.Info($"No ScriptExtender-Updater asset found in the latest release");
                return null;
            }

            string? name = asset.Value.GetProperty("name").GetString();
            string? url = asset.Value.TryGetProperty("browser_download_url", out var urlProperty) ? urlProperty.GetString() : null;
            if(string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url)) return null;

            string outDir = Path.Combine(IOHelper.CommonPaths.GetBaseFolderPath(), "temp-downloads");
            Directory.CreateDirectory(outDir);

            // only use the file name part, in case the asset name contains path characters
            string outFile = Path.Combine(outDir, Path.GetFileName(name));

            var zipBytes = await client.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(outFile, zipBytes);

            return outFile;
        }
        catch (System.Exception ex)
        {
            Log.Warn($"Failed to download the script extender: {ex.Message}");
            return null;
        }
    }
}