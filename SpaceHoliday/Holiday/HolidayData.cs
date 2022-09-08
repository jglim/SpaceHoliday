using System.Globalization;
using System.Net;
using System.Text.Json;

namespace SpaceHoliday.Holiday;

public class HolidayData
{
    private static List<HolidayEntry> CachedHolidayEntries = new List<HolidayEntry>();
    private static List<HolidayEntry> CachedPrunedHolidayEntries = new List<HolidayEntry>();

    /// <summary>
    /// Finds the nearest future holiday entry from the now. May return null if there are no valid matches
    /// </summary>
    /// <returns>Nearest future holiday entry</returns>
    public static HolidayEntry GetNextHoliday()
    {
        /*
        // no need for prior implementation since list is pre-sorted
        HolidayEntry bestMatch = null;
        int daysToHoliday = 366;
        
        var rows = HolidayData.GetPrunedHolidayEntries();
        foreach (var row in rows)
        {
            TimeSpan ts = row.Date.Subtract(DateTime.Now);
            // Console.WriteLine($"{row.Name} on.. {row.Date.ToShortDateString()} / {ts.Days} : {row.Date.DayOfWeek}");
            if (ts.Days <= 0)
            {
                // Console.WriteLine($"discarding {row.Name}, past event");
                continue;
            }
            if (ts.Days < daysToHoliday)
            {
                daysToHoliday = ts.Days;
                // Console.WriteLine($"storing {row.Name} as current best match, days: {daysToHoliday}");
                bestMatch = row;
            }
        }
        return bestMatch;
        */
        var prunedTable = HolidayData.GetPrunedHolidayEntries();
        return (prunedTable.Count > 0) ? prunedTable[0] : null;
    }

    public static List<HolidayEntry> GetHolidayEntries()
    {
        if (CachedHolidayEntries.Count == 0)
        {
            CachedHolidayEntries = FetchHolidayData();
        }
        return CachedHolidayEntries;
    }
    public static List<HolidayEntry> GetPrunedHolidayEntries()
    {
        if (CachedPrunedHolidayEntries.Count == 0)
        {
            var today = DateTime.Today;
            var entries = GetHolidayEntries();
            foreach (var entry in entries)
            {
                // only store entries that have not elapsed
                if (entry.Date.Subtract(today).Days >= 0)
                {
                    CachedPrunedHolidayEntries.Add(entry);
                }
            }
            
            // sort by date, ascending, so that future searches are faster
            CachedPrunedHolidayEntries.Sort( (x, y) => DateTime.Compare(x.Date, y.Date));
        }
        else
        {
            // if we are fetching from an existing table, ensure that all entries are in the future
            if (CachedPrunedHolidayEntries.Count > 0)
            {
                if (CachedHolidayEntries[0].Date.Subtract(DateTime.Today).Days < 0)
                {
                    // the first entry is stale, time to regenerate this table
                    CachedHolidayEntries.Clear();
                    GetPrunedHolidayEntries();
                }
            }
        }

        return CachedPrunedHolidayEntries;
    }

    private static string[] FetchJsonFromEndpoint()
    {
        List<string> result = new();
        var config = DGSEndpointConfig.LoadConfig();

        using var client = new HttpClient(new HttpClientHandler
            { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
    
        client.BaseAddress = new Uri(config.EndpointBaseAddress);
        
        // check if we have cached the resource as a file
        foreach (string resourceId in config.DGSResourceIdSet)
        {
            string fileName = $"{resourceId}.json";
            string jsonData = string.Empty;
            if (File.Exists(fileName))
            {
                // cached json exists, load from local data
                jsonData = File.ReadAllText(fileName);
            }
            else
            {
                try
                {
                    HttpResponseMessage response =
                        client.GetAsync($"{config.EndpointRequestUri}{resourceId}").Result;
                    response.EnsureSuccessStatusCode();
                    jsonData = response.Content.ReadAsStringAsync().Result;
                    
                    // fetched endpoint json successfully, cache locally
                    File.WriteAllText(fileName, jsonData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to fetch from endpoint: {ex.Message}");
                }
            }

            // if a valid json is available, try to parse it
            if (jsonData != string.Empty)
            {
                result.Add(jsonData);
            }
        }
        return result.ToArray();
    }

    public static List<HolidayEntry> FetchHolidayData()
    {
        List<HolidayEntry> result = new();
        
        string[] endpointData = FetchJsonFromEndpoint();
        foreach (string jsonData in endpointData)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonData);
                var root = doc.RootElement;
                var rows = root.GetProperty("result").GetProperty("records");
                foreach (var row in rows.EnumerateArray())
                {
                    string date = row.GetProperty("date").GetString() ?? "2000-01-01";
                    string name = row.GetProperty("holiday").GetString() ?? "Invalid Event";
                    name = name.Trim();
                    DateTime dt = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    
                    result.Add(new HolidayEntry(){ Name =  name, Date = dt});
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse JSON {ex.Message} : {jsonData}");
            }
        }
        return result;
    }
}