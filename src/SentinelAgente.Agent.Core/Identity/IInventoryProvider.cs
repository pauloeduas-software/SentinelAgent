namespace SentinelAgente.Agent.Core.Identity;

public interface IInventoryProvider
{
    string GetCpuModel();
    string GetLocalIp();
    string GetMacAddress();
    List<string> GetInstalledSoftware();
}
