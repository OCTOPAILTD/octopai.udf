using NiFiMetadataPlatform.API.Models;

namespace NiFiMetadataPlatform.API.Services
{
    public interface IDockerContainerService
    {
        Task<ContainerInfo> CreateNiFiContainerAsync(CreateNiFiContainerRequest request);
        Task<ContainerInfo> CreateKafkaContainerAsync(CreateKafkaContainerRequest request);
        Task<ContainerInfo> CreateHiveContainerAsync(CreateHiveContainerRequest request);
        Task<ContainerInfo> CreateTrinoContainerAsync(CreateTrinoContainerRequest request);
        Task<ContainerInfo> CreateImpalaContainerAsync(CreateImpalaContainerRequest request);
        Task<ContainerInfo> CreateHBaseContainerAsync(CreateHBaseContainerRequest request);
        Task<ContainerInfo> CreateDataHubContainerAsync(CreateDataHubContainerRequest request);
        Task<IEnumerable<ContainerInfo>> ListContainersAsync();
        Task<ContainerInfo?> GetContainerAsync(string containerId);
        Task<bool> StartContainerAsync(string containerId);
        Task<bool> StopContainerAsync(string containerId);
        Task<bool> RemoveContainerAsync(string containerId);
        Task<ContainerHealth> GetContainerHealthAsync(string containerId);
        Task<Stream> StreamContainerLogsAsync(string containerId);
        Task StreamContainerLogsAsync(string containerId, Func<string, Task> onLogLine, CancellationToken cancellationToken);
    }
}
