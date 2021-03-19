using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MessageFields : MonoBehaviour
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

}
