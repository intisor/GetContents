using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using GetContents.Models;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Videos;
using System.Net;

namespace GetContents.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly YoutubeClient _youtubeClient;
    public HomeController(ILogger<HomeController> logger, YoutubeClient youtubeClient)
    {
        _logger = logger;
        _youtubeClient = youtubeClient;

        // Enforce TLS 1.2
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        // Optionally, you can add a callback to handle SSL certificate validation
        ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
    }
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }
    [HttpPost]
    public async Task<IActionResult> Download(string videoUrl)
    {
        var videoId = YoutubeExplode.Videos.VideoId.Parse(videoUrl);
        var video = await _youtubeClient.Videos.GetAsync(videoId);
        var title = video.Title;
        var author = video.Author;
        var duration = video.Duration;
        var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(videoId);
        var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
        // ...

        // Get the actual stream
        var stream = await _youtubeClient.Videos.Streams.GetAsync(streamInfo);

        // Ensure the TempFiles directory exists
        var directory = Path.Combine(Directory.GetCurrentDirectory(), "TempFiles");
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Download the stream to a file in the TempFiles directory
        var tempFilePath = Path.Combine(directory, Path.GetRandomFileName());
        await _youtubeClient.Videos.Streams.DownloadAsync(streamInfo, tempFilePath);

        // Sanitize the title for the file name
        var sanitizedTitle = string.Join("_", title.Split(Path.GetInvalidFileNameChars()));

        // Read the downloaded file
        var fileBytes = await System.IO.File.ReadAllBytesAsync(tempFilePath);

        // Return the file as a download
        return File(fileBytes, "audio/mpeg", $"{sanitizedTitle}.mp3");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
