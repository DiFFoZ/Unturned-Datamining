namespace SDG.Unturned;

/// <summary>
/// Currently used by plugins to identify what damaged a buildable.
/// </summary>
public enum EDamageOrigin
{
    Unknown,
    Mega_Zombie_Boulder,
    Vehicle_Bumper,
    Horde_Beacon_Self_Destruct,
    Trap_Wear_And_Tear,
    Carepackage_Timeout,
    Plant_Harvested,
    Charge_Self_Destruct,
    Zombie_Swipe,
    Grenade_Explosion,
    Rocket_Explosion,
    Food_Explosion,
    Vehicle_Explosion,
    Charge_Explosion,
    Trap_Explosion,
    Bullet_Explosion,
    Radioactive_Zombie_Explosion,
    Flamable_Zombie_Explosion,
    Zombie_Electric_Shock,
    Zombie_Stomp,
    Zombie_Fire_Breath,
    Sentry,
    Useable_Gun,
    Useable_Melee,
    Punch,
    Animal_Attack,
    Kill_Volume,
    Vehicle_Collision_Self_Damage,
    Lightning,
    VehicleDecay,
    /// <summary>
    /// Explosion instigated by <see cref="T:SDG.Unturned.ExplosionSpawner" />.
    /// </summary>
    ExplosionSpawnerComponent
}
