public class DS3SaveBackupOptions
{
    public string WorkingDirectory { get; set; } = "/home/deck/.local/share/DS3SaveBackup/";

    public string ClientId { get; set; } = "";
    public string[] Scopes { get; set; } = { };
    public string Socket { get; set; } = "";
    public string CloudFolder { get; set; } = "";
    public string LocalFolderOrFile { get; set; } = "";
}