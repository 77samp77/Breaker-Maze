using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public float v;

    GameObject gm;
    GameManagerScript gms;

    public GameObject RoomPrefab;
    GameObject rm;
    RoomManager rms;

    GameObject bm;
    BreakerManager bms;

    public int px, pz;

    List<GameObject> colliders = new List<GameObject>();
    public GameObject[] wallUI = new GameObject[4];    // 0123...下上左右
    public GameObject wallUIPanel;

    GameObject p_light;
    float fear;
    GameObject fearGauge;
    float fearGauge_fullWidth;
    RectTransform fgrt;

    public AudioClip sound_turnLight;
    public AudioClip sound_getItem;
    public AudioClip sound_collide;
    AudioSource AudioSource;

    void Start()
    {
        InitVariables();
    }

    void InitVariables(){   // 変数の初期化
        gm = GameObject.Find("GameManager");
        gms = gm.GetComponent<GameManagerScript>();

        rm = GameObject.Find("RoomManager");
        rms = rm.GetComponent<RoomManager>();

        bm = GameObject.Find("BreakerManager");
        bms = bm.GetComponent<BreakerManager>();

        wallUIPanel = GameObject.Find("WallPanel");
        GameObject PUI = wallUIPanel.transform.Find("PlayerUI").gameObject;
        wallUI[0] = PUI.transform.Find("Wall_Down").gameObject;
        wallUI[1] = PUI.transform.Find("Wall_Up").gameObject;
        wallUI[2] = PUI.transform.Find("Wall_Left").gameObject;
        wallUI[3] = PUI.transform.Find("Wall_Right").gameObject;

        fearGauge = GameObject.Find("Gauge");
        fgrt = fearGauge.GetComponent<RectTransform>();
        fearGauge_fullWidth = 395;

        AudioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if(gms.GameIsStop() && gms.isStart){
            return;
        }

        move();
        SetPositions();
        
        SearchWallAroundPlayer(px, pz);
        p_light = rms.rooms[px, pz].transform.Find("Light").gameObject;
        if(p_light.activeSelf) TransparentWallFrontPlayer(px, pz);
        CalFearGauge();
        if(Input.GetKeyDown(KeyCode.Space)) Examine(px, pz);
        CheckTouchingEnemy();

        if(!gms.isClear && GameIsClear()) GameClear();
    }

    void move(){    // 移動
        Vector3 pos = this.transform.position;
        if(Input.GetKey(KeyCode.W)){
            pos.z += v;
        }
        if(Input.GetKey(KeyCode.A)){
            pos.x -= v;
        }
        if(Input.GetKey(KeyCode.S)){
            pos.z -= v;
        }
        if(Input.GetKey(KeyCode.D)){
            pos.x += v;
        }
        
        if(!gms.isStart && this.transform.position != pos){
            gms.GameStart();
        }
        this.GetComponent<Rigidbody>().MovePosition(pos);
    }

    void SetPositions(){    // 相対座標の対応
        Vector3 p_pos = this.transform.position;
        Vector3 f_scl = RoomPrefab.transform.Find("Floor").localScale;
        px = (int)((p_pos.x + f_scl.x/2) / f_scl.x);
        pz = (int)((p_pos.z + f_scl.z/2) / f_scl.z);
    }

    void CalFearGauge(){    // 暗闇ゲージの更新
        if(!p_light.activeSelf) fear += 0.3f;
        else{
            LightManager lms = p_light.GetComponent<LightManager>();
            if(lms.isFix) fear -= 1.0f;
            else fear -= 0.2f;
        }
        if(fear < 0) fear = 0;
        else if(fear > 100) fear = 100;
        float fgrt_w = Mathf.Pow((fear / 100), 0.8f) * fearGauge_fullWidth;
        fgrt.sizeDelta = new Vector2(fgrt_w, fgrt.sizeDelta.y);
        if(fear > 70) gms.gopAlpha = (fear - 70) / 30;
        else gms.gopAlpha = 0;
        if(fear == 100) gms.GameOver();
    }

    void Examine(int px, int pz){   // 自分のいる部屋や周囲を調べる
        LightManager p_lms = p_light.GetComponent<LightManager>();

        if(p_light.activeSelf){
            int eil = ExistInList(colliders, "Item");
            if(eil != -1){
                AudioSource.PlayOneShot(sound_getItem);
                gms.gain_items++;
                fear -= 30;
                colliders[eil].SetActive(false);
                colliders.RemoveAt(eil);
            }
            else if(ExistInList(colliders, "Breaker") != -1) bms.isTrip = false;
            else turnLight(px, pz, p_light, p_lms);
        }
        else turnLight(px, pz, p_light, p_lms);
    }

    void CheckTouchingEnemy(){  // 敵と触れているか
        int eilc = ExistInList(colliders, "Enemy");
        if(eilc != -1){
            EnemyController ec = colliders[eilc].GetComponent<EnemyController>();
            if(ec.isRush){
                AudioSource.PlayOneShot(sound_collide);
                gms.GameOver();
            }
            else if(p_light.activeSelf){
                ec.GameOver();
            }
            wallUIPanel.GetComponent<Image>().color = new Color(0.9f, 0.5f, 0.5f, 1);
        }
        else wallUIPanel.GetComponent<Image>().color = new Color(1, 1, 1, 1);
    }

    bool GameIsClear(){ // ゲームクリアの条件を満たしているか
        if(!gms.toReturn) return false;
        if(px != rms.goal_x) return false;
        if(pz != rms.goal_z) return false;
        return true;
    }

    void GameClear(){   // ゲームクリアの処理
        gms.isClear = true;
        gms.SetClear();
    }

    void turnLight(int px, int pz, GameObject p_light, LightManager p_lms){ // 部屋のライトON/OFF
        if(bms.isTrip) return;

        if(!p_lms.isFix){
            if(p_light.activeSelf){
                AudioSource.PlayOneShot(sound_turnLight);
                p_light.SetActive(false);
                rms.bright_rooms.Remove(rms.rooms_num_x * pz + px);
            }
            else{
                AudioSource.PlayOneShot(sound_turnLight);
                p_light.SetActive(true);
                rms.bright_rooms.Add(rms.rooms_num_x * pz + px);
            }
        }
    }

    int ExistInList(List<GameObject> list, string _name){   // リストに指定した名前のオブジェクトがあるか
        for(int i = 0; i < list.Count; i++){
            if(list[i].name == _name) return i;
        }
        return -1;
    }

    void TransparentWallFrontPlayer(int px, int pz){
        Vector3 player_screen_pos = Camera.main.WorldToScreenPoint(this.transform.position);
        Ray ray_player = Camera.main.ScreenPointToRay(player_screen_pos);
        // Debug.DrawRay (ray_player.origin, ray_player.direction * 1000, Color.red, 1, false);

        RaycastHit[] hits = Physics.RaycastAll(ray_player);
        foreach(RaycastHit hit in hits){
            GameObject hit_object = hit.collider.gameObject;
            if(hit_object.tag == "Wall"){
                Color wall_color = hit_object.GetComponent<Renderer>().material.color;
                WallManager wms = hit_object.GetComponent<WallManager>();
                if(wms.wz > pz) return;
                if(!wms.isVer && wms.wx == px && wall_color.a > 0.5f){
                    wall_color.a -= 0.1f;
                    hit_object.GetComponent<Renderer>().material.color = wall_color;
                }
            }
        }
    }
    
    void SearchWallAroundPlayer(int px, int pz){    // プレイヤーが四方の壁に触れているか
        bool[] isWallAroundPlayer = new bool[4];    // 0123...下上左右
        for(int i = 0; i < colliders.Count; i++){
            if(colliders[i].tag == "Wall"){
                WallManager wms;
                if(colliders[i].name == "Wall"){
                    wms = colliders[i].GetComponent<WallManager>();
                }
                else{
                    GameObject wall_parent = colliders[i].transform.parent.gameObject;
                    wms = wall_parent.GetComponent<WallManager>();
                }
                
                if(!wms.isVer){
                    if(wms.wz == pz) isWallAroundPlayer[0] = true;
                    else if(wms.wz == pz + 1) isWallAroundPlayer[1] = true;
                }
                else{
                    if(wms.wx == px) isWallAroundPlayer[2] = true;
                    else if(wms.wx == px + 1) isWallAroundPlayer[3] = true;
                }
            }
        }

        for(int i = 0; i < 4; i++){
            wallUI[i].SetActive(isWallAroundPlayer[i]);
        }
    }

    void OnTriggerEnter(Collider collider){
        if(collider.gameObject.name == "Enemy"){
            EnemyController ec = collider.gameObject.GetComponent<EnemyController>();
            if(ec.isRush){
                AudioSource.PlayOneShot(sound_collide);
                gms.GameOver();
            }
            else if(p_light.activeSelf) ec.GameOver();
        }
        colliders.Add(collider.gameObject);
    }

    void OnTriggerExit(Collider collider){
        colliders.RemoveAt(NumInColliders(collider.gameObject));
    }

    int NumInColliders(GameObject c){   // 指定したゲームオブジェクトがリストcollidersの中で何番目か
        for(int i = 0; i < colliders.Count; i++){
            if(colliders[i] == c) return i;
        }
        return -1;
    }
}