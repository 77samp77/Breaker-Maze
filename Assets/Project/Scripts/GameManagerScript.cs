using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManagerScript : MonoBehaviour
{
    public bool isStart, isClear;
    bool isGameOver, isPause;
    public bool enemyFindPlayer;
    public bool toReturn;

    public GameObject RoomManager;
    RoomManager rms;
    public GameObject BreakerManager;
    BreakerManager bms;

    public GameObject clearPanel;

    public GameObject timeText;
    Text ttt;
    public GameObject secText;
    Text stt;

    public GameObject itemPanel;
    public GameObject itemText;
    Text itt;
    public GameObject itemsCountText;
    TextMeshProUGUI ictt;

    public GameObject GameOverPanel;
    public float gopAlpha;
    public GameObject GameOverText;
    Text got;
    float gotAlpha;

    public GameObject StartText;
    float startAlpha = 1;

    public float timeLimit;
    float time;
    float time_clear, time_enemyFindPlayer;

    public CanvasGroup GameUI;

    public int gain_items;
    public int total_items;

    public GameObject[] enemies;
    public int[] enemy_put_sec = new int[5];
    float enemy_time;
    public int enemies_count;
    public int enemies_count_max;


    void Start()
    {
        Application.targetFrameRate = 60;

        InitVariables();
        time = timeLimit;
        clearPanel.SetActive(false);
    }

    void InitVariables(){   // 変数の初期化
        rms = RoomManager.GetComponent<RoomManager>();
        bms = BreakerManager.GetComponent<BreakerManager>();

        stt = secText.GetComponent<Text>();
        ttt = timeText.GetComponent<Text>();
        itt = itemText.GetComponent<Text>();
        ictt = itemsCountText.GetComponent<TextMeshProUGUI>();
        got = GameOverText.GetComponent<Text>();

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
        if(enemies_count < enemy_put_sec.Length){
            enemy_time = timeLimit - time;
            if(enemy_time > enemy_put_sec[enemies_count]){
                enemies_count++;
                rms.SetEnemy();
            }
        }

        // if(isStart) time -= Time.deltaTime;

        if(GameIsStop()){
            time_enemyFindPlayer += Time.deltaTime;
            if(!isGameOver && time_enemyFindPlayer - time > 1){
                GameOver();
            }
        }
        else{
            time -= Time.deltaTime;
            if(GameUI.alpha < 1){
                GameUI.alpha += 0.02f;
                startAlpha -= 0.02f;
                StartText.GetComponent<Text>().color = new Color(1, 1, 1, startAlpha);
            }

            ictt.text = "<size=50>" + gain_items + "</size> /" + total_items;
            if(time < 20) ttt.color = new Color(1, 0, 0);
            if(time < 5) gopAlpha = 1 - time / 5;
            if(time <= 0) GameOver();
            stt.text = time.ToString("N1") + "sec";
        }

        if(isStart){
            if(!toReturn && gain_items == total_items) SwitchReturn();
        }
        DisplayGameOverPanel();

        if(Input.GetKeyDown(KeyCode.R)) Reset();
        if(Input.GetKeyDown(KeyCode.P)) isPause = !isPause;
    }

    public void GameStart(){
        timeLimit = Mathf.FloorToInt((rms.rooms_num_x * rms.rooms_num_z * 3 + total_items * 10) / 10) * 10;
        time = timeLimit;
        isStart = true;
    }

    void SwitchReturn(){    // 行き→戻りの転換
        toReturn = true;
        itemsCountText.SetActive(false);
        itt.text = "緑の光に\n向かえ!";
        itt.color = new Color(0, 0.6f, 0);
        itt.fontSize = 30;
    }

    void DisplayGameOverPanel(){    // gop,gotAlphaに合わせてGameOverPanelを表示
        GameOverPanel.GetComponent<Image>().color = new Color(0, 0, 0, gopAlpha);
        if(isGameOver){
            gotAlpha += 0.02f;
            got.color = new Color(1, 1, 1, gotAlpha);
        }
    }

    public void SetClear(){ // クリアタイムの決定、ClearUIを変更
        Debug.Log("Clear()");
        ttt.text = "タイム";
        ttt.color = new Color(0, 0, 0);
        time_clear = timeLimit - time;
        stt.text = time_clear.ToString("N1") + "sec";
        stt.color = new Color(0, 0, 0);
        itemPanel.SetActive(false);
        itemText.SetActive(false);
        clearPanel.SetActive(true);
        GameUI.alpha = 0;
        isClear = true;
        gopAlpha = 0;
    }

    public void GameOver(){
        if(isClear) return;
        Debug.Log("GameOver()");
        isGameOver = true;
        gopAlpha = 1;
        GameUI.alpha = 0;
    }

    public void Reset(){ 
        rms.Reset();
        bms.Reset();

        ResetGmsVariables();
        ResetUIs();
    }

    void ResetGmsVariables(){    // gmsの変数周りのリセット
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
        gotAlpha = 0;
        got.color = new Color(1, 1, 1, gotAlpha);
        gopAlpha = 0;
        GameUI.alpha = 0;
        
        itemPanel.SetActive(true);
        itemText.SetActive(true);
        itemsCountText.SetActive(true);
        itt.text = "アイテム";
        itt.color = new Color(0, 0, 0);
        itt.fontSize = 14;

        ttt.text = "残り:";
        ttt.color = new Color(0, 0, 0);
        clearPanel.SetActive(false);
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
}