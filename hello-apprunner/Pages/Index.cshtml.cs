using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace hello_apprunner.Pages;

public class IndexModel : PageModel
{
    static readonly RegionEndpoint region = RegionEndpoint.USWest2;

    private readonly ILogger<IndexModel> _logger;

    public string Heading { get; internal set; }
    public List<string> Items { get; } = new List<string>();

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    public async Task<IActionResult> OnGet(string day = null)
    {
        try
        {
            _logger.LogInformation($"00 enter GET, day = {day}");

            if (day == null)
            {
                day = DateTime.Today.ToString("MM/dd");
            }

            Heading = $"What happened on {day}?";

            var client = new AmazonDynamoDBClient(region);
            Table table = Table.LoadTable(client, "OnThisDate");

            var filter = new ScanFilter();
            filter.AddCondition("Day", ScanOperator.Equal, day);

            var scanConfig = new ScanOperationConfig()
            {
                Filter = filter,
                Select = SelectValues.SpecificAttributes,
                AttributesToGet = new List<string> { "Title" }
            };

            _logger.LogInformation($"10 table.Scan");

            Search search = table.Scan(scanConfig);

            do
            {
                _logger.LogInformation($"20 table.GetNextSetAsync");
                var matches = await search.GetNextSetAsync();
                foreach (var match in matches)
                {
                    Items.Add(Convert.ToString(match["Title"]));
                }
            } while (!search.IsDone);

            _logger.LogInformation($"30 exited results loop");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "90 Exception");
            Heading = $"Error: {ex.Message}";
        }
        return Page();
    }
}
