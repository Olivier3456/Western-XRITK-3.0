using UnityEngine;

public class GunBulletChamber : MonoBehaviour
{
    private GunBullet gunBullet = null;

    public GunBullet GunBullet
    {
        get => gunBullet;
    }

    public void AddBullet(GunBullet gunBullet)
    {
        if (this.gunBullet != null)
        {
            Debug.LogError($"Can't add a bullet in gun bullet chamber {gameObject.name}: there is already a bullet in it!");
            return;
        }

        this.gunBullet = gunBullet;

        Debug.Log("Gun Bullet Chamber: bullet addded.");
    }

    public void RemoveBullet(GunBullet gunBullet)
    {
        if (gunBullet == null)
        {
            Debug.LogWarning($"Can't remove a bullet from gun bullet chamber {gameObject.name}: it is already empty.");
            return;
        }

        gunBullet = null;
        Debug.Log($"Gun Bullet Chamber: bullet removed from gun bullet chamber {gameObject.name}.");
    }
}