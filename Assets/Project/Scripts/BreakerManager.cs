using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BreakerManager : MonoBehaviour
{
    public GameObject gm;
    GameManagerScript gms;

    public GameObject rm;
    RoomManager rms;

    public GameObject tm;
    TextManager tms;

    public GameObject ampsText;
    TextMeshProUGUI att;
    Color def_attc;

    public int limit;
    public bool isTrip;

    int rnx, rnz;

    void Start()
    {
        InitVariables();
    }

    void InitVariables(){   // 変数の初期化
        gms = gm.GetComponent<GameManagerScript>();
        rms = rm.GetComponent<RoomManager>();
        tms = tm.GetComponent<TextManager>();
        rnx = rms.rooms_num_x;
        rnz = rms.rooms_num_z;

        att = ampsText.GetComponent<TextMeshProUGUI>();
        def_attc = att.color;
    }

    void Update()
    {
        if(gms.GameIsStop()) return;

        if(CalAmp() > limit) Trip();
        SetAmpsText();
    }

    public int CalAmp(){    // 電流の計算
        int amp = 0;
        for(int x = 0; x < rnx; x++){
            for(int z = 0; z < rnz; z++){
                GameObject a_light = rms.rooms[x, z].transform.Find("Light").gameObject;
                LightManager a_lms = a_light.GetComponent<LightManager>();
                if(a_lms.isFix) continue;
                if(a_light.activeSelf) amp += 1;
            }
        }
        return amp;
    }

    void Trip(){    // 限界をこえたときブレーカー落とす
        for(int x = 0; x < rnx; x++){
            for(int z = 0; z < rnz; z++){
                GameObject a_light = rms.rooms[x, z].transform.Find("Light").gameObject;
                LightManager a_lms = a_light.GetComponent<LightManager>();
                if(!a_lms.isFix) a_light.SetActive(false);
            }
        }
        isTrip = true;
    }

    public void Reset(){
        isTrip = false;
    }

    public void SetAmpsText(){
        if(isTrip) att.text = tms.trip();
        else att.text = tms.amps(CalAmp(), limit);

        if(isTrip || CalAmp() > limit - 2) att.color = new Color(1, 0, 0);
        else att.color = def_attc;
    }
}