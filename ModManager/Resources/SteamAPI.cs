using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SteamAPI;

public class SteamAPIResponse
{
    [JsonPropertyName("response")]
    public PublishedFileResponse Response { get; set; } = new();
}

public class PublishedFileResponse
{
    [JsonPropertyName("result")]
    public int Result { get; set; }

    [JsonPropertyName("resultcount")]
    public int ResultCount { get; set; }

    [JsonPropertyName("publishedfiledetails")]
    public List<PublishedFileDetail> PublishedFileDetails { get; set; } = new();
}

public class PublishedFileDetail
{
    [JsonPropertyName("publishedfileid")]
    public string PublishedFileId { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public int Result { get; set; }

    [JsonPropertyName("creator")]
    public string Creator { get; set; } = string.Empty;

    [JsonPropertyName("creator_app_id")]
    public ulong CreatorAppId { get; set; }

    [JsonPropertyName("consumer_app_id")]
    public ulong ConsumerAppId { get; set; }

    [JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;

    [JsonPropertyName("file_size")]
    public string FileSize { get; set; } = "0";

    [JsonPropertyName("file_url")]
    public string FileUrl { get; set; } = string.Empty;

    [JsonPropertyName("hcontent_file")]
    public string HContentFile { get; set; } = string.Empty;

    [JsonPropertyName("preview_url")]
    public string PreviewUrl { get; set; } = string.Empty;

    [JsonPropertyName("hcontent_preview")]
    public string HContentPreview { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("time_created")]
    public long TimeCreated { get; set; }

    [JsonPropertyName("time_updated")]
    public long TimeUpdated { get; set; }

    [JsonPropertyName("visibility")]
    public int Visibility { get; set; }

    [JsonPropertyName("banned")]
    public int Banned { get; set; }

    [JsonPropertyName("ban_reason")]
    public string BanReason { get; set; } = string.Empty;

    [JsonPropertyName("subscriptions")]
    public int Subscriptions { get; set; }

    [JsonPropertyName("favorited")]
    public int Favorited { get; set; }

    [JsonPropertyName("lifetime_subscriptions")]
    public int LifetimeSubscriptions { get; set; }

    [JsonPropertyName("lifetime_favorited")]
    public int LifetimeFavorited { get; set; }

    [JsonPropertyName("views")]
    public int Views { get; set; }

    [JsonPropertyName("tags")]
    public List<WorkshopTag> Tags { get; set; } = new();

    // Helper to format Unix creation timestamp to DateTime
    [JsonIgnore]
    public DateTime CreatedDateTime => DateTimeOffset.FromUnixTimeSeconds(TimeCreated).LocalDateTime;

    // Helper to format Unix update timestamp to DateTime
    [JsonIgnore]
    public DateTime UpdatedDateTime => DateTimeOffset.FromUnixTimeSeconds(TimeUpdated).LocalDateTime;
}

public class WorkshopTag
{
    [JsonPropertyName("tag")]
    public string Tag { get; set; } = string.Empty;

    [JsonPropertyName("admin")]
    public int Admin { get; set; }
}