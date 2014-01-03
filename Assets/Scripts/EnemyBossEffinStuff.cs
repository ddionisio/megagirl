using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
public class EnemyBossEffinStuff : MonoBehaviour {
    [System.Serializable]
    public class Data {
        public RigidBodyMoveToTarget mover;
        public float startOfsY;
        public float originOfsY;
        public float expandOfsY;
    }

    public Transform holder;

    public Data[] data;

	public bool fillData;

    public bool applyStartOfs;
    public bool applyOriginOfs;
    public bool applyExpandOfs;

    public bool setStartOfs;
    public bool setOriginOfs;
    public bool setExpandOfs;

    public bool addOriginOfs;

    public void Shuffle() {
        M8.ArrayUtil.Shuffle(data);
    }

    int MoverCompare(RigidBodyMoveToTarget x, RigidBodyMoveToTarget y) {
        return x.name.CompareTo(y.name);
    }
	
#if UNITY_EDITOR
	// Update is called once per frame
	void Update () {
        if(!Application.isPlaying) {
            if(fillData) {
                if(data == null || data.Length == 0) {
                    RigidBodyMoveToTarget[] movers = holder.GetComponentsInChildren<RigidBodyMoveToTarget>();
                    System.Array.Sort(movers, MoverCompare);

                    data = new Data[movers.Length];

                    for(int i = 0; i < movers.Length; i++) {
                        data[i] = new Data();
                        data[i].mover = movers[i];
                        data[i].originOfsY = movers[i].offset.y;
                        data[i].expandOfsY = movers[i].offset.y;
                        data[i].startOfsY = movers[i].offset.y;
                    }
                }
                else {
                    List<Data> newStuff = new List<Data>(data.Length);
                    for(int i = 0; i < data.Length; i++) {
                        if(data[i].mover.gameObject.activeSelf)
                            newStuff.Add(data[i]);
                    }
                    data = newStuff.ToArray();
                }

                fillData = false;
            }

            if(applyStartOfs) {
                foreach(Data dat in data) {
                    dat.mover.offset.y = dat.startOfsY;
                }

                applyStartOfs = false;
            }

            if(applyOriginOfs) {
                foreach(Data dat in data) {
                    dat.mover.offset.y = dat.originOfsY;
                }

                applyOriginOfs = false;
            }

            if(applyExpandOfs) {
                foreach(Data dat in data) {
                    dat.mover.offset.y = dat.expandOfsY;
                }

                applyExpandOfs = false;
            }

            if(setStartOfs) {
                foreach(Data dat in data) {
                    dat.startOfsY = dat.mover.offset.y;
                }

                setStartOfs = false;
            }

            if(setOriginOfs) {
                foreach(Data dat in data) {
                    dat.originOfsY = dat.mover.offset.y;
                }

                setOriginOfs = false;
            }

            if(setExpandOfs) {
                foreach(Data dat in data) {
                    dat.expandOfsY = dat.mover.offset.y;
                }

                setExpandOfs = false;
            }

            if(addOriginOfs) {
                foreach(Data dat in data) {
                    dat.mover.offset.y += dat.originOfsY;
                }

                addOriginOfs = false;
            }
        }
	}
#endif
}
