using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextManager : MonoBehaviour
{
    // public GameObject gameManager;
    // GameManagerScript gms;

    // Start is called before the first frame update
    void Start()
    {
        // gms = gameManager.GetComponent<GameManagerScript>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public string floor(int _floor){
        return "<size=40>B" + _floor + "</size><size=30>F</size>";
    }

    public string remain(){
        return "残り:";
    }

    public string item(){
        return "アイテム:";
    }

    public string itemCount(int gain, int total){
        return "<size=50>" + gain + "</size> /" + total;
    }

    public string goGreenLight(){
        return "ハシゴを\n降りろ!";
    }

    public string time(){
        return "タイム:";
    }

    public string timeSecond(float time){
        return time.ToString("N1") + "sec";
    }

    public string trip(){
        return "<size=35>TRIP!!</size>";
    }

    public string amps(int amp, int limit){
        return "<size=40>" + amp + "</size><size=25>A</size> /" + limit + "A";
    }
}
