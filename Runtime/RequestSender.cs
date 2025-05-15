using UnityEngine;
using System;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace MyUnityPackage.RequestKit
{
    public class RequestSender
    {
        public int responseCode { get; private set; }
        public bool isRequired { get; private set; }
        public string receivedData { get; private set; }
        public string data { get; private set; }

        private string url;
        private int timeOut;
        private UnityWebRequest uwr = null;
        private RequestMethod requestMethod;

        public Action<RequestSender> onRequestSuccess;
        public Action<RequestSender> onRequestFail;
        public Action<RequestSender> onRequestEnded;

        public RequestSender(RequestMethod requestMethod, string url, string data, bool isRequired = false, int timeOut = 0)
        {
            this.url = url;
            this.data = data;
            this.timeOut = timeOut;
            this.isRequired = isRequired;
            this.requestMethod = requestMethod;
            responseCode = -1;
        }

        public void Send()
        {
    #if DEMO
                Debug.Log("SendRequest cancelled because DEMO is enabled.");
                return;
    #endif

            if (!LaunchArgumentHandler.IsReadyToCommunicateWithLti())
            {
                Debug.LogWarning("SendRequest returned because LaunchArgumentHandler is not ready.");
                onRequestFail?.Invoke(this);
                return;
            }

            PrepareRequest(LaunchArgumentHandler.getLtiURL() + url, requestMethod, data, LaunchArgumentHandler.getAccessToken());
        }

        private void PrepareRequest(string pURL, RequestMethod pMethod, string pData, string pToken)
        {
            Debug.Log(" - PrepareRequest: pURL=" + pURL + " pMethod=" + pMethod + " pData=" + pData + " pToken=" + pToken);

            switch (pMethod)
            {
                case RequestMethod.GET:
                    uwr = UnityWebRequest.Get(pURL);
                    uwr.method = UnityWebRequest.kHttpVerbGET;
                    break;
                case RequestMethod.POST:
                    if (string.IsNullOrEmpty(pData))
                    {
                        Debug.LogWarning(" - PrepareRequest: pData is null");
                        uwr = UnityWebRequest.Put(pURL, "null");
                    }
                    else
                    {
                        uwr = UnityWebRequest.Put(pURL, pData);
                    }
                    uwr.method = UnityWebRequest.kHttpVerbPOST;
                    break;
                default:
                    break;
            }

            uwr.SetRequestHeader("Authorization", "Bearer " + pToken);
            uwr.SetRequestHeader("Content-Type", "application/json");
            uwr.timeout = this.timeOut;
            Debug.Log(" - PrepareRequest: SendRequestAsync");
            _ = SendRequestAsync();
            if (uwr == null)
            {
                Debug.LogError("uwr is null1");
                return;
            }
        }

        private async Task SendRequestAsync()
        {
            try
            {
                Debug.Log("SendRequestAsync : " + url);
                // Créer un TaskCompletionSource pour convertir l'opération asynchrone en Task
                var tcs = new TaskCompletionSource<UnityWebRequest>();
                var operation = uwr.SendWebRequest();
                Debug.Log("operation : " + operation + " url : " + url);
                operation.completed += op => tcs.TrySetResult(((UnityWebRequestAsyncOperation)op).webRequest);
                Debug.Log("tcs.Task : " + tcs.Task + " url : " + url);
                // Attendre la fin de la requête
                await tcs.Task;
                Debug.Log("tcs.Task after await : " + tcs.Task + " url : " + url);
                if (uwr == null)
                {
                    Debug.LogError("uwr is null2");
                    return;
                }
                TreatResponse();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Request error: {ex}");
                onRequestFail?.Invoke(this);
            }
            finally
            {
                Debug.Log("finally " + url);
            }
        }

        private void TreatResponse()
        {
            Debug.Log("TreatResponse - Request url : " + url);
            if (uwr == null)
            {
                Debug.LogWarning("uwr is null3");
                return;
            }
            if (uwr.result == null)
            {
                Debug.LogWarning("uwr.result is null");
                return;
            }
            if (uwr.responseCode == null)
            {
                Debug.LogWarning("uwr.responseCode is null");
                return;
            }
            Debug.Log("TreatResponse - uwr.result : " + uwr.result);
            Debug.Log("TreatResponse - uwr.responseCode : " + uwr.responseCode.ToString());

            responseCode = (int)uwr.responseCode;

            if (uwr.responseCode.ToString() == "200")//!(uwr.result == UnityWebRequest.Result.ConnectionError))
            {
                if (uwr.downloadHandler.data != null)
                {
                    receivedData = uwr.downloadHandler.text;
                    Debug.Log("TreatResponse - Request response : " + uwr.downloadHandler.text);
                }
                else
                {
                    Debug.LogError("TreatResponse - uwr.downloadHandler.data is null");
                }
                onRequestSuccess?.Invoke(this);
            }
            else
            {
                Debug.LogWarning("TreatResponse - Request failed");
                Debug.Log("TreatResponse - error : " + uwr.error);
                Debug.Log("TreatResponse - Request response : " + uwr.downloadHandler.text);
                onRequestFail?.Invoke(this);
            }

            onRequestEnded?.Invoke(this);
        }


        public void Dispose()
        {
            Debug.Log("Dispose : " + url);
            uwr?.Dispose();
        }
    }
}



