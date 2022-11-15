using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Extensions.Configuration;



public class Program
{
    static string clientId = "7e9bf271-a6cd-4786-b4f6-7980ff10acf8";
    static string[] scopes = { "User.Read", "Files.ReadWrite" };
    static string sockFile = "/tmp/ds3-savebackup.sock";

    static IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();


    private static async Task Main(string[] args)
    {
        
        var app = PublicClientApplicationBuilder.Create(clientId)
            .WithRedirectUri("http://localhost")
            .Build();

        var storageProperties =
            new StorageCreationPropertiesBuilder("DS3SaveBackup_cache.json", config["WorkingDirectory"])
                .WithUnprotectedFile()
                .Build();

        var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
        cacheHelper.RegisterCache(app.UserTokenCache);


        var accounts = await app.GetAccountsAsync();
        var strAccounts = string.Join("\n\t", accounts.Select(x => x.Username));
        Console.WriteLine($"Cached accounts:\n\t{strAccounts}");

        AuthenticationResult? result = null;

        try
        {
            result = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault()).ExecuteAsync();
            Console.WriteLine(@$"Acquired token silently.");
        }
        catch (MsalUiRequiredException ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine($"Interactive login required.");
            //System.Console.WriteLine($"https://login.microsoftonline.com/common/oauth2/v2.0/authorize?client_id={clientId}&response_type=code&scope=user.read");

            result = await app.AcquireTokenWithDeviceCode(scopes, (deviceCodeResult) =>
            {
                //System.Console.WriteLine($"Device code: {deviceCodeResult.DeviceCode}");
                Console.WriteLine($"User code: {deviceCodeResult.UserCode}");
                Console.WriteLine($"Verification url: {deviceCodeResult.VerificationUrl}");
                Console.WriteLine();
                Console.WriteLine(deviceCodeResult.Message);
                return Task.FromResult(0);
            }).ExecuteAsync();
            // result = await app.AcquireTokenInteractive(scopes)
            //     .ExecuteAsync();
        }
        catch (OperationCanceledException ex)
        {
            Console.WriteLine(ex.Message);
        }
        catch (MsalServiceException ex)
        {
            Console.WriteLine(ex.Message);
        }
        catch (MsalClientException ex)
        {
            Console.WriteLine(ex.Message);
        }

        if (result is null)
        {
            Console.WriteLine("Authentication failed.");
            Environment.Exit(1);
        }

        var returnedScopes = string.Join(",", result.Scopes);

//         Console.WriteLine(@$"Account: {result?.Account.Username}
// TokenType: {result?.TokenType}
// Scopes: {returnedScopes}");


        var graphClient = new GraphServiceClient(new DelegateAuthenticationProvider((requestMessage) =>
        {
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", result?.AccessToken);
            return Task.FromResult(0);
        }));

        var me = await graphClient.Me.Request().GetAsync();
        // Console.WriteLine($"Id: {me.Id}");
        // Console.WriteLine($"Display Name: {me.DisplayName}");

        // var photo = await graphClient.Me.Photo.Request().GetAsync();

        // foreach(var odata in photo.AdditionalData)
        // {
        //     System.Console.WriteLine($"{odata.Key}: {odata.Value}");
        // }

        // var photoContent = await graphClient.Me.Photo.Content.Request().GetAsync();

        // using(var fs = new FileStream("avatar.jpg", FileMode.Create))
        // {
        //     await photoContent.CopyToAsync(fs);
        // }

        //await app.RemoveAsync(result.Account);
        // var photo = await graphClient.Me.Photo.Content.Request().GetAsync();
        // using(var fs = new FileStream("avatar."))

        // using var memory = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Hello"));
        // await graphClient.Me.Drive.Root.ItemWithPath("DS3SaveBackup\\save.txt").Content.Request().PutAsync<DriveItem>(memory);

        try
        {
            using var stream = await graphClient.Me.Drive.Root.ItemWithPath("DS3SaveBackup\\save.txt").Content.Request().GetAsync();
            using var sr = new StreamReader(stream);
            var content = await sr.ReadToEndAsync();
            Console.WriteLine($"File content: {content}");
        }
        catch (ServiceException ex)
        {
            if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine("File not exists.");
            }
            Console.WriteLine(ex.Message);
        }
    }

    public async Task Login()
    {

        

    }

    public async Task BackupSave()
    {

    }

    public async Task RestoreSave()
    {

    }
}