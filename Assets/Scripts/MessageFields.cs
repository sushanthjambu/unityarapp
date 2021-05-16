using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Handles the Message Window Prefab used to display pop-up messages to the user
/// </summary>
public class MessageFields : MonoBehaviour,IPointerClickHandler
{
    /// <summary>
    /// Title of the Message
    /// </summary>
    [SerializeField]
    TextMeshProUGUI messageTitle;

    /// <summary>
    /// Content of the message
    /// </summary>
    [SerializeField]
    TextMeshProUGUI messageText;

    /// <summary>
    /// Text to be displayed on the OK button
    /// </summary>
    [SerializeField]
    TextMeshProUGUI okButtonText;

    /// <summary>
    /// Text to be displayed on the CANCEL button
    /// </summary>
    [SerializeField]
    TextMeshProUGUI cancelButtonText;

    /// <summary>
    /// OK Button of the message window
    /// </summary>
    [SerializeField]
    GameObject okButton;

    /// <summary>
    /// Cancel button of the message window
    /// </summary>
    [SerializeField]
    GameObject cancelButton;

    /// <summary>
    /// Used to modify the parameters of the message window
    /// </summary>
    /// <param name="msgTitle">Tilte of this particular message</param>
    /// <param name="msgText">Content of this message</param>
    /// <param name="okText">Optional - If not passed OK button is not displayed</param>
    /// <param name="cancelText">Optional - If not passed CANCEL button is not displayed</param>
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

    /// <summary>
    /// Used with the WebAR part of App. If user clicks on the link then it opens the link in browser
    /// </summary>
    /// <param name="eventData">Event of user click</param>
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
