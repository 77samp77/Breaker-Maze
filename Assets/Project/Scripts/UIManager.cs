using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public GameObject gameManager;
    GameManagerScript gms;

    public GameObject textManager;
    TextManager tms;

    public GameObject optionPanel;
    public GameObject roomsNumText;
    public TextMeshProUGUI rntt;

    // Start is called before the first frame update
    void Start()
    {
        gms = gameManager.GetComponent<GameManagerScript>();
        tms = textManager.GetComponent<TextManager>();
        rntt = roomsNumText.GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        if(optionPanel.activeSelf) DrawOptionPanel();
    }


    int rooms_num_opt = 6;
    void DrawOptionPanel(){
        rntt.text = tms.rooms_num_inOption(rooms_num_opt);
    }

    public void OpenOptionPanel(){
        optionPanel.SetActive(true);
    }

    public void CloseOptionPanel(){
        optionPanel.SetActive(false);
    }

    public void IncreaseRoomsNum(){
        if(rooms_num_opt < 12) rooms_num_opt++;
    }

    public void DecreaseRoomsNum(){
        if(rooms_num_opt > 5) rooms_num_opt--;
    }

    public void GenerateMaze(){
        gms.Reset(rooms_num_opt, rooms_num_opt);
    }
}
