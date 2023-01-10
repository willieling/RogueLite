using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class PlayerController : MonoBehaviour
{
    //Ok, there's a mixing of dependencies here but I'm not sure how I want to handle this.
    //The PlayerController (probably needs to be renamed) should not hold a direct reference to 
    //projectiles.  Something else should probably set it when spawning the player.
    //But this is good enough for now to prototype.
    [SerializeField]
    private ProjectileScriptableObject _projectileDataAsset = null;

    private ProjectileData _projectileData;

    private Pool<ProjectileBullet> _projectilePool = null;

    float _shotCooldown = 0;

    // Start is called before the first frame update
    void Awake()
    {
        Assert.IsNotNull<ProjectileScriptableObject>( _projectileDataAsset);

        _projectilePool = new Pool<ProjectileBullet>(_projectileDataAsset.Prefab, new Vector3(0, 0, -100), 10)
            .SetName("Projectile")
            .DisableOnRelease(true);

        _projectileData = _projectileDataAsset.Data;
    }

    // Update is called once per frame
    void Update()
    {
        float horizontal = Input.GetAxis("RightJoystickHorizontal");
        float vertical = Input.GetAxis("RightJoystickVertical");
        if ((horizontal != 0 || vertical != 0)
            && _shotCooldown <= 0)
        {
            ShootProjectile(new Vector2(horizontal, vertical).normalized * _projectileData.Speed);
        }

        _shotCooldown -= Time.deltaTime;
    }

    private void ShootProjectile(Vector2 direction)
    {
        ProjectileBullet bullet = _projectilePool.Get();
        bullet.transform.SetPositionAndRotation(this.transform.position, this.transform.rotation);

        bullet.SetDirection(direction);

        _shotCooldown = _projectileData.Cooldown;
    }
}
