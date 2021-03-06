using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    private static T _instance;

    public static T Instance
    {
        get { return _instance; }
    }

    protected virtual void Awake()
    {
        if(_instance != null)
        {
            Debug.LogError("[Singleton] Trying to create another instance of a singleton class.");
        }
        else
        {
            _instance = (T)this;
        }
    }

    protected virtual void OnDestroy()
    {
        if(_instance == this)
        {
            _instance = null;
        }
    }
}
