using System.Collections.Generic;
using Unturned.SystemEx;

namespace SDG.Unturned;

internal class CargoBuilder
{
    internal Dictionary<string, List<CargoDeclaration>> declarations = new Dictionary<string, List<CargoDeclaration>>();

    /// <summary>
    /// Finds an existing "{{Cargo/name" (if any), otherwise adds a new one.
    /// </summary>
    public CargoDeclaration GetOrAddDeclaration(string name)
    {
        List<CargoDeclaration> orAddNew = declarations.GetOrAddNew(name);
        if (orAddNew.IsEmpty())
        {
            CargoDeclaration item = new CargoDeclaration();
            orAddNew.Add(item);
        }
        return orAddNew[0];
    }

    /// <summary>
    /// Adds a new "{{Cargo/name" even if one already exists.
    /// </summary>
    public CargoDeclaration AddDeclaration(string name)
    {
        List<CargoDeclaration> orAddNew = declarations.GetOrAddNew(name);
        CargoDeclaration cargoDeclaration = new CargoDeclaration();
        orAddNew.Add(cargoDeclaration);
        return cargoDeclaration;
    }

    public void Clear()
    {
        declarations.Clear();
    }
}
