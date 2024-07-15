using System.Net;
using System.Net.Mail;
using RestSharp;
using RestSharp.Authenticators;

namespace WorkerServiceIpPublic;

public class Worker : BackgroundService
{
    private static readonly HttpClient client = new();
    private static string lastIp = string.Empty;
    
    private static readonly string ovhApplicationKey = "629bde031d91fb77";
    private static readonly string ovhApplicationSecret = "5b028ad1c4e0b20f57970c9d2fbe1b40";
    private static readonly string ovhConsumerKey = "ph121757-ovh";
    private static readonly string ovhDomain = "automationip.fr";
    private static readonly string ovhSubDomain = "subdomain";
    
    
    private static string ApplicationKey = "votre_application_key";
    private static string ApplicationSecret = "votre_application_secret";
    private static string ConsumerKey = "votre_consumer_key";
    private static string Endpoint = "https://eu.api.ovh.com/1.0/";
    
    private readonly ILogger<Worker> _logger;

    private readonly IConfiguration _configuration;
    private string? _currentIPAddress = "";

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            //var previousPublicIP = GetPublicIPAddress();
            try
            {
                var currentIp = await GetPublicIpAsync();


                if (currentIp != lastIp)
                {
                    if (_logger.IsEnabled(LogLevel.Information))
                        _logger.LogInformation("Worker running at: {time} {ip}", DateTimeOffset.Now, currentIp);
                    Console.WriteLine($"IP changed: {currentIp}");
                    await SendEmail(currentIp);
                    await UpdateDnsRecordAsync(currentIp);
                    lastIp = currentIp;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }


            await Task.Delay(5000, stoppingToken);
        }
    }

    public async Task<string> GetPublicIPAddress()
    {
        var publicIP = new WebClient().DownloadString("https://www.whatismyip.com/");
        return await Task.FromResult(publicIP);
    }

    private static async Task<string> GetPublicIpAsync()
    {
        var response = await client.GetStringAsync("https://api.ipify.org");
        return response.Trim();
    }
    
    private async Task SendEmail(string? ipAddress)
    {
        var smtpServer = _configuration["SmtpServer"];
        var smtpPort = int.Parse(_configuration["SmtpPort"]);
        var smtpUsername = _configuration["SmtpUsername"];
        var smtpPassword = _configuration["SmtpPassword"];
        var senderEmail = _configuration["SenderEmail"];
        var recipientEmail = _configuration["RecipientEmail"];

        var client = new SmtpClient(smtpServer)
        {
            Port = smtpPort,
            Credentials = new NetworkCredential(smtpUsername, smtpPassword),
            EnableSsl = true
        };

        var message = new MailMessage(senderEmail, recipientEmail)
        {
            Subject = "Nouvelle adresse IP publique",
            Body = $"Votre nouvelle adresse IP publique est : {ipAddress}"
        };

        client.Send(message);
    }

    private static async Task getDnsOvh()
    {
        string domain = "example.com";
        
        var client = new RestClient(Endpoint);
        var request = new RestRequest($"domain/zone/{domain}/record", Method.Get);
        
        AddHeaders(request);
        
        var response = await client.ExecuteAsync(request);
        
        if (response.IsSuccessful)
        {
            Console.WriteLine(response.Content);
        }
        else
        {
            Console.WriteLine("Erreur: " + response.ErrorMessage);
        }
    }
    
     private static async Task UpdateDnsRecordAsync(string newIp)
    {
   
    }
     
}


