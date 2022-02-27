using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    GameObject gm;
    GameManagerScript gms;

    GameObject rm;
    RoomManager rms;

    GameObject head;
    GameObject headlight;
    Light ll;

    GameObject Player;
    PlayerController pcs;

    public int rx, rz; // 今いる部屋(相対座標)
    public int nrx, nrz;    // 次いく部屋(相対座標)

    public float v;
    float interval;  // 移動のあいだの時間(秒)

    int angle;
    int p_angle, now_angle;
    // 0123...下上左右
    // Rotationだと 0:270, 1:90, 2:180, 3:0
    Vector3 rotate;
    float rot_y;
    bool isSetAngles, isTurning;

    public bool foundLight, isRush;
    bool findLightSE;
    int[] rooms_isRushed = new int[2];

    bool isStart, appearSE;
    bool moveSE, rushSE, hitSE;

    float time;

    public AudioClip sound_appear;
    public AudioClip sound_findLight;
    public AudioClip sound_move;
    public AudioClip sound_rush;
    public AudioClip sound_hit;
    AudioSource AudioSource;

    void Start()
    {
        InitVariables();
        now_angle = 3;
    }

    void InitVariables(){   // 変数の初期化
        gm = GameObject.Find("GameManager");
        gms = gm.GetComponent<GameManagerScript>();
        rm = GameObject.Find("RoomManager");
        rms = rm.GetComponent<RoomManager>();
        head = transform.Find("Head").gameObject;
        headlight = head.transform.Find("Light").gameObject;
        ll = headlight.GetComponent<Light>();
        Player = GameObject.Find("Player");
        pcs = Player.GetComponent<PlayerController>();
        AudioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if(gms.GameIsStop()) return;
        time += Time.deltaTime;

        if(!isStart){   // 出現~移動開始の間
            DisplayAppear();
            return;
        }

        if(foundLight){ // 突進時
            Rush();
            return;
        }

        if(!isTurning){ // 四方いずれかを向いているとき
            int fl = FindLight(now_angle);
            if(fl != -1) SwitchMoveMode(fl);
        }
        
        if((rx == nrx) && (rz == nrz)) DecideNextRoom();
        if(time > interval) move();
    }

    void DisplayAppear(){   // 出現時の演出
        BlinkLight();
        if(!appearSE){
            AudioSource.PlayOneShot(sound_appear);
            appearSE = true;
        }
        if(time > 2.0f){
            isStart = true;
            ll.intensity = 0;
        }
    }

    void SwitchMoveMode(int fl){  // 動きの切り替え(通常/ラッシュ)
        nrx = fl % rms.rooms_num_x;
        nrz = Mathf.FloorToInt(fl / rms.rooms_num_x);
        v = 0.8f;
        foundLight = true;
        time = 0;
    }

    void Rush(){    // ラッシュ時の演出/移動
        if(time < 1.0f){
            BlinkLight();
            if(!findLightSE){
                AudioSource.PlayOneShot(sound_findLight);
                findLightSE = true;
            }
            return;
        }

        if(!isRush) isRush = true;
        ll.intensity = 100;
        Vector3 g_pos = this.transform.position;
        Vector3 nr_pos = rms.rooms[nrx, nrz].transform.position;
        nr_pos = new Vector3(nr_pos.x, g_pos.y, nr_pos.z);

        if(!rushSE){
            AudioSource.PlayOneShot(sound_rush);
            rushSE = true;
        }

        if(Mathf.Abs(g_pos.x - nr_pos.x) < v
            && Mathf.Abs(g_pos.z - nr_pos.z) < v){
            this.transform.position = nr_pos;
            rx = nrx;
            rz = nrz;
            isSetAngles = false;

            v = 0.2f;
            ll.intensity = 0;
            rooms_isRushed[0] = rooms_isRushed[1];
            rooms_isRushed[1] = rz * rms.rooms_num_x + rx;
            findLightSE = false;
            rushSE = false;
            foundLight = false;
            isRush = false;
        }
        else{
            if(g_pos.x > nr_pos.x) g_pos.x -= v;
            else if(g_pos.x < nr_pos.x) g_pos.x += v;
            else if(g_pos.z > nr_pos.z) g_pos.z -= v;
            else if(g_pos.z < nr_pos.z) g_pos.z += v;
            this.transform.position = g_pos;
        }
    }

    void BlinkLight(){  // 光の点滅
        if(time % 0.5f < 0.25f) ll.intensity = 100;
        else ll.intensity = 0;
    }

    void DecideNextRoom(){  // 次に移動する部屋の決定
        List<int> list_next_dir = new List<int>();   // 0123...下上左右
        if(canMoveZ(rz - 1, rz)) list_next_dir.Add(0);
        if(canMoveZ(rz + 1, rz)) list_next_dir.Add(1);
        if(canMoveX(rx - 1, rx)) list_next_dir.Add(2);
        if(canMoveX(rx + 1, rx)) list_next_dir.Add(3);

        int nd = list_next_dir[Random.Range(0, list_next_dir.Count)];
        if(nd == 0) nrz = rz - 1;
        else if(nd == 1) nrz = rz + 1;
        else if(nd == 2) nrx = rx - 1;
        else if(nd == 3) nrx = rx + 1;

        interval = Random.Range(2.0f, 5.0f);
        time = 0;
    }

    bool canMoveX(int pnrx, int rx){    // 直線で横移動可能か(rx→pnrx)
        if(pnrx < 0 || pnrx >= rms.rooms_num_x) return false;
        if(rms.isWalls_ver[Mathf.Max(rx, pnrx), rz]) return false;
        return true;
    }

    bool canMoveZ(int pnrz, int rz){    // 直線で縦移動可能か(rz→pnrz)
        if(pnrz < 0 || pnrz >= rms.rooms_num_z) return false;
        if(rms.isWalls_hor[rx, Mathf.Max(rz, pnrz)]) return false;
        return true;
    }

    void move(){    // 移動
        Vector3 g_pos = this.transform.position;
        Vector3 nr_pos = rms.rooms[nrx, nrz].transform.position;
        nr_pos = new Vector3(nr_pos.x, g_pos.y, nr_pos.z);

        if(!isSetAngles){
            rotate = transform.localEulerAngles;
            rot_y = rotate.y;

            if(g_pos.x > nr_pos.x) angle = 2;
            else if(g_pos.x < nr_pos.x) angle = 3;
            else if(g_pos.z > nr_pos.z) angle = 1;
            else if(g_pos.z < nr_pos.z) angle = 0;

            isSetAngles = true;
        }
        if(angle != now_angle){
            ChangeRotation();
            return;
        }

        if(!moveSE){
            AudioSource.PlayOneShot(sound_move);
            moveSE = true;
        }

        if(Mathf.Abs(g_pos.x - nr_pos.x) < v
            && Mathf.Abs(g_pos.z - nr_pos.z) < v){  // 到着
            this.transform.position = nr_pos;
            rx = nrx;
            rz = nrz;
            moveSE = false;
            isSetAngles = false;
        }
        else{
            if(g_pos.x > nr_pos.x) g_pos.x -= v;
            else if(g_pos.x < nr_pos.x) g_pos.x += v;
            else if(g_pos.z > nr_pos.z) g_pos.z -= v;
            else if(g_pos.z < nr_pos.z) g_pos.z += v;
            this.transform.position = g_pos;
        }
    }

    int FindLight(int na){  // 向いている方向に光があるか(あったら部屋番号返す)
        if(na == 0){
            if(rz == rms.rooms_num_z - 1) return -1;
            for(int sz = rz + 1; sz <= rms.rooms_num_z; sz++){
                if(!canMoveZ(sz, sz - 1)) return -1;
                if(rms.CheckLightInRoom(rx, sz)){
                    if(isRushed(sz * rms.rooms_num_x + rx)){
                        if(ThereIsPlayer(rx, sz)) return sz * rms.rooms_num_x + rx;
                        return -1;
                    }
                    return sz * rms.rooms_num_x + rx;
                }
            }
        }
        else if(na == 1){
            if(rz == 0) return -1;
            for(int sz = rz - 1; sz >= 0; sz--){
                if(!canMoveZ(sz, sz + 1)) return -1;
                if(rms.CheckLightInRoom(rx, sz)){
                    if(isRushed(sz * rms.rooms_num_x + rx)){
                        if(ThereIsPlayer(rx, sz)) return sz * rms.rooms_num_x + rx;
                        return -1;
                    }
                    return sz * rms.rooms_num_x + rx;
                }
            }
        }
        else if(na == 2){
            if(rx == 0) return -1;
            for(int sx = rx - 1; sx >= 0; sx--){
                if(!canMoveX(sx, sx + 1)) return -1;
                if(rms.CheckLightInRoom(sx, rz)){
                    if(isRushed(rz * rms.rooms_num_x + sx)){
                        if(ThereIsPlayer(sx, rz)) return rz * rms.rooms_num_x + sx;
                        return -1;
                    }
                    return rz * rms.rooms_num_x + sx;
                }
            }
        }
        else if(na == 3){
            if(rx == rms.rooms_num_x - 1) return -1;
            for(int sx = rx + 1; sx <= rms.rooms_num_x; sx++){
                if(!canMoveX(sx, sx - 1)) return -1;
                if(rms.CheckLightInRoom(sx, rz)){
                    if(isRushed(rz * rms.rooms_num_x + sx)){
                        if(ThereIsPlayer(sx, rz)) return rz * rms.rooms_num_x + sx;
                        return -1;
                    }
                    return rz * rms.rooms_num_x + sx;
                }
            }
        }

        return -1;
    }

    bool isRushed(int room_num){    // 以前2回分、突進した部屋を記録
        for(int i = 0; i < 2; i++){
            if(rooms_isRushed[i] == room_num) return true;
        }
        return false;
    }

    bool ThereIsPlayer(int rx, int rz){ // 指定した部屋にプレイヤーがいるか
        if(rx == pcs.rx && rz == pcs.rz) return true;
        return false;
    }

    void ChangeRotation(){  // 時計回りに90°回転
        if(now_angle == 3) p_angle = 1;
        else if(now_angle == 1) p_angle = 2;
        else if(now_angle == 2) p_angle = 0;
        else p_angle = 3;

        transform.localEulerAngles = new Vector3(rotate.x, rot_y, rotate.z);
        isTurning = false;
        if(!angleIsCorrect(p_angle)){
            isTurning = true;
            rot_y += 3;
        }
    }
    
    // Rotationだと 0:270, 1:90, 2:180, 3:0
    bool angleIsCorrect(int ang){   // 目標角度と一致しているか
        int aim;
        if(ang == 0) aim = 270;
        else if(ang == 1) aim = 90;
        else if(ang == 2) aim = 180;
        else aim = 0;
        
        float diff = Mathf.Abs(rot_y % 360 - aim);
        if(diff > 180) diff -= 180;

        if(diff < 5){
            rot_y = aim;
            now_angle = ang;
            return true;
        }
        else return false;
    }

    public void GameOver(){
        ll.intensity = 100;
        AudioSource.PlayOneShot(sound_hit);
        gms.Set_EnemyFindPlayer();
    }
}