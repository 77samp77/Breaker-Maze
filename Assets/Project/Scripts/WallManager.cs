using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallManager : MonoBehaviour
{
    GameObject rm;
    RoomManager rms;

    public GameObject WallChildPrefab;
    public int wx, wz, len;
    public bool isVer;
    bool isSetLength, isChild;

    GameObject player;
    PlayerController pcs;

    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.Find("Player");
        pcs = player.GetComponent<PlayerController>();
        rm = GameObject.Find("RoomManager");
        rms = rm.GetComponent<RoomManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if(!isChild && !isSetLength) Init_ChildWalls();
    }

    void Init_ChildWalls(){ // 壁の長さが複数部屋分ある場合、ひとつひとつ分割
        for(int i = 1; i < len; i++){
            GameObject wall_child = 
                Instantiate(WallChildPrefab, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
            wall_child.transform.parent = this.transform;
            rms.walls.Add(wall_child);
                
            WallManager wcms = wall_child.GetComponent<WallManager>();
            wcms.isVer = isVer;
            wcms.isChild = true;

            Vector3 wall_scl = this.transform.localScale;
            wall_child.transform.localScale = new Vector3(1, 1, 1);

            Vector3 wall_pos = this.transform.position;
            if(isVer){
                wcms.wx = wx;
                wcms.wz = wz + i;
                wall_child.transform.rotation = Quaternion.Euler(0, 90, 0);
                float wc_pre_pos = wall_pos.z + i * wall_scl.x;
                wall_child.transform.position = new Vector3(wall_pos.x, wall_pos.y, wc_pre_pos);
            }
            else{
                wcms.wx = wx + i;
                wcms.wz = wz;
                float wc_pre_pos = wall_pos.x + i * wall_scl.x;
                wall_child.transform.position = new Vector3(wc_pre_pos, wall_pos.y, wall_pos.z);
            }
        }

        isSetLength = true;
    }
}
