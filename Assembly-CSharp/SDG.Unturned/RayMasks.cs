using System;

namespace SDG.Unturned;

public class RayMasks
{
    public const int DEFAULT = 1;

    public const int TRANSPARENT_FX = 2;

    public const int IGNORE_RAYCAST = 4;

    public const int WATER = 16;

    public const int UI = 32;

    public const int LOGIC = 256;

    public const int PLAYER = 512;

    public const int ENEMY = 1024;

    public const int VIEWMODEL = 2048;

    public const int DEBRIS = 4096;

    public const int ITEM = 8192;

    public const int RESOURCE = 16384;

    public const int LARGE = 32768;

    public const int MEDIUM = 65536;

    public const int SMALL = 131072;

    public const int SKY = 262144;

    public const int ENVIRONMENT = 524288;

    public const int GROUND = 1048576;

    public const int CLIP = 2097152;

    public const int NAVMESH = 4194304;

    public const int ENTITY = 8388608;

    public const int AGENT = 16777216;

    public const int LADDER = 33554432;

    public const int VEHICLE = 67108864;

    public const int BARRICADE = 134217728;

    public const int STRUCTURE = 268435456;

    public const int TIRE = 536870912;

    public const int TRAP = 1073741824;

    public const int GROUND2 = int.MinValue;

    public const int ALL = -1;

    public static readonly int REFLECTION = 1687552;

    public static readonly int CHART = 1687552;

    public static readonly int FOLIAGE_FOCUS = -2146336768;

    public static readonly int POWER_INTERACT = 134463488;

    public static readonly int BARRICADE_INTERACT = 471449600;

    public static readonly int STRUCTURE_INTERACT = 270123008;

    public static readonly int ROOFS_INTERACT = -1877360640;

    public static readonly int CORNERS_INTERACT = 270385152;

    public static readonly int WALLS_INTERACT = 270123040;

    public static readonly int LADDERS_INTERACT = 337231874;

    public static readonly int SLOTS_INTERACT = 505004288;

    public static readonly int LADDER_INTERACT = 505004032;

    public static readonly int CLOTHING_INTERACT = 9728;

    public static readonly int PLAYER_INTERACT = 505144321;

    public static readonly int EDITOR_INTERACT = 402882560;

    public static readonly int EDITOR_WORLD = -1743536128;

    public static readonly int EDITOR_LOGIC = 262400;

    public static readonly int EDITOR_VR = -2146320384;

    public static readonly int EDITOR_BUILDABLE = 402669568;

    public static readonly int BLOCK_LADDER = 473546752;

    public static readonly int BLOCK_PICKUP = 402751488;

    public static readonly int BLOCK_LASER = 479847424;

    public static readonly int BLOCK_RESOURCE = 638976;

    public static readonly int BLOCK_ITEM = 404340736;

    public static readonly int BLOCK_VEHICLE = 1671168;

    /// <summary>
    /// Used to test whether player can fit in a space.
    /// Includes terrain because tested capsule could be slightly underground, and clip to prevent exploits at sky limit.
    /// </summary>
    public static readonly int BLOCK_STANCE = 473546752;

    public static readonly int BLOCK_NAVMESH = 4734976;

    public static readonly int BLOCK_KILLCAM = 471384064;

    public static readonly int BLOCK_PLAYERCAM = 471384064;

    public static readonly int BLOCK_PLAYERCAM_1P = 470335488;

    /// <summary>
    /// Used for third-person camera in vehicle.
    /// Does not include resource layer because attached barricades are put on that layer.
    /// Barricades layer itself is included to prevent looking inside player bases.
    /// </summary>
    public static readonly int BLOCK_VEHICLECAM = 404258816;

    public static readonly int BLOCK_VISION = 98304;

    public static readonly int BLOCK_COLLISION = 473546752;

    public static readonly int BLOCK_GRASS = 622592;

    public static readonly int BLOCK_LEAN = BLOCK_STANCE;

    /// <summary>
    /// Used to test whether player can enter a vehicle.
    /// Does not include resource layer because attached barricades are put on that layer.
    /// </summary>
    public static readonly int BLOCK_ENTRY = 405897216;

    public static readonly int BLOCK_EXIT = 406437888;

    public static readonly int BLOCK_EXIT_FIND_GROUND = 473546752;

    public static readonly int BLOCK_BARRICADE_INTERACT_LOS = 268533760;

    public static readonly int BLOCK_TIRE = 3784704;

    public static readonly int BLOCK_BARRICADE = 403293696;

    public static readonly int BLOCK_DOOR_OPENING = 403293696;

    public static readonly int BLOCK_BED_LOS = BLOCK_ITEM;

    [Obsolete]
    public static readonly int BLOCK_STRUCTURE = 469763584;

    public static readonly int BLOCK_EXPLOSION = 403816448;

    public static readonly int BLOCK_EXPLOSION_PENETRATE_BUILDABLES = 1163264;

    public static readonly int BLOCK_WIND = 402685952;

    public static readonly int BLOCK_FRAME = 134217732;

    public static readonly int BLOCK_WINDOW = 134217732;

    public static readonly int BLOCK_SENTRY = 471449600;

    public static readonly int BLOCK_CHAR_BUILDABLE_OVERLAP = 512;

    public static readonly int BLOCK_CHAR_BUILDABLE_OVERLAP_NOT_ON_VEHICLE = 67109376;

    public static readonly int BLOCK_CHAR_HINGE_OVERLAP = 67109376;

    public static readonly int BLOCK_CHAR_HINGE_OVERLAP_ON_VEHICLE = 512;

    public static readonly int BLOCK_TRAIN = 469860352;

    public static readonly int WAYPOINT = 404340736;

    public static readonly int DAMAGE_PHYSICS = 471449600;

    public static readonly int DAMAGE_CLIENT = 479970304;

    public static readonly int DAMAGE_SERVER = 404340736;

    public static readonly int DAMAGE_ZOMBIE = 469762048;

    [Obsolete("Replaced by EFFECT_SPLATTER to make const")]
    public static readonly int SPLATTER = 1671168;

    /// <summary>
    /// 2023-02-02: adding more layers since splatter can be attached to them now.
    /// parent should only be set if that system also calls ClearAttachments, otherwise attachedEffects will leak memory.
    /// </summary>
    public const int EFFECT_SPLATTER = 471433216;

    /// <summary>
    /// Layer mask for CharacterController overlap test.
    /// </summary>
    public const int CHARACTER_CONTROLLER_MOVE = 406437888;

    /// <summary>
    /// Layer mask for CharacterController overlap test while inside landscape hole volume.
    /// </summary>
    public const int CHARACTER_CONTROLLER_MOVE_IGNORE_GROUND = 405389312;

    /// <summary>
    /// Lightning strike raycasts from sky to ground using this layer mask.
    /// </summary>
    public const int LIGHTNING = 471449600;
}
