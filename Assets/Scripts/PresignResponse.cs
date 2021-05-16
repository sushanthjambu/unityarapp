using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Used to serialize the Json Response of Presign Request from Amazon S3  
/// </summary>
[Serializable]
public class PresignResponse
{
    public string url;
    public Fields fields;
}

[Serializable]
public class Fields
{
    public string acl;
    public string key;

    public string xamzalgorithm;
    public string xamzcredential;
    public string xamzdate;
    public string policy;
    public string xamzsignature;
}
