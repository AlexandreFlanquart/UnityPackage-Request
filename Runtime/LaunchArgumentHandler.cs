using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Events;
using MyUnityPackage.Toolkit;

namespace Prismify.RequestKit
{
    public class LaunchArgumentHandler : MonoBehaviour
    {
        [SerializeField] private UnityEvent onUrlReceived;

    private static Dictionary<string, string> validLtiUrls = new Dictionary<string, string>
    {
        { "prod", "https://***prod.osc-fr1.scalingo.io" },
        { "staging", "https://*****-acceptance.osc-fr1.scalingo.io" }
    };

    private Dictionary<string, string> arguments;

    private static string accessToken = "";
    private static string accessTokenDebug = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJlbWFpbCI6InRlc3RAbWFpbC5jb20iLCJpZCI6Ijk3ZGZlYmY0MDk4YzBmNWMxNmJjYTYxZTJiNzZjMzczIiwiaGFzaCI6ImNjZjE5ZTU5M2MyMzVhYWE3MmY5MjJmYmI1M2QzN2IyM2YxMjYwYTkxZGZmMjM0YTM2YjAwMWU2OTc2Nzk2YTMiLCJpYXQiOjE3NDQzNTkyODYsImV4cCI6MTc0NDQ0NTY4Nn0.CpPz5ESokSKpWjrBTwEvxcTfsnjiy9lzMcPWKgpaCjs";
    private static string LtiUrl = "";
    private string appEnv = "";

    void Awake()
    {
        // Subscribe to deep link activation while the app is running
        Application.deepLinkActivated += HandleDeepLink;
    }

    void Start()
    {
        Debug.Log("Start -> Try to SetLtiUrlAndAccessToken");
#if UNITY_EDITOR
        SetLtiUrlAndAccessToken("https://*****.fra1.cdn.digitaloceanspaces.com/index.html?access_token=" + accessTokenDebug + "&app_env=prod");
#else
        SetLtiUrlAndAccessToken(Application.absoluteURL);
#endif

        if (!IsReadyToCommunicateWithLti())
        {
            Debug.Log("LaunchArgumentHandler is not ready.");
            ServiceLocator.GetService<RequestManager>().ActivePanelReconnect(true);
        }
        else
        {
           // TODO: Get Player Data
        }
    }

    private void HandleDeepLink(string url)
    {
        Debug.Log("HandleDeepLink -> URL : " + url);
        if (!string.IsNullOrEmpty(url))
        {
            //if (ServiceLocator.GetService<PlayerStatsManager>().CheckConnexion()) return;
            Debug.Log("OnApplicationFocus -> Try to SetLtiUrlAndAccessToken");
            SetLtiUrlAndAccessToken(url);
            if (IsReadyToCommunicateWithLti())
            {
                Debug.Log("LaunchArgumentHandler is ready.");
                ServiceLocator.GetService<RequestManager>().ActivePanelReconnect(false);
                // TODO: Get Player Data
            }
            else
            {
                Debug.Log("LaunchArgumentHandler is not ready.");
                ServiceLocator.GetService<RequestManager>().ActivePanelReconnect(true);
                return;
            }
            onUrlReceived?.Invoke();
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from the event to avoid memory leaks
        Application.deepLinkActivated -= HandleDeepLink;
    }

    private void SetLtiUrlAndAccessToken(string url)
    {
        Debug.Log("SetLtiUrlAndAccessToken -> URL : " + url);
        if (IsReadyToCommunicateWithLti()) return;

        arguments = GetArguments(url);
        SetAccessToken();
        SetAppEnv();

        if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(appEnv))
        {
            SetAndValidateLtiUrl(appEnv);
        }
    }

    public static bool IsReadyToCommunicateWithLti()
    {
        return !string.IsNullOrEmpty(LtiUrl) && !string.IsNullOrEmpty(accessToken);
    }

    private bool SetAndValidateLtiUrl(string appEnv)
    {
        Debug.Log("SetAndValidateLtiUrl -> AppEnv : " + appEnv);
        // Right now we need to be able to update the url as some requests will be fired even without having a valid access token...
        // To Refactor
        //if (!string.IsNullOrEmpty(baseUrl)) return true;

        var keys = new List<string>(validLtiUrls.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            if (appEnv.StartsWith(keys[i]))
            {
                Debug.Log("SetAndValidateLtiUrl -> Valid LTI URL found: " + keys[i]);
                LtiUrl = validLtiUrls[keys[i]];
                return true;
            }
        }

        Debug.Log("SetAndValidateLtiUrl -> No Valid LTI URL Found");
        return false;
    }

    public static string getLtiURL()
    {
        if (string.IsNullOrEmpty(LtiUrl))
        {
            // Set default url
            Debug.Log("getLtiURL -> No valid LTI URL found, defaulting to last (staging) URL in list");
            return new List<string>(validLtiUrls.Values).Last(); // Default to the last URL
        }

        return LtiUrl;
    }

    public static string getAccessToken()
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            // Set default url
            Debug.Log("getAccessToken -> No valid accessToken found");
            return string.Empty;
        }

        return accessToken;
    }

    private void SetAccessToken()
    {
        if (arguments != null && arguments.ContainsKey("access_token"))
        {
            Debug.Log("SetAccessToken -> Access Token : " + arguments["access_token"]);
            accessToken = arguments["access_token"];
        }
    }

    private void SetAppEnv()
    {
        if (arguments != null && arguments.ContainsKey("app_env"))
        {
            Debug.Log("SetAppEnv -> App Env : " + arguments["app_env"]);
            appEnv = arguments["app_env"];
        }
    }

    private Dictionary<string, string> GetArguments(string url)
    {
        string parameters = "";
#if UNITY_EDITOR
        // TODO: Use String Builder :D
        parameters = "access_token=" + accessTokenDebug + "&app_env=prod";
#elif (UNITY_WEBGL || UNITY_ANDROID || UNITY_IOS)
            // url with parameters syntax : http://example.com?arg1=value1&arg2=value2
            parameters = url.Substring(url.IndexOf("?")+1);
#endif

        Debug.Log("GetArguments - URL : " + url);
        Debug.Log("parameters : " + parameters);

        if (parameters == "")
            return new Dictionary<string, string>();

        string[] args = parameters.Split('&');
        Dictionary<string, string> arguments = new Dictionary<string, string>();
        foreach (string arg in args)
        {
            string[] parts = arg.Split('=');
            if (parts.Length < 2) return new Dictionary<string, string>();
            Debug.Log("parts : " + parts[0] + " : " + parts[1]);
            arguments.Add(parts[0], parts[1]);
        }

        return arguments;
        }
    }
}
