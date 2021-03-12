using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void OnOptionsClick()
    {
        GameManager.Instance.LoadLevel(GameManager.GameState.Viewer);
    }
}
