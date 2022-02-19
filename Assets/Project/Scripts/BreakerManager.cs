using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BreakerManager : MonoBehaviour
{
    public GameObject gm;
    GameManagerScript gms;

    public GameObject rm;
    RoomManager rms;

    public GameObject ampsText;
    Text att;
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
        rnx = rms.rooms_num_x;
        rnz = rms.rooms_num_z;

        att = ampsText.GetComponent<Text>();
        def_attc = att.color;
    }

    void Update()
    {
        if(gms.GameIsStop()) return;

        if(CalAmp() > limit) Trip();

        if(isTrip) att.text = "TRIP";
        else att.text = CalAmp() + "A";

        if(isTrip || CalAmp() > limit - 2) att.color = new Color(1, 0, 0);
        else att.color = def_attc;
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
}