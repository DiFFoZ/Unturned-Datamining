namespace SDG.Unturned;

public delegate void VehicleUpdated(bool isDriveable, ushort newFuel, ushort maxFuel, float newSpeed, float minSpeed, float maxSpeed, ushort newHealth, ushort maxHealth, ushort newBatteryCharge);
