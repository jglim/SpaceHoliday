using System.Text;
using System.Text.Json;

namespace SpaceHoliday.Holiday;


public class DGSEndpointConfig
{
    public const string ConfigFileName = "dgs.json";

    public string[] DGSResourceIdSet { get; set; } = 
    {
        "6228c3c5-03bd-4747-bb10-85140f87168b", // 2020
        "550f6e9e-034e-45a7-a003-cf7f7e252c9a", // 2021
        "04a78f5b-2d12-4695-a6cd-d2b072bc93fe", // 2022
        "98aa24ef-954d-4f76-b733-546e0fcf1d0a", // 2023
    };

    public string EndpointBaseAddress { get; set; } = "https://data.gov.sg/api/action/";
    public string EndpointRequestUri { get; set; } = "datastore_search?resource_id=";

    public string Dump()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append($"Endpoint: {EndpointBaseAddress}");
        foreach (string s in DGSResourceIdSet)
        {
            sb.Append("\r\n");
            sb.Append(s);
        }
        return sb.ToString();
    }

    public static DGSEndpointConfig LoadConfig()
    {
        var config = new DGSEndpointConfig();
        bool configLoadedSuccessfully = false;
        
        if (File.Exists(ConfigFileName))
        {
            try
            {
                config = JsonSerializer.Deserialize<DGSEndpointConfig>(File.ReadAllText(ConfigFileName));
                configLoadedSuccessfully = true;
            }
            catch (Exception ex)
            {
                // failed to load json even though the file exists (malformed?)
                Console.WriteLine($"DGS endpoint config load failed, malformed file? : {ex.Message}");
            }
        }

        // if config is unavailable, create a new default
        if (!configLoadedSuccessfully)
        {
            config = new DGSEndpointConfig();
            string configAsJson = JsonSerializer.Serialize(config);
            File.WriteAllText(ConfigFileName, configAsJson);
        }

        return config;
    }
}