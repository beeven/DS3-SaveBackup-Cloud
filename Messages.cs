
public class AuthResult
{
    public bool Ok {get;set;}
    public string? DisplayName {get;set;}
    public string? Id {get;set;}
    public string? PhotoBase64 {get;set;}
    public string? Error {get;set;}
}


public class AuthInfo
{
    public string DeviceCode {get;set;} = "";
    public string LoginUrl {get;set;} = "";
}