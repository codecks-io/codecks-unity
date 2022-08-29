using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codecks.Runtime
{
    public class CodecksCardCreatorForm : MonoBehaviour
    {
        /// <summary>
        /// Reference to the CardCreator class inside the hierarchy.
        /// </summary>
        public CodecksCardCreator cardCreator;

        [Header("UI References")]
        public TMP_Dropdown categoryDropdown;
        public TMP_InputField textArea;
        public TMP_InputField emailInput;
        public TMP_Text statusText;
        public Button sendButton;

        [Header("Texts")]
        public string statusShortText;
        public string statusSending;
        public string statusSent;
        public string statusError;
        
        private byte[] queuedScreenshot;

        /// <summary>
        /// Shows the Codecks Report Form.
        /// </summary>
        public void ShowCodecksForm()
        {
            // TODO: Implement this to show the UI
            
            cardCreator.StartCoroutine(ShowCodecksFormCoroutine());
        }

        /// <summary>
        /// The coroutine that shows Codecks Report Form.
        /// </summary>
        private IEnumerator ShowCodecksFormCoroutine()
        {
            yield return new WaitForEndOfFrame();
            
            var screenshotTex = ScreenCapture.CaptureScreenshotAsTexture();

#if UNITY_STANDALONE
            queuedScreenshot = screenshotTex.EncodeToJPG();
#else
            // used on consoles to get screenshots easily
            queuedScreenshot = screenshotTex.EncodeToPNG();
#endif

            Destroy(screenshotTex);

            
            textArea.text = "";
            sendButton.interactable = true;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the Codecks Report Form
        /// </summary>
        public void HideCodecksForm()
        {
            // TODO: Implement this to hide the UI
            
            queuedScreenshot = null;
            
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Hides the Codecks Report Form after a short delay so that there is enough time to read the status text that
        /// confirms sent reports.
        /// </summary>
        private IEnumerator HideCodecksFormWithDelayCoroutine()
        {
            yield return new WaitForSeconds(1);
            HideCodecksForm();
        }

        /// <summary>
        /// Called when the -Send Report- button is clicked.
        /// </summary>
        public void OnButtonSend()
        {
            if (textArea.text.Length < 10)
            {
                statusText.text = statusShortText;
                return;
            }
            
            string reportText = $"{textArea.text}\n\n";
            reportText += GetMetaText();
            
            var files = new Dictionary<string, (byte[], CodecksCardCreator.CodecksFileType)>();
            
#if UNITY_STANDALONE
            files["screenshot.jpg"] = (queuedScreenshot, CodecksCardCreator.CodecksFileType.JPG);
#else
            files["screenshot.png"] = (queuedScreenshot, CodecksCardCreator.CodecksFileType.PNG);
#endif
            
            statusText.text = statusSending;
            sendButton.interactable = false;

            cardCreator.CreateNewCard(
                text: reportText,
                files: files,
                severity: (CodecksCardCreator.CodecksSeverity)categoryDropdown.value,
                userEmail: emailInput.text,
                resultDelegate: (success, result) =>
                {
                    if (success)
                    {
                        statusText.text = statusSent;
                        sendButton.interactable = false;
                        StartCoroutine(HideCodecksFormWithDelayCoroutine());
                    }
                    else
                    {
                        sendButton.interactable = true;
                        statusText.text = statusError;
                    }
                });
        }
        
        /// <summary>
        /// Called when the Cancel button is clicked.
        /// </summary>
        public void OnButtonCancel()
        {
            HideCodecksForm();
        }

        /// <summary>
        /// This adds some game-related text information to the card content of the report. Feel free to add your own
        /// game data here that you want to be able see it at a glance.
        /// </summary>
        private static string GetMetaText()
        {
            StringBuilder metaText = new StringBuilder(); 
            metaText.AppendLine($"```");
            metaText.AppendLine($"Platform: {Application.platform.ToString()}");
            metaText.AppendLine($"App Version: {Application.version}");
            metaText.AppendLine("```");
            return metaText.ToString();
        }

        
    }
}
