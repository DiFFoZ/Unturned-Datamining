using SDG.Framework.IO.Streams;

namespace SDG.Provider.Services.Economy;

public interface IEconomyItemDefinition : INetworkStreamable
{
    string getPropertyValue(string key);
}
