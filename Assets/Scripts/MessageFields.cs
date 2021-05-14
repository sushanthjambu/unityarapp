using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class MessageFields : MonoBehaviour,IPointerClickHandler
{
    [SerializeField]
    TextMeshProUGUI messageTitle;

    [SerializeField]
    TextMeshProUGUI messageText;

    [SerializeField]
    TextMeshProUGUI okButtonText;

    [SerializeField]
    TextMeshProUGUI cancelButtonText;

    [SerializeField]
    GameObject okButton;

    [SerializeField]
    GameObject cancelButton;

    public void MessageDetails(string msgTitle, string msgText, string okText = "Not Displayed", string cancelText = "Not Displayed")
    {
        messageTitle.text = msgTitle;
        messageText.text = msgText;
        if (okText != "Not Displayed")
            okButtonText.text = okText;
        else
            okButton.SetActive(false);

        if (cancelText != "Not Displayed")
            cancelButtonText.text = cancelText;
        else
            cancelButton.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(messageText, eventData.position, null);
        if (linkIndex != -1)
        {
            TMP_LinkInfo tmpLinkInfo = messageText.textInfo.linkInfo[linkIndex];
            string webUrl = tmpLinkInfo.GetLinkID();
            if (webUrl != "")
            {
                Application.OpenURL(webUrl);
            }
        }
    }
}
