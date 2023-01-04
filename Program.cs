using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Extensions.Configuration;
using System.CommandLine;
using System.Text.Json;

public class Program
{


    private static async Task<int> Main(string[] args)
    {


        var rootCommand = new RootCommand("OneDrive plugin for DS3SaveBackup");
        var configFileOption = new Option<string>(
            name: "--config",
            description: "The config file. Default to appsettings.json",
            getDefaultValue: () => "appsettings.json"
            );

        //configFileOption.SetDefaultValue("appsettings.json");
        rootCommand.AddGlobalOption(configFileOption);
        // rootCommand.SetHandler((config)=>{

        // }, configFileOption);


        var uploadCommand = new Command("upload", "Upload file to cloud.");
        rootCommand.AddCommand(uploadCommand);

        var downloadCommand = new Command("download", "Download file from cloud.");
        rootCommand.AddCommand(downloadCommand);

        var loginCommand = new Command("login", "Login via device code flow.");
        rootCommand.AddCommand(loginCommand);



        loginCommand.SetHandler(async (context) =>
        {
            var configFile = context.ParseResult.GetValueForOption(configFileOption);
            var config = ParseConfig(configFile);

            var app = PublicClientApplicationBuilder.Create(config.ClientId)
            .WithRedirectUri("http://localhost")
            .Build();

            var storageProperties =
                new StorageCreationPropertiesBuilder("DS3SaveBackup_cache.json", config.WorkingDirectory)
                    .WithUnprotectedFile()
                    .Build();

            var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
            cacheHelper.RegisterCache(app.UserTokenCache);


            var accounts = await app.GetAccountsAsync();
            var strAccounts = string.Join("\n\t", accounts.Select(x => x.Username));
            //Console.WriteLine($"Cached accounts:\n\t{strAccounts}");
            AuthenticationResult? result = null;
            string? ErrorMessage = null;

            try
            {
                result = await app.AcquireTokenSilent(config.Scopes, accounts.FirstOrDefault()).ExecuteAsync();
                //Console.WriteLine(@$"Acquired token silently.");
            }
            catch (MsalUiRequiredException ex)
            {
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine($"Interactive login required.");
                //System.Console.WriteLine($"https://login.microsoftonline.com/common/oauth2/v2.0/authorize?client_id={clientId}&response_type=code&scope=user.read");

                result = await app.AcquireTokenWithDeviceCode(config.Scopes, (deviceCodeResult) =>
                {
                    //System.Console.WriteLine($"Device code: {deviceCodeResult.DeviceCode}");
                    var res = new AuthResult
                    {
                        Ok = false,
                        LoginInfo = new AuthInfo
                        {
                            UserCode = deviceCodeResult.UserCode,
                            VerificationUrl = deviceCodeResult.VerificationUrl,
                            Message = deviceCodeResult.Message
                        }
                    };
                    System.Console.WriteLine(JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }));
                    return Task.FromResult(0);
                }).ExecuteAsync();
                // result = await app.AcquireTokenInteractive(scopes)
                //     .ExecuteAsync();
            }
            catch (OperationCanceledException ex)
            {
                ErrorMessage = ex.Message;
                //Console.Error.WriteLine(ex.Message);
            }
            catch (MsalServiceException ex)
            {
                ErrorMessage = ex.Message;
                //Console.Error.WriteLine(ex.Message);
            }
            catch (MsalClientException ex)
            {
                ErrorMessage = ex.Message;
                //Console.Error.WriteLine(ex.Message);
            }

            if (result is null)
            {
                //Console.Error.WriteLine("Authentication failed.");
                var res = new AuthResult
                {
                    Ok = false,
                    Error = ErrorMessage ?? "Authentication failed."
                };
                System.Console.WriteLine(JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }));
                context.ExitCode = 1;
                return;
            }


            // var graphClient = new GraphServiceClient(new DelegateAuthenticationProvider((requestMessage) =>
            // {
            //     requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", result?.AccessToken);
            //     return Task.FromResult(0);
            // }));

            // var me = await graphClient.Me.Request().GetAsync();
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

            System.Console.WriteLine(JsonSerializer.Serialize(new AuthResult
            {
                Ok = true,
                DisplayName = result.Account.Username,
                Id = result.UniqueId
            }, new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }));

        });


        uploadCommand.SetHandler(async (context) =>
        {
            var configFile = context.ParseResult.GetValueForOption(configFileOption);
            var config = ParseConfig(configFile);

            var app = PublicClientApplicationBuilder.Create(config.ClientId)
            .WithRedirectUri("http://localhost")
            .Build();

            var storageProperties =
                new StorageCreationPropertiesBuilder("DS3SaveBackup_cache.json", config.WorkingDirectory)
                    .WithUnprotectedFile()
                    .Build();

            var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
            cacheHelper.RegisterCache(app.UserTokenCache);

            var accounts = await app.GetAccountsAsync();
            var strAccounts = string.Join("\n\t", accounts.Select(x => x.Username));
            //Console.WriteLine($"Cached accounts:\n\t{strAccounts}");
            AuthenticationResult? result = null;

            try
            {
                result = await app.AcquireTokenSilent(config.Scopes, accounts.FirstOrDefault()).ExecuteAsync();
                //Console.WriteLine(@$"Acquired token silently.");
            }
            catch (MsalUiRequiredException ex)
            {
                Console.Error.WriteLine("Login Required: {0}", ex.Message);
                context.ExitCode = 1;
                return;
            }


            var graphClient = new GraphServiceClient(new DelegateAuthenticationProvider((requestMessage) =>
            {
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", result?.AccessToken);
                return Task.FromResult(0);
            }));
            try
            {
                foreach (var filename in System.IO.Directory.GetFiles(config.LocalFolder))
                {
                    System.Console.WriteLine($"Uploading {filename}");
                    var name = System.IO.Path.GetFileName(filename);
                    using var fs = System.IO.File.OpenRead(filename);

                    var uploadSession = await graphClient.Me.Drive.Root.ItemWithPath(config.CloudFolder + "\\" + name)
                        .CreateUploadSession(new DriveItemUploadableProperties{
                            AdditionalData = new Dictionary<string, object>
                            {
                                {"@microsoft.graph.conflictBehavior", "replace"}
                            }
                        }).Request().PostAsync();

                    
                    var fileUploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, fs);
                    var totalLength = fs.Length;
                    IProgress<long> progress = new Progress<long>(prog => {
                        System.Console.WriteLine($"Uploaded {prog} bytes of {totalLength} bytes");
                    });
                    var uploadResult = await fileUploadTask.UploadAsync(progress);
                    
                    //var resp = await graphClient.Me.Drive.Root.ItemWithPath(config.CloudFolder + "\\" + name).
                    System.Console.WriteLine($"Upload completed.");
                }

            }
            catch (ServiceException ex)
            {
                
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(ex.RawResponseBody);
                context.ExitCode = 1;
            }

        });





        downloadCommand.SetHandler(async (context) =>
        {
            var configFile = context.ParseResult.GetValueForOption(configFileOption);
            var config = ParseConfig(configFile);

            var app = PublicClientApplicationBuilder.Create(config.ClientId)
            .WithRedirectUri("http://localhost")
            .Build();

            var storageProperties =
                new StorageCreationPropertiesBuilder("DS3SaveBackup_cache.json", config.WorkingDirectory)
                    .WithUnprotectedFile()
                    .Build();

            var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
            cacheHelper.RegisterCache(app.UserTokenCache);

            var accounts = await app.GetAccountsAsync();
            var strAccounts = string.Join("\n\t", accounts.Select(x => x.Username));
            //Console.WriteLine($"Cached accounts:\n\t{strAccounts}");
            AuthenticationResult? result = null;

            try
            {
                result = await app.AcquireTokenSilent(config.Scopes, accounts.FirstOrDefault()).ExecuteAsync();
                //Console.WriteLine(@$"Acquired token silently.");
            }
            catch (MsalUiRequiredException ex)
            {
                Console.Error.WriteLine("Login Required: {0}", ex.Message);
                context.ExitCode = 1;
                return;
            }


            var graphClient = new GraphServiceClient(new DelegateAuthenticationProvider((requestMessage) =>
            {
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", result?.AccessToken);
                return Task.FromResult(0);
            }));
            try
            {
                var items = await graphClient.Me.Drive.Root.ItemWithPath(config.CloudFolder).Children.Request().GetAsync();
                foreach(var item in items.Where(x=>x.File != null))
                {
                    System.Console.Write(item.Name);
                    using var content = await graphClient.Me.Drive.Items[item.Id].Content.Request().GetAsync();
                    await content.CopyToAsync(new FileStream(System.IO.Path.Combine(config.LocalFolder, item.Name), FileMode.Create, FileAccess.Write));
                    System.Console.WriteLine("\tdownloaded.");
                }
            }
            catch (ServiceException ex)
            {
                Console.Error.WriteLine(ex.Message);
                context.ExitCode = 1;
            }
        });




        return await rootCommand.InvokeAsync(args);

    }

    private static DS3SaveBackupOptions ParseConfig(string? filename)
    {

        if (!System.IO.File.Exists(filename))
        {
            //result.ErrorMessage = "Config file does not exist";
            return new DS3SaveBackupOptions();
        }
        else
        {
            IConfiguration config = new ConfigurationBuilder()
                                        .AddJsonFile(filename)
                                        .AddEnvironmentVariables()
                                        .Build();

            var backupOptions = new DS3SaveBackupOptions();
            backupOptions.WorkingDirectory = config["WorkingDirectory"] ?? backupOptions.WorkingDirectory;
            backupOptions.ClientId = config["ClientId"] ?? backupOptions.ClientId;
            backupOptions.CloudFolder = config["CloudFolder"] ?? backupOptions.CloudFolder;
            backupOptions.LocalFolder = config["LocalFolder"] ?? backupOptions.LocalFolder;
            backupOptions.Scopes = config.GetSection("Scopes").Get<string[]>() ?? backupOptions.Scopes;
            backupOptions.Socket = config["Socket"] ?? backupOptions.Socket;

            return backupOptions;
        }
    }

}