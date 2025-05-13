using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Prismify.RequestKit
{
    public class Request : MonoBehaviour
    {
        [SerializeField]
        private string method = default;
        [SerializeField]
        private GameObject loadingPanel;
        [SerializeField]
        private RequestMethod type = RequestMethod.POST;
        [SerializeField]
        private int timeOut = default;
        [SerializeField]
        private UnityEvent OnRequestSuccess = default;
        [SerializeField]
        private UnityEvent OnRequestFail = default;

        public static bool isReady = true;
        public bool isSucced = false;


        private UnityWebRequest uwr = null;
        private string data = default;

        [Space(5)]
        [SerializeField]
        private List<ResponseStatusHandler> responseStatusList;

        private Dictionary<string, UnityEvent> serverResponseHandler;
        private List<string> requestList = new List<string>();

        #region Events
        public delegate void RequestEvent();
        public event RequestEvent RequestSuccess;
        public event RequestEvent RequestFail;
        static bool isConnectionLost = false;

        public void AddSuccessListener(RequestEvent listener)
        {
            if (RequestSuccess == null || !RequestSuccess.GetInvocationList().ToList().Contains(listener))
            {
                RequestSuccess += listener;
            }
        }

        public void AddFailListener(RequestEvent listener)
        {
            if (RequestFail == null || !RequestFail.GetInvocationList().ToList().Contains(listener))
            {
                RequestFail += listener;
            }
        }

        public void RemoveAllListener()
        {
            RequestSuccess = null;
            RequestFail = null;
        }
        #endregion

        private string receivedData;
        public string ReceivedData { get => receivedData; }

        void Start()
        {
            serverResponseHandler = new Dictionary<string, UnityEvent>();
            for (int i = 0; i < responseStatusList.Count; i++)
            {
                serverResponseHandler.Add(responseStatusList[i].code, responseStatusList[i].OnTrigger);
            }

        }

        public void Send(string pData = "")
        {
    #if DEMO
            return;
    #endif

            if (!LaunchArgumentHandler.IsReadyToCommunicateWithLti()) {
                Debug.Log(gameObject.name + " - RequestManager SendRequest returned because LaunchArgumentHandler is not ready.");
                return;
            }

            //Debug.Log("token : " + LaunchArgumentHandler.getAccessToken() + " method : " + method + " pData : " + pData);
            requestList.Add(pData);
            if (requestList.Count == 1)
            {
                Debug.Log("Send : PrepareRequest");
                //Debug.Log(this.name + " -> Send : PrepareRequest, LtiUrl : " + LaunchArgumentHandler.getLtiURL() + this.method + " type : " + this.type + " pData : " + pData + " token : " + LaunchArgumentHandler.getAccessToken());
                PrepareRequest(LaunchArgumentHandler.getLtiURL() + this.method, this.type, requestList[0], LaunchArgumentHandler.getAccessToken());
            }
        }

        public void Prepare(string pData, string pToken = null)
        {
            PrepareRequest(LaunchArgumentHandler.getLtiURL() + this.method, this.type, pData, pToken);
        }

        public void SetToken(string pToken)
        {
            if (pToken != null)
            {
                uwr.SetRequestHeader("Authorization", "Bearer " + pToken);
            }
        }

        private void PrepareRequest(string pURL, RequestMethod pMethod, string pData, string pToken)
        {
            Debug.Log(gameObject.name + " - PrepareRequest: pURL=" + pURL + " pMethod=" + pMethod + " pData=" + pData + " pToken=" + pToken);

            loadingPanel.SetActive(true);
            uwr = UnityWebRequest.Put(pURL, pData);

            SetToken(pToken);
            switch (pMethod)
            {
                case RequestMethod.GET:
                    uwr.method = UnityWebRequest.kHttpVerbGET;
                    uwr = UnityWebRequest.Get(pURL);
                    uwr.SetRequestHeader("Content-Type", "application/json");
                    break;
                case RequestMethod.POST:
                    if (string.IsNullOrEmpty(pData))
                    {
                        uwr = UnityWebRequest.Put(pURL, "null");
                    }
                    else
                    {
                        uwr = UnityWebRequest.Put(pURL, pData);
                    }
                    uwr.method = UnityWebRequest.kHttpVerbPOST;
                    uwr.SetRequestHeader("Content-Type", "application/json");
                    break;
                default:
                    break;
            }
            SetToken(pToken);
            uwr.timeout = this.timeOut;
            StartCoroutine(SendRequest());
        }

        private IEnumerator SendRequest()
        {
            yield return uwr.SendWebRequest();
            loadingPanel.SetActive(false);

            TreatResponse();
            uwr.Dispose();
            yield return new WaitForSeconds(0.2f);
        // isReady = true;
            requestList.Remove(requestList[0]);
            if (requestList.Count >= 1)
            {
                Debug.Log("Error: Resend");

                PrepareRequest(LaunchArgumentHandler.getLtiURL() + this.method, this.type, requestList[0], LaunchArgumentHandler.getAccessToken());
            }
        }

        private void TreatResponse()
        {
            bool ok = true; //httpResponseCodeManager.CheckResponseCode(uwr.responseCode.ToString());

            Debug.Log("Request : " + gameObject.name);
            Debug.Log("uwr.result : " + uwr.result);

            Debug.Log("uwr.responseCode : " + uwr.responseCode.ToString());
            //playerStatsManager.SetConnexion(uwr.responseCode == 200);

            // Debug
            if (uwr.responseCode != 200 && uwr.responseCode != 401)
            {
                Debug.LogError(gameObject.name + " uwr.responseCode : " + uwr.responseCode);
            }

            isSucced = uwr.responseCode == 200;
            if (serverResponseHandler.ContainsKey(uwr.responseCode.ToString()))
            {
                Debug.Log("Found");
                serverResponseHandler[uwr.responseCode.ToString()].Invoke();
            }
            else{
                if (!(uwr.result == UnityWebRequest.Result.ConnectionError))
                {
                    if (ok)
                    {
                        isConnectionLost = false;
                        if (uwr.downloadHandler.data != null)
                        {
                            receivedData = uwr.downloadHandler.text;
                            Debug.Log("OK data : " + receivedData);
                        }
                        else
                        {
                            Debug.LogError("uwr.downloadHandler.data is null");
                        }
                        this.OnRequestSuccess.Invoke();
                        if (RequestSuccess != null)
                        {
                            RequestSuccess.Invoke();
                        }
                    }
                }
                else
                {
                    this.OnRequestFail.Invoke();
                    if (RequestFail != null)
                    {
                        RequestFail.Invoke();
                    }
                    if (uwr.responseCode == 500)
                    {
                        if (!isConnectionLost)
                        {
                            Debug.LogError("Connection Error");
                            Debug.LogError(method + " : " + uwr.error);
                            Debug.LogError(method + " : " + uwr.url);
                        }
                        isConnectionLost = true;
                    }
                    else
                    {
                        isConnectionLost = false;
                        Debug.LogError("Connection Error");
                        Debug.LogError(method + " : " + uwr.error);
                        Debug.LogError(method + " : " + uwr.url);
                    }

                }
            }
            //Debug.Log($"<color=#40DDFF>{method} [{uwr.responseCode}] -> </color>{uwr.downloadHandler.text}");
            uwr.Dispose();
        }

        public void Dispose()
        {
        // receivedData.Dispose();
        }

        [System.Serializable]
        public class ResponseStatusHandler
        {
            public string code;
            public UnityEvent OnTrigger;
        }
    }
}
