using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;
using MyUnityPackage.Toolkit;

namespace Prismify.RequestKit
{
    public enum RequestMethod
    {
        GET,
        POST
    }

    [System.Serializable]
    public class ResponseStatusHandler
    {
        public string code;
        public UnityEvent OnTrigger;
    }

    public class RequestManager : MonoBehaviour
    {
        [SerializeField] private RequestConfigSO requestConfigSO;
        [SerializeField] private List<ResponseStatusHandler> responseStatusList;
        [SerializeField] private float delayBetweenRequests = 0.2f;
        [SerializeField] private GameObject panelReconnect;
        [SerializeField] private GameObject panelError;

        private List<RequestSender> requests = new List<RequestSender>();
        private Dictionary<string, UnityEvent> serverResponseHandler;
        private int totalRequestAllowed = 15;
        private int retryAllowed = 3;
        private int tryCount = 0;
        private static bool isConnected = false;
        private bool isLoading = false;

        private void Awake()
        {
            ServiceLocator.AddService<RequestManager>(gameObject);
        }

        private void Start()
        {
            serverResponseHandler = new Dictionary<string, UnityEvent>();
            for (int i = 0; i < responseStatusList.Count; i++)
            {
                serverResponseHandler.Add(responseStatusList[i].code, responseStatusList[i].OnTrigger);
            }
        }

        public void AddRequest(string requestName, string data, bool isRequired = false, Action<RequestSender> onRequestSuccess = null, Action<RequestSender> onRequestFail = null)
        {
    #if DEMO
                    Debug.Log("RequestManager AddRequest cancelled because DEMO is enabled");
                    return;
    #endif
            Debug.Log("RequestManager SendRequest : " + requestName);
            Debug.Log("data : " + data);
            RequestConfig requestConfig = requestConfigSO.GetRequestConfig(requestName);
            if (requestConfig == null)
            {
                Debug.LogError("RequestManager: Request config not found : " + requestName);
                return;
            }
            RequestSender requestSender = new RequestSender(requestConfig.method, requestConfig.url, data, isRequired, 3);
            requestSender.onRequestSuccess += onRequestSuccess;
            requestSender.onRequestFail += onRequestFail;
            requests.Add(requestSender);
            if (requests.Count == 1 && !isLoading)
            {
                SendRequest();
            }
            else if (requests.Count > totalRequestAllowed)
            {
                Debug.LogWarning("RequestManager: Too many requests");
            }
        }

        public void SendRequest(bool alreadySet = false)
        {
            Debug.Log("SendRequest");
            isLoading = true;
            if (!alreadySet)
            {
                requests[0].onRequestSuccess += OnRequestSuccess;
                requests[0].onRequestFail += OnRequestFail;
            }
            if (!CheckInternetConnection())
            {
                Debug.LogWarning("No internet connection");
                panelError.SetActive(true);
                return;
            }
            requests[0].Send();
        }

        private void OnRequestSuccess(RequestSender requestSender)
        {
            Debug.Log("RequestManager OnRequestSuccess");
            //isConnected = true;
            panelError.SetActive(false);
            Invoke("SendNextRequest", delayBetweenRequests);
        }

        private void OnRequestFail(RequestSender requestSender)
        {
            Debug.Log("RequestManager OnRequestFail : " + requestSender.responseCode);
            //isConnected = false;

            if (requestSender.isRequired)
            {
                Debug.LogWarning("Request is required");

                if (serverResponseHandler.ContainsKey(requestSender.responseCode.ToString()))
                {
                    serverResponseHandler[requestSender.responseCode.ToString()].Invoke();
                }
                else
                {
                    Debug.Log("RequestManager: No response handler found for response code : " + requestSender.responseCode);
                    if (tryCount < retryAllowed)
                    {
                        tryCount++;
                        Debug.Log("RequestManager: request failed, trying again : " + tryCount);
                        SendRequest(true);
                        return;
                    }
                    else
                    {
                        panelError.SetActive(true);
                    }
                    tryCount = 0;
                }
                isLoading = false;
            }
            else
            {
                Invoke("SendNextRequest", delayBetweenRequests);
            }
        }

        private void SendNextRequest()
        {
            requests[0].onRequestSuccess -= OnRequestSuccess;
            requests[0].onRequestFail -= OnRequestFail;
            requests[0].Dispose();
            Debug.Log("SendNextRequest - requests.Count : " + requests.Count);
            requests.RemoveAt(0);

            if (requests.Count > 0)
            {
                Debug.Log("SendNextRequest - send next request");
                SendRequest();
            }
            else
            {
                isLoading = false;
            }
        }

        public void CancelRequests()
        {
            requests.Clear();
        }

        public static bool IsConnected()
        {
            return isConnected;
        }

        public void ActivePanelReconnect(bool active)
        {
            panelReconnect.SetActive(active);
        }

        public bool CheckInternetConnection()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.Log("internet not reachable");
                isConnected = false;
                return false;
            }
            else if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork || 
                    Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
            {
                Debug.Log("Internet connected");
                isConnected = true;
                return true;
            }
            return false;
        }
    }
}