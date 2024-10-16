using UnityEngine;

public class GunBulletChamber : MonoBehaviour
{
    public MeshRenderer BulletPlacementRenderer => bulletPlacementRenderer;
    [SerializeField] private MeshRenderer bulletPlacementRenderer;


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
        this.gunBullet.SetGunBulletChamber(this);

        //Debug.Log("Gun Bullet Chamber: bullet addded.");
    }

    public void RemoveBullet()
    {
        if (gunBullet == null)
        {
            Debug.LogWarning($"Can't remove a bullet from gun bullet chamber {gameObject.name}: it is already empty.");
            return;
        }

        this.gunBullet.SetGunBulletChamber(null);
        this.gunBullet = null;
        Debug.Log($"Gun Bullet Chamber: bullet removed from gun bullet chamber {gameObject.name}.");
    }




    public void ShowBulletPlacementVisual() => bulletPlacementRenderer.enabled = true;
    public void HideBulletPlacementVisual() => bulletPlacementRenderer.enabled = false;
}