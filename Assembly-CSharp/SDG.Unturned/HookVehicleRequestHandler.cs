namespace SDG.Unturned;

public delegate void HookVehicleRequestHandler(InteractableVehicle instigatingVehicle, InteractableVehicle targetVehicle, ref bool shouldAllow);
