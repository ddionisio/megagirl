using UnityEngine;
using System.Collections;

public class Damage : MonoBehaviour {
    public const string DamageMessage = "OnDamage";

    public enum Type {
        Contact, //body contact
        Energy,
        Fire,
        Lightning,
        Explosion,
        Shadow,

        NumType
    }

    public float amount;
    public Type type = Type.Energy;
    public bool stun = false;

    public bool CallDamageTo(Stats stat, Vector3 hitPos, Vector3 hitNorm) {
        return stat.ApplyDamage(this, hitPos, hitNorm);
    }

    public bool CallDamageTo(GameObject target, Vector3 hitPos, Vector3 hitNorm) {
        //target.SendMessage(DamageMessage, this, SendMessageOptions.DontRequireReceiver);
        Stats stat = target.GetComponent<Stats>();
        if(stat) {
            return CallDamageTo(stat, hitPos, hitNorm);
        }

        return false;
    }
}
