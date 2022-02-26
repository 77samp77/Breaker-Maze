using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public GameObject Camera;
    public GameObject RoomPrefab;
    public GameObject WallPrefab;
    public GameObject GoalFloorPrefab;
    public GameObject LadderPrefab;
    public GameObject PlayerPrefab;
    public GameObject BreakerPrefab;
    public GameObject ItemPrefab;
    public GameObject EnemyPrefab;

    public GameObject GameManager;
    GameManagerScript gms;

    public GameObject BreakerManager;
    BreakerManager bms;

    GameObject player;
    GameObject breaker;

    public int rooms_num_x;
    public int rooms_num_z;
    public GameObject[,] rooms;
    int fixLights_count, fixLights_max;

    public List<int> bright_rooms = new List<int>(); // x*z + x;
    List<int> canPutEnemy_rooms = new List<int>();

    int walls_ver_num_x, walls_ver_num_z;   // 縦の壁の数(xとz)
    int walls_hor_num_x, walls_hor_num_z;   // 横の壁の数(xとz)
    public bool[,] isWalls_ver; // 縦の壁ある
    public bool[,] isWalls_hor; // 横の壁ある
    public List<GameObject> walls = new List<GameObject>();    // 実際の壁のリスト

    int[,] walls_seed;  // 生成時の起点
    // 0,1,2,3... 下上左右


    public int start_x, start_z;
    public int goal_x = 0, goal_z = 0;
    int breaker_x, breaker_z;
    int[] item_x;
    int[] item_z;

    Vector3 f_scl;  // 部屋1つのlocalScale

    GameObject Player;
    PlayerController pcs;

    GameObject[] items;

    void Start()
    {
        InitVariables();
        Init_Set();
    }

    void InitVariables(){   // 変数の初期化
        gms = GameManager.GetComponent<GameManagerScript>();
        bms = BreakerManager.GetComponent<BreakerManager>();
        f_scl = RoomPrefab.transform.Find("Floor").transform.localScale;
    }

    void Update()
    {
        if(gms.isStart && Player == null){
            Player = GameObject.Find("Player"); 
            pcs = Player.GetComponent<PlayerController>();
        }
    }

    void InstRoom(int x, int z){    // 各部屋の初期化
        GameObject room =
            Instantiate(RoomPrefab, new Vector3(x * 20, 0, z * 20), Quaternion.identity);
            room.name = RoomPrefab.name;
        rooms[x, z] = room;

        if(x == start_x && z == start_z) SetStart(room);
        if(x == breaker_x && z == breaker_z) SetBreaker(room);
        if(x == goal_x && z == goal_z) SetGoal(room);

        int iir = ItemInRoom(x, z);
        if(iir != -1) SetItem(iir, room);
    }

    public void SetEnemy(){ // 敵の出現位置決め、PutEnemy()で設置
        int fex = Random.Range(0, rooms_num_x);
        int fez = Random.Range(0, rooms_num_z);
        while(CalDistSquare(fex, fez, pcs.rx, pcs.rz) < 3 || CheckLightInRoom(fex, fez)){
            if(fex >= rooms_num_x - 1){
                fex = 0;
                if(fez >= rooms_num_z - 1) fez = 0;
                else fez++;
            }
            else fex++;
        }
        PutEnemy(fex, fez);
    }

    void PutEnemy(int fex, int fez){    // 敵の設置
        GameObject enemy = Instantiate(EnemyPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            enemy.name = EnemyPrefab.name;
        EnemyController ecs = enemy.GetComponent<EnemyController>();
            ecs.rx = fex;
            ecs.nrx = fex;
            ecs.rz = fez;
            ecs.nrz = fez;
        Vector3 e_scl = enemy.transform.localScale;
        GameObject room = rooms[fex, fez];
        Vector3 r_pos = room.transform.position;
        enemy.transform.position = new Vector3(r_pos.x, r_pos.y + e_scl.y, r_pos.z);
        gms.enemies[gms.enemies_count - 1] = enemy; 
        gms.charas.Add(enemy);
    }

    int ItemInRoom(int x, int z){   // 指定した部屋にアイテムがあった場合、その番号を返す
        for(int i = 0; i < gms.total_items; i++){
            if(x == item_x[i] && z == item_z[i]) return i;
        }
        return -1;
    }

    void InstWallsArrays(){ // 壁関連の配列の初期化
        walls_ver_num_x = rooms_num_x + 1;
        walls_ver_num_z = rooms_num_z;
        isWalls_ver = new bool[walls_ver_num_x, walls_ver_num_z];
        walls_hor_num_x = rooms_num_x;
        walls_hor_num_z = rooms_num_z + 1;
        isWalls_hor = new bool[walls_hor_num_x, walls_hor_num_z];
        
        walls_seed = new int[rooms_num_x - 1, rooms_num_z - 1];
    }

    void GenerateMaze(){    // 迷路のランダム生成
        for(int x = 0; x < walls_seed.GetLength(0); x++){
            walls_seed[x, 0] = Random.Range(0, 4);
            while(WallIsOverlap(x, 0, walls_seed[x, 0])){
                walls_seed[x, 0] = Random.Range(0, 4);
            }
            setIsWallInMaze(x, 0, walls_seed[x, 0]);
        }

        for(int z = 1; z < walls_seed.GetLength(1); z++){
            for(int x = 0; x < walls_seed.GetLength(0); x++){
                walls_seed[x, z] = Random.Range(1, 4);
                while(WallIsOverlap(x, z, walls_seed[x, z])){
                    walls_seed[x, z] = Random.Range(1, 4);
                }
                setIsWallInMaze(x, z, walls_seed[x, z]);
            }
        }

        for(int x = 0; x < rooms_num_x; x++){
            isWalls_hor[x, 0] = true;
            isWalls_hor[x, rooms_num_z] = true;
        }
        for(int z = 0; z < rooms_num_z; z++){
            isWalls_ver[0, z] = true;
            isWalls_ver[rooms_num_x, z] = true;
        }
    }

    int ins_d, ins_sd;    // 0123…上右下左
    int ins_rx, ins_rz; // 点検中の部屋の相対座標
    int ins_srx, ins_srz;   // 最初の部屋の相対座標
    bool[,] roomIsInspected;    // 部屋が点検済か
    bool[,] roomIsLinked;   // 部屋がスタート地点とつながっているか
    bool routeIsLinked; // 点検中のルートがスタート地点とつながっているか
    List<int> inspectingRooms = new List<int>();  // 点検中の部屋番号(z * max(x) + x)
    int route_count;    // 点検ルートの本数

    void InspectMaze(){ // 点検全体
        roomIsInspected = new bool[rooms_num_x, rooms_num_z];
        roomIsLinked = new bool[rooms_num_x, rooms_num_z];
        roomIsLinked[start_x, start_z] = true;
        
        route_count = 0;
        if(decideStartDirect(start_x, start_z)){
            InspectRoute();
            route_count++;
        }
        while(RoomIsUninspected() != -1 && route_count < 100){
            int temp = RoomIsUninspected();
            int temp_sx = temp % rooms_num_x;
            int temp_sz = Mathf.FloorToInt(temp / rooms_num_x);
            if(decideStartDirect(temp_sx, temp_sz)){
                InspectRoute();
            }
            else{
                inspectingRooms.Add(temp);
                CommitInspectedRooms();
            }
            route_count++;
            if(route_count == 99) Debug.Log("InspectMaze()");
        }
    }

    void InspectRoute(){    // 始点に戻るまで一部屋ずつ点検
        InspectRoom(ins_d, ins_rx, ins_rz);
        int temp_count = 0;
        while(!IsStartedState(ins_d, ins_rx, ins_rz) && !temp_isFinish && temp_count < 1000){
            InspectRoom(ins_d, ins_rx, ins_rz);
            temp_count++;
            if(temp_count == 999){
                Debug.Log("InspectRoute()  sd = " + ins_sd);
            }
        }
        CommitInspectedRooms();
        temp_isFinish = false;
    }

    bool temp_isFinish;
    void InspectRoom(int d, int rx, int rz){    // 1部屋の点検、進行方向の変更
        if(roomIsLinked[rx, rz]) routeIsLinked = true;
        inspectingRooms.Add(rz * rooms_num_x + rx);
        int left = d - 1;
        if(left == -1) left = 3;
        if(IsStartedState(left, rx, rz)) temp_isFinish = true;
        if(!ThereIsWall(left, rx, rz)){
            if(d == 0 && d == ins_sd && rx == ins_srx && rz == ins_srz){
                ins_sd = left;
                if(!ThereIsWall(left - 1, rx, rz)) ins_sd--;
            }
            GoForward(left, rx, rz);
        }
        else{
            int temp_count = 0;
            while(ThereIsWall(d, rx, rz) && temp_count < 5){
                d = (d + 1) % 4;
                if(IsStartedState(d, rx, rz)){
                    temp_isFinish = true;
                    ins_d = d;
                    return;
                }
                temp_count++;
                if(temp_count == 4) Debug.Log("InspectRoom()");
            }
            GoForward(d, rx, rz);
        }
    }

    void GoForward(int d, int rx, int rz){  // 方向dに対し前進
        switch(d){
            case 0:
                rz++;
                break;
            case 1:
                rx++;
                break;
            case 2:
                rz--;
                break;
            case 3:
                rx--;
                break;
        }
        ins_d = d;
        ins_rx = rx;
        ins_rz = rz;
    }

    bool decideStartDirect(int srx, int srz){   // 始点の周辺の壁をもとに、最初の進行方向を決定
        for(int d = 0; d < 4; d++){
            if(!ThereIsWall(d, srx, srz)){
                SetFirstState(d, srx, srz);
                return true;
            }
        }
        return false;
    }

    bool ThereIsWall(int d, int rx, int rz){    // 方向dに壁があるか否か
        switch(d){
            case 0:
                if(isWalls_hor[rx, rz + 1]) return true;
                break;
            case 1:
                if(isWalls_ver[rx + 1, rz]) return true;
                break;
            case 2:
                if(isWalls_hor[rx, rz]) return true;
                break;
            case 3:
                if(isWalls_ver[rx, rz]) return true;
                break;
        }
        return false;
    }

    void SetFirstState(int d, int rx, int rz){  // 点検開始時の位置、方向を記録
        ins_sd = d;
        ins_srx = rx;
        ins_srz = rz;
        SetInspectState(d, rx, rz);
    }

    void SetInspectState(int d, int rx, int rz){    // 現状の位置、方向を記録
        ins_d = d;
        ins_rx = rx;
        ins_rz = rz;
    }

    bool IsStartedState(int d, int rx, int rz){ // 現状が点検開始時と同じか否か
        if(d != ins_sd) return false;
        if(rx != ins_srx) return false;
        if(rz != ins_srz) return false;
        return true;
    }

    void CommitInspectedRooms(){    // 点検済の部屋を記録
        int firstCommitRoom = 0;
        if(route_count == 0 && inspectingRooms.Count < 9){
            int del_w_rx = inspectingRooms[0] % rooms_num_x;
            int del_w_rz = Mathf.FloorToInt(inspectingRooms[0] / rooms_num_x);
            DeleteOneIsWall(del_w_rx, del_w_rz);
            routeIsLinked = true;
            roomIsLinked[del_w_rx, del_w_rz] = true;
            firstCommitRoom = 1;
        }

        for(int i = firstCommitRoom; i < inspectingRooms.Count; i++){
            int temp_rx = inspectingRooms[i] % rooms_num_x;
            int temp_rz = Mathf.FloorToInt(inspectingRooms[i] / rooms_num_x);
            roomIsInspected[temp_rx, temp_rz] = true;
            if(routeIsLinked){
                roomIsLinked[temp_rx, temp_rz] = true;
            }
            else{
                DeleteOneIsWall(temp_rx, temp_rz);
                routeIsLinked = true;
                roomIsLinked[temp_rx, temp_rz] = true;
            }
        }
        routeIsLinked = false;
        inspectingRooms.Clear();
    }

    void DeleteOneIsWall(int rx, int rz){   // 任意の部屋[rx, rz]の壁をひとつ削除
        for(int d = 0; d < 4; d++){
            switch(d){
                case 0:
                    if(CanDeleteTheWall(isWalls_hor, rx, rz + 1)){
                        isWalls_hor[rx, rz + 1] = false;
                        return;
                    }
                    break;
                case 1:
                    if(CanDeleteTheWall(isWalls_ver, rx + 1, rz)){
                        isWalls_ver[rx + 1, rz] = false;
                        return;
                    }
                    break;
                case 2:
                    if(CanDeleteTheWall(isWalls_hor, rx, rz)){
                        isWalls_hor[rx, rz] = false;
                        return;
                    }
                    break;
                case 3:
                    if(CanDeleteTheWall(isWalls_ver, rx, rz)){
                        isWalls_ver[rx, rz] = false;
                        return;
                    }
                    break;
            }
        }
    }

    bool CanDeleteTheWall(bool[,] isWallsArray, int wx, int wz){    // 壁消せるかどうか
        if(!isWallsArray[wx, wz]) return false;
        if(isWallsArray == isWalls_ver && (wx == 0 || wx == walls_ver_num_x - 1)) return false;
        if(isWallsArray == isWalls_hor && (wz == 0 || wz == walls_hor_num_z - 1)) return false;
        return true;
    }

    int RoomIsUninspected(){    // 未点検の部屋があった場合、その部屋番号を返す
        for(int x = 0; x < rooms_num_x; x++){
            for(int z = 0; z < rooms_num_z; z++){
                if(!roomIsInspected[x, z]){
                    return z * rooms_num_x + x;
                }
            }
        }
        return -1;
    }


    bool WallIsOverlap(int x, int z, int seedValue){    // (迷路生成過程)壁のかぶり判定
        if(seedValue == 0){
            if(isWalls_ver[x + 1, z + 1]) return true;
            return false;
        }
        else if(seedValue == 1){
            if(isWalls_ver[x + 1, z]) return true;
            return false;
        }
        else if(seedValue == 2){
            if(isWalls_hor[x, z + 1]) return true;
            return false;
        }
        else if(seedValue == 3){
            if(isWalls_hor[x + 1, z + 1]) return true;
            return false;
        }
        return false;
    }

    void setIsWallInMaze(int x, int z, int seedValue){  // 生成した迷路に沿って壁配列を更新
        if(seedValue == 0){
            isWalls_ver[x + 1, z + 1] = true;
        }
        else if(seedValue == 1){
            isWalls_ver[x + 1, z] = true;
        }
        else if(seedValue == 2){
            isWalls_hor[x, z + 1] = true;
        }
        else if(seedValue == 3){
            isWalls_hor[x + 1, z + 1] = true;
        }
    }

    void InstWalls(){   // 壁配列をもとにすべての壁を生成
        for(int x = 0; x < walls_ver_num_x; x++){
            for(int z = 0; z < walls_ver_num_z; z++){
                if(isWalls_ver[x, z]){
                    int fwz = z;
                    int contWallsCount = 1;
                    z++;
                    while(z < walls_ver_num_z && isWalls_ver[x, z]){
                        contWallsCount++;
                        z++;
                    }
                    InstWall(true, x, fwz, contWallsCount);
                }
            }
        }

        for(int z = 0; z < walls_hor_num_z; z++){
            for(int x = 0; x < walls_hor_num_x; x++){
                if(isWalls_hor[x, z]){
                    int fwx = x;
                    int contWallsCount = 1;
                    x++;
                    while(x < walls_hor_num_x && isWalls_hor[x, z]){
                        contWallsCount++;
                        x++;
                    }
                    InstWall(false, fwx, z, contWallsCount);
                }
            }
        }
    }

    void InstWall(bool isVer, int fwx, int fwz, int count){ // 壁生成
        GameObject wall = 
            Instantiate(WallPrefab, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
        wall.name = WallPrefab.name;
        walls.Add(wall);
        WallManager wms = wall.GetComponent<WallManager>();
        wms.wx = fwx;
        wms.wz = fwz;
        wms.len = count;
        wms.isVer = isVer;

        Vector3 w_scl = wall.transform.localScale;
        if((isVer && (fwx == 0 || fwx == walls_ver_num_x - 1)) || !isVer && (fwz == walls_hor_num_z - 1)){
            w_scl.y = 30;
        }
        wall.transform.localScale = new Vector3(w_scl.x, w_scl.y, w_scl.z);
        Vector3 w_pos = wall.transform.position;
        BoxCollider w_col = wall.GetComponent<BoxCollider>();
        w_col.size = new Vector3(w_col.size.x * count, w_col.size.y, w_col.size.z);

        if(isVer){
            w_col.center = new Vector3(-(count - 1) / 2.0f, w_col.center.y, w_col.center.z);
            wall.transform.rotation = Quaternion.Euler(0, 90, 0);
            wall.transform.position = new Vector3(w_scl.x * (fwx - 0.5f), w_scl.y / 2 - 1, w_scl.x * fwz);
        }
        else{
            w_col.center = new Vector3((count - 1) / 2.0f, w_col.center.y, w_col.center.z);
            wall.transform.position = new Vector3(w_scl.x * fwx, w_scl.y / 2 - 1, w_scl.x * (fwz - 0.5f));
        }
    }

    void SetStart(GameObject room){ // スタート地点の設定
        player = Instantiate(PlayerPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            player.name = PlayerPrefab.name;
        Vector3 r_pos = room.transform.position;
        Vector3 pl_scl = player.transform.localScale;
        player.transform.position = new Vector3(r_pos.x, r_pos.y + pl_scl.y/2, r_pos.z);
        gms.charas.Add(player);

        GameObject p_light = room.transform.Find("Light").gameObject;
        LightManager p_lms = p_light.GetComponent<LightManager>();
        Light p_ll = p_light.GetComponent<Light>();
        p_light.SetActive(true);
        p_lms.isFix = true;
        p_ll.color = new Color32(255, 255, 200, 1);
        fixLights_count++;
    }

    void SetBreaker(GameObject breakerRoom){  // ブレーカーがある部屋の設定
        breaker = Instantiate(BreakerPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        breaker.name = BreakerPrefab.name;

        Vector3 br_pos = breakerRoom.transform.position;
        Vector3 b_pos = breaker.transform.position;
        Vector3 b_scl = breaker.transform.localScale;
        b_pos.x = br_pos.x;
        b_pos.y = br_pos.y + b_scl.y;
        b_pos.z = br_pos.z + f_scl.z / 2 - b_scl.x;
        breaker.transform.position = new Vector3(b_pos.x, b_pos.y, b_pos.z);

        Vector3 b_rot = breaker.transform.localEulerAngles;
        b_rot.y = 90;
        breaker.transform.localEulerAngles = new Vector3(b_rot.x, b_rot.y, b_rot.z);

        GameObject br_light = breakerRoom.transform.Find("Light").gameObject;
        LightManager br_lms = br_light.GetComponent<LightManager>();
        Light br_ll = br_light.GetComponent<Light>();
        br_light.SetActive(true);
        br_lms.isFix = true;
        if(!(breaker_x == start_x && breaker_z == start_z)
            && !(breaker_x == goal_x && breaker_z == goal_z)){
            br_ll.color = new Color32(255, 255, 200, 1);
            fixLights_count++;
        }
    }

    int FirstWallInRoom(int rx, int rz){    // 0123...下上左右
        if(isWalls_hor[rx, rz + 1]) return 1;
        if(isWalls_ver[rx, rz]) return 2;
        if(isWalls_ver[rx + 1, rz]) return 3;
        if(isWalls_hor[rx, rz]) return 0;
        return -1;
    }

    void SetGoal(GameObject room){  // ゴール地点の設定
        GameObject p_floor = room.transform.Find("Floor").gameObject;
        Vector3 pf_pos = p_floor.transform.position;
        Vector3 pf_scl = p_floor.transform.localScale;
        GameObject g_floor = Instantiate(GoalFloorPrefab, new Vector3(pf_pos.x, -0.5f, pf_pos.z), Quaternion.Euler(90, 90, 0));
        Destroy(p_floor);
        g_floor.name = GoalFloorPrefab.name;
        g_floor.transform.SetParent(room.transform);

        GameObject ladder = Instantiate(LadderPrefab, new Vector3(0, 0, 0), Quaternion.Euler(0, 180, 0));
        Vector3 lad_scl = ladder.transform.localScale;
        Vector3 lad_pos = new Vector3(pf_pos.x + pf_scl.x * 0.2f,  lad_scl.y / 2, pf_pos.z + pf_scl.z * 0.3f);
        ladder.transform.position = lad_pos;
        ladder.name = LadderPrefab.name;
        ladder.transform.SetParent(room.transform);

        GameObject p_light = room.transform.Find("Light").gameObject;
        LightManager p_lms = p_light.GetComponent<LightManager>();
        Light p_ll = p_light.GetComponent<Light>();
        p_light.SetActive(true);
        p_lms.isFix = true;
        p_ll.intensity = 5;
        p_ll.color = new Color32(200, 255, 200, 1);
        fixLights_count++;
    }

    void FixRandomLights(){ // ランダムにいくつかの部屋のライト点ける
        for(int i = fixLights_count; i < fixLights_max; i++){
            int fl_rx = Random.Range(0, rooms_num_x);
            int fl_rz = Random.Range(0, rooms_num_z);
            while(CheckLightInRoom(fl_rx, fl_rz)){
                fl_rx = Random.Range(0, rooms_num_x);
                fl_rz = Random.Range(0, rooms_num_z);
            }
            GameObject fl_light = rooms[fl_rx, fl_rz].transform.Find("Light").gameObject;
            LightManager fl_lms = fl_light.GetComponent<LightManager>();
            Light fl_ll = fl_light.GetComponent<Light>();
            fl_light.SetActive(true);
            fl_lms.isFix = true;
            fl_ll.color = new Color32(255, 255, 200, 1);
            fixLights_count++;
        }
    }

    void SetAnItemPosition(int num, int sx, int sz){  // アイテム1つずつを置く場所の決定
        int ix = Random.Range(0, rooms_num_x);
        int iz = Random.Range(0, rooms_num_z);
        while((CalDistSquare(sx, sz, ix, iz) < 4 || OverlapItemPos(num, ix, iz))){
            if(ix == rooms_num_x - 1){
                ix = 0;
                if(iz == rooms_num_z - 1) iz = 0;
                else iz++;
            }
            else ix++;
        }
        item_x[num] = ix;
        item_z[num] = iz;
    }

    bool OverlapItemPos(int num, int ix, int iz){   // アイテム位置のかぶり判定
        for(int i = 0; i < num; i++){
            if(item_x[i] == ix && item_z[i] == iz) return true;
        }
        return false;
    }

    public int CalDistSquare(int x1, int z1, int x2, int z2){  // 2者の距離
        int dx = Mathf.Abs(x1 - x2);
        int dz = Mathf.Abs(z1 - z2);
        return dx + dz;
    }

    void SetItem(int num, GameObject room){ // アイテム設置
        GameObject item = 
            Instantiate(ItemPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            item.name = ItemPrefab.name;
        ItemController ics = item.GetComponent<ItemController>();
            ics.rx = item_x[num];
            ics.nrx = item_x[num];
            ics.rz = item_z[num];
            ics.nrz = item_z[num];
        Vector3 i_scl = item.transform.localScale;
        Vector3 r_pos = room.transform.position;
        item.transform.position = 
            new Vector3(r_pos.x, r_pos.y + i_scl.y, r_pos.z);
            
        items[num] = item;
        gms.charas.Add(item);
    }

    public bool CheckLightInRoom(int rx, int rz){   // 部屋の電気がついているか
        GameObject room = rooms[rx, rz];
        GameObject r_light = room.transform.Find("Light").gameObject;
        return r_light.activeSelf;
    }

    public void Reset(){
        Init_Destroy();
        Init_Set();
    }

    void Init_Destroy(){    // (Reset時)初期化の削除関連
        Destroy(player);
        Destroy(breaker);

        for(int x = 0; x < rooms_num_x; x++){
            for(int z = 0; z < rooms_num_z; z++){
                Destroy(rooms[x, z]);
            }
        }
        fixLights_count = 0;

        for(int i = 0; i < walls.Count; i++) Destroy(walls[i]);
        walls.Clear();

        for(int i = 0; i < gms.total_items; i++) Destroy(items[i]);
    }

    int rooms_num;
    void Init_Set(){    // 初期化の生成関連
        SetStartPosition();
        
        rooms_num = rooms_num_x * rooms_num_z;
        SetItemsPosition();
        SetLightVals();
        
        InstWallsArrays();
        GenerateMaze();
        InspectMaze();

        SetBreakerPosition();
        SetGoalPosition();

        rooms = new GameObject[rooms_num_x, rooms_num_z];
        for(int x = 0; x < rooms_num_x; x++){
            for(int z = 0; z < rooms_num_z; z++){
                InstRoom(x, z);
            }
        }
        InstWalls();
        FixRandomLights();
        
        Camera.transform.localPosition = new Vector3(rooms_num_x * 10 - 10, rooms_num_z * 20, -5);
    }

    void SetStartPosition(){
        if(gms.isClear){
            start_x = goal_x;
            start_z = goal_z;
        }
        else{
            start_x = Random.Range(0, rooms_num_x);
            start_z = Random.Range(0, rooms_num_z);
        }
    }

    void SetItemsPosition(){
        gms.total_items = Mathf.FloorToInt(rooms_num / 6);
        item_x = new int[gms.total_items];
        item_z = new int[gms.total_items];
        for(int i = 0; i < gms.total_items; i++){
            SetAnItemPosition(i, start_x, start_z);
        }
        items = new GameObject[gms.total_items];
    }

    void SetLightVals(){
        fixLights_max = Mathf.FloorToInt(Mathf.Sqrt(rooms_num_x * rooms_num_z)) - 1;
        bms.limit = Mathf.FloorToInt(rooms_num / 3) - (fixLights_max - 2);
    }

    void SetBreakerPosition(){  // ブレーカーの位置決め
        breaker_x = start_x;
        breaker_z = start_z;
        while(!isWalls_hor[breaker_x, breaker_z + 1]) breaker_z++;
    }

    void SetGoalPosition(){ // ゴール地点の位置決め
        goal_x = Random.Range(0, rooms_num_x);
        goal_z = Random.Range(0, rooms_num_z);
        while((goal_x == start_x && goal_z == start_z)
                || (goal_x == breaker_x && goal_z == breaker_z)
                || !isWalls_hor[goal_x, goal_z + 1]){
            goal_x = Random.Range(0, rooms_num_x);
            goal_z = Random.Range(0, rooms_num_z);
        }
    }
}