using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Codecks.Editor
{
    [System.Serializable]
    public struct TokenRequestData
    {
        [SerializeField] public string label;
    }

    [System.Serializable]
    internal struct TokenResponseData
    {
        [SerializeField] public bool ok;
        [SerializeField] public string token;
    }

    public class CodecksTokenCreator
    {
        static UnityWebRequest HttpPost(string url, string bodyJsonString)
        {
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            return request;
        }

        public static void CreateAndSetNewToken(string accessKey, string tokenLabel, Action<bool> callback)
        {
            void HandleTokenResult(string token)
            {
                if (string.IsNullOrEmpty(token))
                {
                    callback?.Invoke(false);
                    return;
                }

                bool result = false;
                try
                {
                    var resourcePath = Path.Combine(Application.dataPath, "Resources", "Codecks");
                    if (!Directory.Exists(resourcePath))
                        Directory.CreateDirectory(resourcePath);

                    var tokenPath = Path.Combine(resourcePath, "codecksToken.txt");
                    File.WriteAllText(tokenPath, token);
                    AssetDatabase.Refresh();
                    result = true;
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(new System.Exception("Error saving codecks token", ex));
                }

                callback?.Invoke(result);
            }

            try
            {
                CreateNewToken(accessKey, tokenLabel, HandleTokenResult);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(new System.Exception("Error creating codecks token", ex));
                callback?.Invoke(false);
            }
        }

        public static void CreateNewToken(string accessKey, string tokenLabel, Action<string> callback)
        {
            TokenRequestData reqData = new TokenRequestData { label = tokenLabel };
            string reqJson = JsonConvert.SerializeObject(reqData);

            var request = HttpPost($"https://api.codecks.io/user-report/v1/create-report-token?accessKey={accessKey}", reqJson);
            var reqTask = request.SendWebRequest();

            var promise = new TaskCompletionSource<string>();

            reqTask.completed += (aop) =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    UnityEngine.Debug.Log($"Codecks token response error: {request.error}: {request.downloadHandler.text}");
                    callback?.Invoke(null);
                    return;
                }

                var resultString = request.downloadHandler.text;
                var response = JsonConvert.DeserializeObject<TokenResponseData>(resultString);
                if (!response.ok)
                    callback?.Invoke(null);
                else
                    callback?.Invoke(response.token);
            };
        }
    }
}
