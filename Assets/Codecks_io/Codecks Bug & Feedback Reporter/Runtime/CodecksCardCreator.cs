using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Codecks.Runtime
{
    [Serializable]
    public struct CardCreateRequestData
    {
        [SerializeField] public string content;
        [SerializeField] public List<string> fileNames;
        [SerializeField] public string severity;
        [SerializeField] public string userEmail;
    }

    [Serializable]
    public struct CardCreateFileResponseData
    {
        [SerializeField] public string fileName;
        [SerializeField] public string url;
        [SerializeField] public Dictionary<string, string> fields;
    }

    [Serializable]
    struct CardCreateResponseData
    {
        [SerializeField] public bool ok;
        [SerializeField] public string cardId;
        [SerializeField] public CardCreateFileResponseData[] uploadUrls;
    }

    public class CodecksCardCreator : MonoBehaviour
    {
        void IL2CPPCompatibility()
        {
            // to generate proper il2cpp code, generics must be called somewhere
            // see https://docs.unity3d.com/Manual/ScriptingRestrictions.html
            var dummy = new List<CardCreateFileResponseData>();

            throw new Exception("Never call this!");
        }

        public string codecksURL = "https://api.codecks.io/user-report/v1/create-report";
        public string defaultToken;

        private string loadedToken;

        public delegate void CardCreationResultDelegate(bool success, string result);

        private void Start()
        {
            var loadedTokenFile = Resources.Load<TextAsset>("Codecks/codecksToken");
            if (loadedTokenFile != null)
                loadedToken = loadedTokenFile.text;
        }

        static UnityWebRequest HttpPost(string url, string bodyJsonString)
        {
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            return request;
        }

        public enum CodecksFileType
        {
            Binary,
            PlainText,
            JSON,
            PNG,
            JPG
        }

        public enum CodecksSeverity
        {
            None,
            Low,
            High,
            Critical
        }

        /// <summary>
        /// Call this method to send a data request to Codecks.
        /// </summary>
        /// <param name="text">The text that will appear on the card.</param>
        /// <param name="files">The files to be sent with the report (e.g. savegame).</param>
        /// <param name="severity">The severity of the card (optional).</param>
        /// <param name="userEmail">The email of the user to receive updates (optional)</param>
        /// <param name="resultDelegate"></param>
        /// <exception cref="Exception"></exception>
        public void CreateNewCard(string text, Dictionary<string, (byte[], CodecksFileType)> files = null,
            CodecksSeverity severity = CodecksSeverity.None, string userEmail = null,
            CardCreationResultDelegate resultDelegate = null)
        {
            if (files != null && files.Any(f => f.Value.Item1 == null))
                throw new Exception("Null file in files list");

            StartCoroutine(CreateNewCardCoroutine(text, files, severity, userEmail, resultDelegate));
        }

        public void CreateNewCard(string text, CodecksSeverity severity = CodecksSeverity.None,
            string userEmail = null, CardCreationResultDelegate resultDelegate = null)
        {
            StartCoroutine(CreateNewCardCoroutine(text, null, severity, userEmail, resultDelegate));
        }

        IEnumerator CreateNewCardCoroutine(string text, Dictionary<string, (byte[], CodecksFileType)> files = null,
            CodecksSeverity severity = CodecksSeverity.None, string userEmail = null,
            CardCreationResultDelegate resultDelegate = null)
        {
            string tokenToUse = string.IsNullOrEmpty(loadedToken) ? defaultToken : loadedToken;
            if (string.IsNullOrEmpty(tokenToUse))
            {
                resultDelegate?.Invoke(false, "empty codecks token");
                yield break;
            }

            files ??= new Dictionary<string, (byte[], CodecksFileType)>();

            UnityWebRequest request;
            try
            {
                string url = codecksURL + "?token=" + tokenToUse;

                string severityStr = severity switch
                {
                    CodecksSeverity.Low => "low",
                    CodecksSeverity.High => "high",
                    CodecksSeverity.Critical => "critical",
                    _ => null
                };

                CardCreateRequestData cardData = new CardCreateRequestData
                {
                    content = text,
                    fileNames = files.Keys.ToList(),
                    severity = severityStr,
                    userEmail = userEmail
                };

                string json = JsonConvert.SerializeObject(cardData).Replace(
                    ",\"severity\":null", "");

                request = HttpPost(url, json);
            }
            catch (Exception ex)
            {
                resultDelegate?.Invoke(false, $"exception sending initial request: {ex}");
                yield break;
            }

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                resultDelegate?.Invoke(false, $"request unsuccessful: {request.result}  {request.error}");
                yield break;
            }

            CardCreateResponseData response;
            string resultString = request.downloadHandler.text;

            try
            {
                response = JsonConvert.DeserializeObject<CardCreateResponseData>(resultString);
            }
            catch (Exception ex)
            {
                resultDelegate?.Invoke(false, $"exception deserializing response: {ex}");
                yield break;
            }

            if (!response.ok)
            {
                resultDelegate?.Invoke(true, $"Codecks OK = false {resultString}");
                yield break;
            }

            foreach (var uploadUrl in response.uploadUrls)
            {
                if (!files.ContainsKey(uploadUrl.fileName))
                    throw new Exception($"Unexpected file in uploadUrls {uploadUrl.fileName}");

                List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
                foreach (var field in uploadUrl.fields)
                {
                    formData.Add(new MultipartFormDataSection(field.Key, field.Value));
                }

                var fileData = files[uploadUrl.fileName];
                string contentType;
                switch (fileData.Item2)
                {
                    default:
                        contentType = "application/octet-stream";
                        break;
                    case CodecksFileType.PlainText:
                        contentType = "text/plain";
                        break;
                    case CodecksFileType.JSON:
                        contentType = "application/json";
                        break;
                    case CodecksFileType.PNG:
                        contentType = "image/png";
                        break;
                    case CodecksFileType.JPG:
                        contentType = "image/jpeg";
                        break;
                }

                formData.Add(new MultipartFormDataSection("Content-Type", contentType));
                formData.Add(new MultipartFormFileSection("file", fileData.Item1, uploadUrl.fileName, contentType));

                UnityWebRequest uploadRequest = UnityWebRequest.Post(uploadUrl.url, formData);
                yield return uploadRequest.SendWebRequest();

                if (uploadRequest.result != UnityWebRequest.Result.Success)
                {
                    resultDelegate?.Invoke(false, $"Error uploading file {uploadUrl.fileName} to {uploadUrl.url}" +
                        $" with {fileData.Item1.Length} bytes: {uploadRequest.error}");

                    yield break;
                }
            }

            resultDelegate?.Invoke(true, response.cardId);
        }
    }
}
