
public enum EntityState {
    Invalid = -1,
    Normal,
    Hurt,
    Dead,
    Stun,

    //for enemies
    BossEntry, //boss enters
    RespawnWait,

    // specific for player
    Lock,
    Victory
}