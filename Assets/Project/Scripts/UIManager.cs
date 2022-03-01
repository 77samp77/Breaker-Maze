using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject optionPanel;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenOptionPanel(){
        optionPanel.SetActive(true);
    }

    public void CloseOptionPanel(){
        optionPanel.SetActive(false);
    }
}
