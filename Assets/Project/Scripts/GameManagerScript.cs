using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManagerScript : MonoBehaviour
{
    public bool isStart, isClear;
    bool isGameOver;
    public bool isPause;
    public bool enemyFindPlayer;
    public bool toReturn;

    public GameObject TextManager;
    TextManager tms;
    public GameObject RoomManager;
    RoomManager rms;
    public GameObject BreakerManager;
    BreakerManager bms;
    public GameObject UIManager;
    UIManager ums;

    public GameObject clearPanel;

    public GameObject timeText;
    Text ttt;
    public GameObject secText;
    Text stt;

    public GameObject itemPanel;
    public GameObject itemText;
    Text itt;
    public GameObject floorText;
    TextMeshProUGUI ftt;
    public GameObject itemsCountText;
    TextMeshProUGUI ictt;

    public GameObject GameOverPanel;
    public float gopAlpha;

    public float timeLimit;
    public float time;
    float time_clear, time_enemyFindPlayer;

    public CanvasGroup GameUI, GameOverTexts, TitleUI;

    public int gain_items;
    public int total_items;

    public GameObject[] enemies;
    public int[] enemy_put_sec = new int[5];
    float enemy_time;
    public int enemies_count;
    public int enemies_count_max;

    public List<GameObject> charas = new List<GameObject>();    // 自機、敵、アイテムまとめ
    public List<GameObject> trans_walls = new List<GameObject>();   // 透過する壁
    public List<GameObject> not_trans_walls = new List<GameObject>();   // 透過しない壁

    public int floor = 1;
    public bool increaseRoom = true;

    void Start()
    {
        Application.targetFrameRate = 60;

        InitVariables();
        SetFloorText();
        time = timeLimit;
        clearPanel.SetActive(false);
    }

    void InitVariables(){   // 変数の初期化
        rms = RoomManager.GetComponent<RoomManager>();
        bms = BreakerManager.GetComponent<BreakerManager>();
        tms = TextManager.GetComponent<TextManager>();
        ums = UIManager.GetComponent<UIManager>();

        stt = secText.GetComponent<Text>();
        ttt = timeText.GetComponent<Text>();
        ftt = floorText.GetComponent<TextMeshProUGUI>();
        itt = itemText.GetComponent<Text>();
        ictt = itemsCountText.GetComponent<TextMeshProUGUI>();

        enemies_count_max = enemy_put_sec.Length;
        enemies = new GameObject[enemies_count_max];

        int temp_sec = 15;
        int temp_inc_sec = 15;
        for(int i = 0; i < enemy_put_sec.Length; i++){
            enemy_put_sec[i] = temp_sec;
            temp_sec += temp_inc_sec;
            temp_inc_sec += 5;
        }
    }

    void Update()
    {
        ManageEnemyTime();
        TransparentWalls();

        if(GameIsStop()){
            if(enemyFindPlayer) Direction_EnemyFindPlayer();
        }
        else{
            time -= Time.deltaTime;
            SetUIsInGame();
        }

        if(isStart && !toReturn && gain_items == total_items) SwitchReturn();
        DisplayGameOverPanel();

        if(isClear || isGameOver){
            if(Input.GetKeyDown(KeyCode.Space)){
                int next_rNum_x = rms.rooms_num_x, next_rNum_z = rms.rooms_num_z;
                if(isClear && increaseRoom){
                    if(rms.rooms_num_x < rms.rooms_num_max) next_rNum_x++;
                    if(rms.rooms_num_z < rms.rooms_num_max) next_rNum_z++;
                }
                else if(isGameOver) next_rNum_x = next_rNum_z = rms.rooms_num_min;
                Reset(next_rNum_x, next_rNum_z);
            }
        }
        else if(Input.GetKeyDown(KeyCode.O)){
            if(!isPause) ums.OpenOptionPanel();
            else ums.CloseOptionPanel();
        }
    }

    void ManageEnemyTime(){
        if(enemies_count < enemy_put_sec.Length){
            enemy_time = timeLimit - time;
            if(enemy_time > enemy_put_sec[enemies_count]){
                enemies_count++;
                rms.SetEnemy();
            }
        }
    }

    void Direction_EnemyFindPlayer(){
        time_enemyFindPlayer += Time.deltaTime;
        if(!isGameOver && time_enemyFindPlayer - time > 1) GameOver();
    }

    void SetUIsInGame(){
        if(GameUI.alpha < 1){
            GameUI.alpha += 0.02f;
            TitleUI.alpha -= 0.02f;
        }
        SetItemCountText();
        SetTimeSecondText();
    }

    void TransparentWalls(){    // 壁透過メイン
        InitTransWallsArray();
        InitNotTransWallArray();
        foreach(GameObject twall in trans_walls){
            not_trans_walls.Remove(twall);
            Color wall_color = twall.GetComponent<Renderer>().material.color;
            if(wall_color.a > 0.5f){
                wall_color.a -= 0.1f;
                twall.GetComponent<Renderer>().material.color = wall_color;
            }
        }
        foreach(GameObject ntwall in not_trans_walls){
            Color wall_color = ntwall.GetComponent<Renderer>().material.color;
            if(wall_color.a <= 1){
                wall_color.a += 0.05f;
                ntwall.GetComponent<Renderer>().material.color = wall_color;
            }
        }
    }

    void InitTransWallsArray(){ // trans_walls(透過する壁の配列)の初期化
        trans_walls.Clear();
        foreach(GameObject chara in charas){
            if(!chara.activeSelf) continue;
            int rx = 0, rz = 0;
            bool isTransWall = false;
            if(chara.name == "Player"){
                PlayerController pcs = chara.GetComponent<PlayerController>();
                rx = pcs.rx;
                rz = pcs.rz;
            }
            else if(chara.name == "Enemy"){
                EnemyController ecs = chara.GetComponent<EnemyController>();
                rx = ecs.rx_tw;
                rz = ecs.rz_tw;
                isTransWall = (!ecs.isStart || ecs.foundLight || ecs.isRush);
            }
            else if(chara.name == "Item"){
                ItemController ics = chara.GetComponent<ItemController>();
                rx = ics.rx_tw;
                rz = ics.rz_tw;
            }

            GameObject c_light = rms.rooms[rx, rz].transform.Find("Light").gameObject;
            if(!isTransWall && !c_light.activeSelf) continue;
            Vector3 chara_screen_pos = Camera.main.WorldToScreenPoint(chara.transform.position);
            Ray ray_chara = Camera.main.ScreenPointToRay(chara_screen_pos);
            RaycastHit[] hits = Physics.RaycastAll(ray_chara);
            foreach(RaycastHit hit in hits){
                GameObject hit_object = hit.collider.gameObject;
                if(hit_object.tag == "Wall"){
                    WallManager wms = hit_object.GetComponent<WallManager>();
                    if(!wms.isVer && wms.wx == rx && wms.wz <= rz) trans_walls.Add(hit_object);
                }
            }
        }
    }

    void InitNotTransWallArray(){ // not_trans_walls(透過しない壁の配列)の初期化
        not_trans_walls.Clear();
        foreach(GameObject wall in rms.walls){
            WallManager wms = wall.GetComponent<WallManager>();
            if(!wms.isVer) not_trans_walls.Add(wall);
        }
    }

    public void GameStart(){
        timeLimit = Mathf.FloorToInt((rms.rooms_num_x * rms.rooms_num_z * 3 + total_items * 10) / 10) * 10;
        time = timeLimit;
        isStart = true;
    }

    void SwitchReturn(){    // 行き→戻りの転換
        toReturn = true;
        SetGoGreenLightText();
    }

    void DisplayGameOverPanel(){    // gopに合わせてGameOverPanelを表示
        GameOverPanel.GetComponent<Image>().color = new Color(0, 0, 0, gopAlpha);
        if(isGameOver) GameOverTexts.alpha += 0.02f;
    }

    public void SetClear(){ // クリアタイムの決定、ClearUIを変更
        ttt.text = tms.time();
        ttt.color = new Color(0, 0, 0);
        time_clear = timeLimit - time;
        stt.text = tms.timeSecond(time_clear);
        stt.color = new Color(0, 0, 0);

        clearPanel.SetActive(true);
        GameUI.alpha = 0;
        isClear = true;
        gopAlpha = 0;
    }

    public void GameOver(){
        if(isClear) return;
        isGameOver = true;
        gopAlpha = 1;
        GameUI.alpha = 0;
    }

    public void Reset(int rNum_x, int rNum_z){ 
        charas.Clear();
        rms.Reset(rNum_x, rNum_z);
        bms.Reset();
        ResetUIs();
        ResetGmsVariables();
        ResetUIsTexts();
    }

    void ResetGmsVariables(){    // gmsの変数周りのリセット
        if(isClear) floor++;
        else if(isGameOver) floor = 1;

        isStart = false;
        isGameOver = false;
        isClear = false;
        isPause = false;
        toReturn = false;
        enemyFindPlayer = false;
        gain_items = 0;
        time = timeLimit;
        enemy_time = 0;

        for(int i = 0; i < enemies_count_max; i++){
            if(enemies[i] != null) Destroy(enemies[i]);
        }
        enemies_count = 0;
    }

    void ResetUIs(){    // UI周りのリセット
        GameOverTexts.alpha = 0;
        gopAlpha = 0;
        if(isClear) GameUI.alpha = 1;
        
        itemsCountText.SetActive(true);
        itt.text = tms.item();
        itt.color = new Color(0, 0, 0);
        itt.fontSize = 14;

        ttt.text = tms.remain();
        ttt.color = new Color(0, 0, 0);
        clearPanel.SetActive(false);
    }

    void ResetUIsTexts(){
        SetFloorText();
        SetItemCountText();
        SetTimeSecondText();
        bms.SetAmpsText();
    }

    public void Set_EnemyFindPlayer(){  // 衝突以外でのゲームオーバー時
        enemyFindPlayer = true;
        time_enemyFindPlayer = time;
    }

    public bool GameIsStop(){   // ゲームが停止状態か否か
        if(!isStart) return true;
        if(isGameOver) return true;
        if(isClear) return true;
        if(isPause) return true;
        if(enemyFindPlayer) return true;
        return false;
    }

    void SetFloorText(){
        ftt.text = tms.floor(floor);
    }

    void SetItemCountText(){
        ictt.text = tms.itemCount(gain_items, total_items);
        if(time < 20) ttt.color = new Color(1, 0, 0);
        if(time < 5) gopAlpha = 1 - time / 5;
        if(time <= 0) GameOver();
    }

    void SetGoGreenLightText(){
        itemsCountText.SetActive(false);
        itt.text = tms.goGreenLight();
        itt.color = new Color(0, 0.6f, 0);
        itt.fontSize = 30;
    }

    void SetTimeSecondText(){
        stt.text = tms.timeSecond(time);
    }
}