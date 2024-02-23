using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;

namespace VisaIta.Controllers;

[ApiController]
[Route("[controller]")]
public class VisaController : ControllerBase
{

    const string MAIN_URL = "https://prenotami.esteri.it/Home?ReturnUrl=%2fServices";
    const string BOOKING_PAGE = "https://prenotami.esteri.it/Services/Booking/4689";
    const int WAIT_SERVER_TIME = 30000;
    private const bool HIDE_BROWNSER = true;

    private readonly ILogger _logger;
    private readonly BrowserNewContextOptions browserOptions;
    static IPlaywright? _playwright;
    static IBrowser? browserInstance;
    
    readonly int NAVIGATION_TIMEOUT;
    readonly int ELEMENT_TIMEOUT;
    readonly string USERNAME;
    readonly string PASSWORD;

    public VisaController(ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _logger = loggerFactory.CreateLogger<VisaController>();

        browserOptions = new BrowserNewContextOptions
        {

            IgnoreHTTPSErrors = true,
            AcceptDownloads = true,
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/107.0.0.0 Safari/537.36"            
        };

        USERNAME = configuration.GetSection("Main").GetValue<string>("USERNAME"); 
        PASSWORD = configuration.GetSection("Main").GetValue<string>("PASSWORD");
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync()
    {
        _logger.LogWarning("CREATING INSTANCES");

        try
        {
            var page = await CreatePageInstanceAsync();

            var status = await DoAll(page);

            return status;
        }
        catch (Exception e)
        {
            return BadRequest(e);
        }
    }

    private async Task<StatusCodeResult> DoAll(IPage page)
    {

        int? threadIndex = Task.CurrentId;

        _logger.LogWarning($"{threadIndex} - CALLING MAIN PAGE");

        var response = await page.GotoAsync(MAIN_URL, new PageGotoOptions());

        if (response is not null && !response.Ok)
        {
            _logger.LogError($"{threadIndex} - INTERNAL SERVER ERROR");
            return new StatusCodeResult(500);
        }

        _logger.LogWarning($"{threadIndex} - FILLING UP INPUTS");

        await page.FillAsync("#login-email", USERNAME);
        await page.FillAsync("#login-password", PASSWORD);

        _logger.LogWarning($"{threadIndex} - >>> TRYING TO AUTHENTICATE <<<");

        await page.WaitForSelectorAsync("button[type='submit']");
        await page.ClickAsync("button[type='submit']");

        _logger.LogWarning($"{threadIndex} - >>> AUTHENTICATION DONE <<<");

        // GOING TO REVERSATION PAGE
        await page.GotoAsync(BOOKING_PAGE);

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        var sorryModal = await page.IsVisibleAsync("div[class='jconfirm jconfirm-light jconfirm-open']");

        if (sorryModal)
        {
            _logger.LogWarning($"{threadIndex} - >>> Reservation Unavailable <<<");
            await page.ClickAsync("div[class='jconfirm-buttons'] > button");

            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            return BadRequest();
        }
        else
        {
            var sorryMessageVisible = await page.IsVisibleAsync("h5");

            if (sorryMessageVisible)
            {
                var sorryMessage = await page.TextContentAsync("h5");

                if (sorryMessage.Contains("Sorry"))
                {
                    _logger.LogError($"{threadIndex} - SORRY, ALL APOINMENTS FOR TODAY ARE BOOKED");
                    return BadRequest();
                }
                else
                {
                    _logger.LogInformation("IT'S ON !!!");

                    await page.ScreenshotAsync(new PageScreenshotOptions
                    {
                        FullPage = true,
                        Path = "TELA.png",
                        Type = ScreenshotType.Png
                    });

                    var fullhtml = await page.ContentAsync();

                    await System.IO.File.WriteAllTextAsync("PAGE_WITH_STUFFS.html", fullhtml);

                    _logger.LogInformation("SCREENSHOT SAVED");

                    return Ok();
                }
            }
        }
        return BadRequest();
    }


    private async Task<IPage> CreatePageInstanceAsync()
    {
        _playwright = await Playwright.CreateAsync();

        var browserType = _playwright.Chromium;

        _logger.LogWarning("CREATING INSTANCES");

        browserInstance = await browserType.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = HIDE_BROWNSER,
        });

        var browserContext = await browserInstance.NewContextAsync(browserOptions);

        browserContext.SetDefaultNavigationTimeout(NAVIGATION_TIMEOUT);
        browserContext.SetDefaultTimeout(ELEMENT_TIMEOUT);

        _logger.LogWarning("CREATING PAGES INSTANCES");

        return await browserContext.NewPageAsync();
    }
}
