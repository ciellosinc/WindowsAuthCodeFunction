#r "Newtonsoft.Json"

using System.Net;
using System.Text;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives; 
using Newtonsoft.Json;
using System;

public static string ADMIN_DOMAIN      = Environment.GetEnvironmentVariable("ADMIN_DOMAIN");
public static string ADMIN_USERNAME    = Environment.GetEnvironmentVariable("ADMIN_USERNAME");
public static string ADMIN_PASSWORD    = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
public static string BASE_URI          = Environment.GetEnvironmentVariable("BASE_URI");
public static string COMPANY_ID        = Environment.GetEnvironmentVariable("COMPANY_ID"); 

public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
{
    HttpClient client = new HttpClient();
    switch(req.Method)
    {
        case "GET":
        {
            log.LogInformation("C# HTTP trigger function processed a GET request.");
            string AuthCode = "";
            string State = "";
            AuthCode = req.Query["code"];
            State = req.Query["state"];

            if(AuthCode != null && State != null)
            {
                string resp = "";
                try{
                    resp = await PostAuthCodeToBCAsync(JsonConvert.SerializeObject(new { code = AuthCode ,state = State}),log);
                    log.LogInformation(resp);
                    dynamic respData = JsonConvert.DeserializeObject(resp);
                    string responseStringMessage = "";
                    switch(respData?.value.ToString())
                    {
                        case "OK": 
                            responseStringMessage = "Authorization successfully passed. Please refresh the Square Settings page in Business Central. You can close this tab.";
                            break; 
                        case "FAILED":
                            responseStringMessage = "Authorization failed. Failed to retrieve access token. You can close this tab.";
                            break; 
                    }
                    return new OkObjectResult(responseStringMessage);
                }
                catch(Exception ex) 
                {
                    return new BadRequestObjectResult(ex.Message + ": " + resp); 
                }
            }
            else
            {
                return new BadRequestObjectResult("Authorization denied. You chose to deny access to the app.");
            }
            break;
        }
        case "POST" :  
        {
            log.LogInformation("C# HTTP trigger function processed a POST request.");
            try{
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                string resp = await PostEventToBCAsync(JsonConvert.SerializeObject(new { inputJson = requestBody}), log);
                log.LogInformation(resp);
                return new OkObjectResult(resp);
            }
            catch(Exception ex)
            {
                return new BadRequestObjectResult(ex.Message); 
            }
        break; 
        }
        default:
        {
            return new BadRequestObjectResult($"HTTPMethod {req.Method} is not supported!"); 
        }
    }
}

public static async Task<string> PostEventToBCAsync(string jsonBody, ILogger log)
{
    string postUri = $"{BASE_URI}/SquareOAuthService_GetSquareWebhookRequest?company={COMPANY_ID}";
    var uri = new Uri (postUri);
    var credentialsCache = new CredentialCache();
    credentialsCache.Add(uri, "NTLM", new NetworkCredential(ADMIN_USERNAME, ADMIN_PASSWORD, ADMIN_DOMAIN));
    var handler = new HttpClientHandler() { Credentials = credentialsCache, PreAuthenticate = true };
    var client = new HttpClient(handler) { Timeout = new TimeSpan(0, 0, 10) };
    var data = new StringContent(jsonBody, Encoding.UTF8, "application/json");
    log.LogInformation($"Sending request to {postUri}");
    log.LogInformation($"Sending request body: {data.ReadAsStringAsync().Result}");
    var response = await client.PostAsync(uri, data);
    var responseString = await response.Content.ReadAsStringAsync(); 
    return responseString; 
}

public static async Task<string> PostAuthCodeToBCAsync(string jsonBody, ILogger log)
{
    string postUri = $"{BASE_URI}/SquareOAuthService_GetAuthorizationCode?company={COMPANY_ID}";
    var uri = new Uri (postUri);
    var credentialsCache = new CredentialCache();
    credentialsCache.Add(uri, "NTLM", new NetworkCredential(ADMIN_USERNAME, ADMIN_PASSWORD, ADMIN_DOMAIN));
    var handler = new HttpClientHandler() { Credentials = credentialsCache, PreAuthenticate = true };
    var client = new HttpClient(handler) { Timeout = new TimeSpan(0, 0, 10) };
    var data = new StringContent(jsonBody, Encoding.UTF8, "application/json");
    log.LogInformation($"Sending request to {postUri}");
    log.LogInformation($"Sending request body: {data.ReadAsStringAsync().Result}");
    var response = await client.PostAsync(uri, data);
    var responseString = await response.Content.ReadAsStringAsync();
    return responseString; 
}
