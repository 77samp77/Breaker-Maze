using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public GameObject gameManager;
    GameManagerScript gms;
    public GameObject roomManager;
    RoomManager rms;
    public GameObject textManager;
    TextManager tms;

    public GameObject optionPanel;
    public GameObject roomsNumText;
    TextMeshProUGUI rntt;

    public GameObject checkIncreaseRoom, checkDirectionalLight;

    // Start is called before the first frame update
    void Start()
    {
        gms = gameManager.GetComponent<GameManagerScript>();
        rms = roomManager.GetComponent<RoomManager>();
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
        gms.isPause = true;
        optionPanel.SetActive(true);
    }

    public void CloseOptionPanel(){
        gms.isPause = false;
        optionPanel.SetActive(false);
    }

    public void IncreaseRoomsNum(){
        if(rooms_num_opt < rms.rooms_num_max) rooms_num_opt++;
    }

    public void DecreaseRoomsNum(){
        if(rooms_num_opt > rms.rooms_num_min) rooms_num_opt--;
    }

    public void GenerateMaze(){
        gms.Reset(rooms_num_opt, rooms_num_opt);
    }

    public void CheckBoxIncreaseRoom(){
        gms.increaseRoom = !gms.increaseRoom;
        checkIncreaseRoom.SetActive(gms.increaseRoom);
    }

    public void CheckBoxDirectionalLight(){
        gms.DirectionalLight.SetActive(!gms.DirectionalLight.activeSelf);
        checkDirectionalLight.SetActive(gms.DirectionalLight.activeSelf);
    }
}
