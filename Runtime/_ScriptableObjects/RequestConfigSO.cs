using System.Collections.Generic;
using UnityEngine;


namespace Prismify.RequestKit
{
    public enum EnvironmentDeployment
    {
        Development,
        Staging,
        Production
    }
    
    [CreateAssetMenu(fileName = "RequestConfigSO", menuName = "ScriptableObjects/RequestConfigSO")]
    public class RequestConfigSO : ScriptableObject
    {
        [SerializeField] private EnvironmentDeployment environmentDeployment;
        [SerializeField] private List<RequestConfig> requestConfigs;

        public RequestConfig GetRequestConfig(string requestName)
        {
            return requestConfigs.Find(config => config.requestName == requestName);
        }
    }

    [System.Serializable]
    public class RequestConfig
    {
        public string requestName;
        public RequestMethod method;
        public string url;    
    }
}
