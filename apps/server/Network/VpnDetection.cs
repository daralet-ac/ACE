using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using ACE.Server.Managers;
using Serilog;

namespace ACE.Server.Network;

public class IspInfo
{
    public string ASN { get; set; }
    public string Provider { get; set; }
    public string Continent { get; set; }
    public string Country { get; set; }
    public string IsoCode { get; set; }
    public string Region { get; set; }
    public string RegionCode { get; set; }
    public string City { get; set; }
    public float Latitude { get; set; }
    public float Longitude { get; set; }
    public string Proxy { get; set; }
    public string Type { get; set; }

    public override string ToString()
    {
        return $"ASN = {ASN}, Provider = {Provider}, Continent = {Continent}, Country = {Country}, IsoCode = {IsoCode}, Region = {Region}, RegionCode = {RegionCode}, City = {City}, Latitude = {Latitude}, Longitude = {Longitude}, Proxy = {Proxy}, Type = {Type}";
    }
}

public static class VpnDetection
{
    private static readonly ILogger _log = Log.ForContext(typeof(VpnDetection));

    private static string ApiKey { get; set; } = PropertyManager.GetString("proxycheck_api_key").Item;

    public static async Task<IspInfo> CheckVpn(string ip)
    {
        //Console.WriteLine("In VPNDetection.CheckVPN");
        if(string.IsNullOrEmpty(ip) || ip.Equals("127.0.0.1"))
        {
            return null;
        }

        var url = $"https://proxycheck.io/v2/{ip}?vpn=1&asn=1&key={ApiKey}";
        if (!string.IsNullOrWhiteSpace(ApiKey))
        {
            url += "&key=" + ApiKey;
        }

        var req = WebRequest.Create(url);
        var task = req.GetResponseAsync();
        if (await Task.WhenAny(task, Task.Delay(3000)) != task)
        {
            _log.Warning($"VPNDetection.CheckVPN task timed out for ip = {ip}");
            return null; //timed out
        }
        var resp = task.Result;
        using (var stream = resp.GetResponseStream())
        {
            using (var sr = new StreamReader(stream))
            {
                var data = await sr.ReadToEndAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };

                var d1 = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(data);
                var d = d1[ip];
                var ispInfo = new IspInfo()
                {
                    ASN = d["asn"],
                    Provider = d["provider"],
                    City = d["city"],
                    Continent = d["continent"],
                    Country = d["country"],
                    IsoCode = d["isocode"],
                    Latitude = d["latitude"],
                    Longitude = d["longitude"],
                    Proxy = d["proxy"],
                    Region = d["region"],
                    RegionCode = d["regioncode"],
                    Type = d["type"]
                };

                if(!string.IsNullOrEmpty(ispInfo.Proxy) && ispInfo.Proxy.ToLower().Equals("yes"))
                {
                    _log.Debug($"VPN detected for ip = {ip} with ISPInfo = {ispInfo.ToString()}");
                }
                //Console.WriteLine($"VPNDetection.CheckVPN returning ISPInfo = {ispinfo.ToString()}");
                return ispInfo;
            }
        }
    }
}
