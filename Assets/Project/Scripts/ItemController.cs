using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemController : MonoBehaviour
{
    GameObject gm;
    GameManagerScript gms;

    GameObject rm;
    RoomManager rms;

    public int rx, rz; // 今いる部屋(相対座標)
    public int nrx, nrz;    // 次いく部屋(相対座標)

    public float v;
    float interval;  // 移動のあいだの時間(秒)
    bool isMoving;

    float time;

    // Start is called before the first frame update
    void Start()
    {
        InitVariables();
    }

    void InitVariables(){   // 変数の初期化
        gm = GameObject.Find("GameManager");
        gms = gm.GetComponent<GameManagerScript>();
        rm = GameObject.Find("RoomManager");
        rms = rm.GetComponent<RoomManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if(gms.GameIsStop()) return;

        time += Time.deltaTime;
        if((rx == nrx) && (rz == nrz)) DecideNextRoom();
        if(time < interval) return;
        move();
    }

    void DecideNextRoom(){  // 次の部屋を決める
        List<int> list_next_dir = new List<int>();   // 0123...下上左右
        if(canMoveZ(rz - 1)) list_next_dir.Add(0);
        if(canMoveZ(rz + 1)) list_next_dir.Add(1);
        if(canMoveX(rx - 1)) list_next_dir.Add(2);
        if(canMoveX(rx + 1)) list_next_dir.Add(3);

        int nd = list_next_dir[Random.Range(0, list_next_dir.Count)];
        if(nd == 0) nrz = rz - 1;
        else if(nd == 1) nrz = rz + 1;
        else if(nd == 2) nrx = rx - 1;
        else if(nd == 3) nrx = rx + 1;
        interval = Random.Range(0.0f, 2.0f);
    }

    bool canMoveX(int pnrx){    // 直線で横移動可能か
        if(pnrx < 0 || pnrx >= rms.rooms_num_x) return false;
        if(rms.isWalls_ver[Mathf.Max(rx, pnrx), rz]) return false;
        return true;
    }

    bool canMoveZ(int pnrz){    // 直線で縦移動可能か
        if(pnrz < 0 || pnrz >= rms.rooms_num_z) return false;
        if(rms.isWalls_hor[rx, Mathf.Max(rz, pnrz)]) return false;
        return true;
    }

    void move(){    // 移動
        Vector3 g_pos = this.transform.position;
        Vector3 nr_pos = rms.rooms[nrx, nrz].transform.position;
        nr_pos = new Vector3(nr_pos.x, g_pos.y, nr_pos.z);

        if(Mathf.Abs(g_pos.x - nr_pos.x) < v
            && Mathf.Abs(g_pos.z - nr_pos.z) < v){
            this.transform.position = nr_pos;
            rx = nrx;
            rz = nrz;
            time = 0;
        }
        else{
            if(g_pos.x > nr_pos.x) g_pos.x -= v;
            else if(g_pos.x < nr_pos.x) g_pos.x += v;
            else if(g_pos.z > nr_pos.z) g_pos.z -= v;
            else if(g_pos.z < nr_pos.z) g_pos.z += v;
            this.transform.position = g_pos;
        }
    }
}
