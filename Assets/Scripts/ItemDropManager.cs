using UnityEngine;
using System.Collections;

public class ItemDropManager : MonoBehaviour {
    [System.Serializable]
    public class ItemData {
        public string itemSpawnType;
        public int weight = 1;
    }

    [System.Serializable]
    public class Group {
        public ItemData[] items;
        public int range = 256;

        private int mItemRange;

        public int itemRange { get { return mItemRange; } }

        public void ComputeItemRange() {
            mItemRange = 0;
            for(int i = 0, max = items.Length; i < max; i++) {
                mItemRange += items[i].weight;
            }
        }
    }

    public Group[] groups;

    private static ItemDropManager mInstance;

    private PoolController mCtrl;

    public static ItemDropManager instance { get { return mInstance; } }

    public void DoDrop(int groupIndex, Vector3 pos) {
        Group dat = groups[groupIndex];

        int r = Random.Range(0, dat.range) + 1;

        if(r <= dat.itemRange) {
            string spawnType = null;

            for(int i = 0, max = dat.items.Length, w = 0; i < max; i++) {
                ItemData drop = dat.items[i];
                w += drop.weight;

                if(r <= w) {
                    spawnType = drop.itemSpawnType;
                    break;
                }
            }

            if(!string.IsNullOrEmpty(spawnType)) {
                Debug.Log("dropping: " + spawnType);
                mCtrl.Spawn(spawnType, spawnType, null, pos, Quaternion.identity);
            }
        }
    }

    void OnDestroy() {
        mInstance = null;
    }

    void Awake() {
        mInstance = this;

        mCtrl = GetComponent<PoolController>();

        foreach(Group grp in groups)
            grp.ComputeItemRange();
    }
}
