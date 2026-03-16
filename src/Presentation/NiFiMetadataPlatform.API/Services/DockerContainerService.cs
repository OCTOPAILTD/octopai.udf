using Docker.DotNet;
using Docker.DotNet.Models;
using NiFiMetadataPlatform.API.Models;

namespace NiFiMetadataPlatform.API.Services
{
    public class DockerContainerService : IDockerContainerService
    {
        private readonly DockerClient _dockerClient;
        private readonly ILogger<DockerContainerService> _logger;
        private const string NetworkName = "docker_nifi-metadata-network"; // Docker Compose prefixes network names

        public DockerContainerService(ILogger<DockerContainerService> logger)
        {
            _logger = logger;
            
            // Connect to Docker daemon (Windows named pipe or Unix socket)
            var dockerUri = Environment.OSVersion.Platform == PlatformID.Win32NT
                ? "npipe://./pipe/docker_engine"
                : "unix:///var/run/docker.sock";
            
            _dockerClient = new DockerClientConfiguration(new Uri(dockerUri)).CreateClient();
        }

        public async Task<ContainerInfo> CreateNiFiContainerAsync(CreateNiFiContainerRequest request)
        {
            _logger.LogInformation("Creating NiFi container: {Name}", request.Name);

            var image = $"apache/nifi:{request.Version}";
            await EnsureImageExistsAsync(image);

            var sanitizedName = SanitizeContainerName(request.Name);

            var labels = new Dictionary<string, string>
            {
                { "managed-by", "nifi-metadata-platform" },
                { "tool-type", "nifi" }
            };
            
            if (!string.IsNullOrEmpty(request.Workspace))
            {
                labels["workspace"] = request.Workspace;
            }

            var createParams = new CreateContainerParameters
            {
                Image = image,
                Name = sanitizedName,
                ExposedPorts = new Dictionary<string, EmptyStruct>
                {
                    { $"{request.HttpPort}/tcp", default },
                    { $"{request.HttpsPort}/tcp", default }
                },
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        { $"{request.HttpPort}/tcp", new List<PortBinding> { new() { HostPort = request.HttpPort.ToString() } } },
                        { $"{request.HttpsPort}/tcp", new List<PortBinding> { new() { HostPort = request.HttpsPort.ToString() } } }
                    },
                    NetworkMode = NetworkName
                },
                Env = BuildEnvironmentVariables(request.Environment, new Dictionary<string, string>
                {
                    { "NIFI_WEB_HTTP_PORT", request.HttpPort.ToString() },
                    { "NIFI_WEB_HTTPS_PORT", request.HttpsPort.ToString() },
                    { "SINGLE_USER_CREDENTIALS_USERNAME", "admin" },
                    { "SINGLE_USER_CREDENTIALS_PASSWORD", "ctsBtRBKHRAx69EqUghvvgEvjnaLjFEB" }
                }),
                Labels = labels
            };

            var response = await _dockerClient.Containers.CreateContainerAsync(createParams);
            await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());

            _logger.LogInformation("NiFi container created: {Id}", response.ID);
            return await GetContainerAsync(response.ID) ?? throw new Exception("Failed to retrieve created container");
        }

        public async Task<ContainerInfo> CreateKafkaContainerAsync(CreateKafkaContainerRequest request)
        {
            _logger.LogInformation("Creating Kafka container: {Name}", request.Name);

            var image = $"confluentinc/cp-kafka:{request.Version}";
            await EnsureImageExistsAsync(image);

            var sanitizedName = SanitizeContainerName(request.Name);

            var labels = new Dictionary<string, string>
            {
                { "managed-by", "nifi-metadata-platform" },
                { "tool-type", "kafka" }
            };
            
            if (!string.IsNullOrEmpty(request.Workspace))
            {
                labels["workspace"] = request.Workspace;
            }

            var createParams = new CreateContainerParameters
            {
                Image = image,
                Name = sanitizedName,
                ExposedPorts = new Dictionary<string, EmptyStruct>
                {
                    { $"{request.Port}/tcp", default }
                },
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        { $"{request.Port}/tcp", new List<PortBinding> { new() { HostPort = request.Port.ToString() } } }
                    },
                    NetworkMode = NetworkName
                },
                Env = BuildEnvironmentVariables(request.Environment, new Dictionary<string, string>
                {
                    { "KAFKA_BROKER_ID", "1" },
                    { "KAFKA_ZOOKEEPER_CONNECT", "zookeeper:2181" },
                    { "KAFKA_ADVERTISED_LISTENERS", $"PLAINTEXT://localhost:{request.Port}" },
                    { "KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1" }
                }),
                Labels = labels
            };

            var response = await _dockerClient.Containers.CreateContainerAsync(createParams);
            await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());

            _logger.LogInformation("Kafka container created: {Id}", response.ID);
            return await GetContainerAsync(response.ID) ?? throw new Exception("Failed to retrieve created container");
        }

        public async Task<ContainerInfo> CreateHiveContainerAsync(CreateHiveContainerRequest request)
        {
            _logger.LogInformation("Creating Hive container: {Name}", request.Name);

            var image = $"apache/hive:{request.Version}";
            await EnsureImageExistsAsync(image);

            var sanitizedName = SanitizeContainerName(request.Name);

            var labels = new Dictionary<string, string>
            {
                { "managed-by", "nifi-metadata-platform" },
                { "tool-type", "hive" }
            };
            
            if (!string.IsNullOrEmpty(request.Workspace))
            {
                labels["workspace"] = request.Workspace;
            }

            var createParams = new CreateContainerParameters
            {
                Image = image,
                Name = sanitizedName,
                ExposedPorts = new Dictionary<string, EmptyStruct>
                {
                    { $"{request.Port}/tcp", default }
                },
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        { $"{request.Port}/tcp", new List<PortBinding> { new() { HostPort = request.Port.ToString() } } }
                    },
                    NetworkMode = NetworkName
                },
                Env = BuildEnvironmentVariables(request.Environment, new Dictionary<string, string>()),
                Labels = labels
            };

            var response = await _dockerClient.Containers.CreateContainerAsync(createParams);
            await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());

            _logger.LogInformation("Hive container created: {Id}", response.ID);
            return await GetContainerAsync(response.ID) ?? throw new Exception("Failed to retrieve created container");
        }

        public async Task<ContainerInfo> CreateTrinoContainerAsync(CreateTrinoContainerRequest request)
        {
            _logger.LogInformation("Creating Trino container: {Name}", request.Name);

            var image = $"trinodb/trino:{request.Version}";
            await EnsureImageExistsAsync(image);

            var sanitizedName = SanitizeContainerName(request.Name);

            var labels = new Dictionary<string, string>
            {
                { "managed-by", "nifi-metadata-platform" },
                { "tool-type", "trino" }
            };
            
            if (!string.IsNullOrEmpty(request.Workspace))
            {
                labels["workspace"] = request.Workspace;
            }

            var createParams = new CreateContainerParameters
            {
                Image = image,
                Name = sanitizedName,
                ExposedPorts = new Dictionary<string, EmptyStruct>
                {
                    { $"{request.Port}/tcp", default }
                },
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        { $"{request.Port}/tcp", new List<PortBinding> { new() { HostPort = request.Port.ToString() } } }
                    },
                    NetworkMode = NetworkName
                },
                Env = BuildEnvironmentVariables(request.Environment, new Dictionary<string, string>()),
                Labels = labels
            };

            var response = await _dockerClient.Containers.CreateContainerAsync(createParams);
            await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());

            _logger.LogInformation("Trino container created: {Id}", response.ID);
            return await GetContainerAsync(response.ID) ?? throw new Exception("Failed to retrieve created container");
        }

        public async Task<ContainerInfo> CreateImpalaContainerAsync(CreateImpalaContainerRequest request)
        {
            _logger.LogInformation("Creating Impala container: {Name}", request.Name);

            var image = $"apache/impala:{request.Version}";
            await EnsureImageExistsAsync(image);

            var sanitizedName = SanitizeContainerName(request.Name);

            var labels = new Dictionary<string, string>
            {
                { "managed-by", "nifi-metadata-platform" },
                { "tool-type", "impala" }
            };
            
            if (!string.IsNullOrEmpty(request.Workspace))
            {
                labels["workspace"] = request.Workspace;
            }

            var createParams = new CreateContainerParameters
            {
                Image = image,
                Name = sanitizedName,
                ExposedPorts = new Dictionary<string, EmptyStruct>
                {
                    { $"{request.Port}/tcp", default }
                },
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        { $"{request.Port}/tcp", new List<PortBinding> { new() { HostPort = request.Port.ToString() } } }
                    },
                    NetworkMode = NetworkName
                },
                Env = BuildEnvironmentVariables(request.Environment, new Dictionary<string, string>()),
                Labels = labels
            };

            var response = await _dockerClient.Containers.CreateContainerAsync(createParams);
            await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());

            _logger.LogInformation("Impala container created: {Id}", response.ID);
            return await GetContainerAsync(response.ID) ?? throw new Exception("Failed to retrieve created container");
        }

        public async Task<ContainerInfo> CreateHBaseContainerAsync(CreateHBaseContainerRequest request)
        {
            _logger.LogInformation("Creating HBase container: {Name}", request.Name);

            var image = $"harisekhon/hbase:{request.Version}";
            await EnsureImageExistsAsync(image);

            var sanitizedName = SanitizeContainerName(request.Name);

            var labels = new Dictionary<string, string>
            {
                { "managed-by", "nifi-metadata-platform" },
                { "tool-type", "hbase" }
            };
            
            if (!string.IsNullOrEmpty(request.Workspace))
            {
                labels["workspace"] = request.Workspace;
            }

            var createParams = new CreateContainerParameters
            {
                Image = image,
                Name = sanitizedName,
                ExposedPorts = new Dictionary<string, EmptyStruct>
                {
                    { $"{request.Port}/tcp", default }
                },
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        { $"{request.Port}/tcp", new List<PortBinding> { new() { HostPort = request.Port.ToString() } } }
                    },
                    NetworkMode = NetworkName
                },
                Env = BuildEnvironmentVariables(request.Environment, new Dictionary<string, string>()),
                Labels = labels
            };

            var response = await _dockerClient.Containers.CreateContainerAsync(createParams);
            await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());

            _logger.LogInformation("HBase container created: {Id}", response.ID);
            return await GetContainerAsync(response.ID) ?? throw new Exception("Failed to retrieve created container");
        }

        public async Task<ContainerInfo> CreateDataHubContainerAsync(CreateDataHubContainerRequest request)
        {
            _logger.LogInformation("Creating DataHub container: {Name}", request.Name);

            var image = $"acryldata/datahub-gms:{request.Version}";
            await EnsureImageExistsAsync(image);

            var sanitizedName = SanitizeContainerName(request.Name);

            var labels = new Dictionary<string, string>
            {
                { "managed-by", "nifi-metadata-platform" },
                { "tool-type", "datahub" }
            };
            
            if (!string.IsNullOrEmpty(request.Workspace))
            {
                labels["workspace"] = request.Workspace;
            }

            var createParams = new CreateContainerParameters
            {
                Image = image,
                Name = sanitizedName,
                ExposedPorts = new Dictionary<string, EmptyStruct>
                {
                    { $"{request.Port}/tcp", default }
                },
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        { $"{request.Port}/tcp", new List<PortBinding> { new() { HostPort = request.Port.ToString() } } }
                    },
                    NetworkMode = NetworkName
                },
                Env = BuildEnvironmentVariables(request.Environment, new Dictionary<string, string>()),
                Labels = labels
            };

            var response = await _dockerClient.Containers.CreateContainerAsync(createParams);
            await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());

            _logger.LogInformation("DataHub container created: {Id}", response.ID);
            return await GetContainerAsync(response.ID) ?? throw new Exception("Failed to retrieve created container");
        }

        public async Task<IEnumerable<ContainerInfo>> ListContainersAsync()
        {
            var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    { "label", new Dictionary<string, bool> { { "managed-by=nifi-metadata-platform", true } } }
                }
            });

            return containers.Select(MapToContainerInfo);
        }

        public async Task<ContainerInfo?> GetContainerAsync(string containerId)
        {
            try
            {
                var container = await _dockerClient.Containers.InspectContainerAsync(containerId);
                var labels = container.Config.Labels?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>();
                
                return new ContainerInfo
                {
                    Id = container.ID,
                    Name = container.Name.TrimStart('/'),
                    Image = container.Config.Image,
                    Status = container.State.Status,
                    State = container.State.Status,
                    Created = container.Created,
                    Ports = container.NetworkSettings.Ports?
                        .Where(p => p.Value != null && p.Value.Any())
                        .ToDictionary(
                            p => p.Key,
                            p => p.Value.First().HostPort
                        ) ?? new Dictionary<string, string>(),
                    Labels = labels,
                    Workspace = labels.ContainsKey("workspace") ? labels["workspace"] : null
                };
            }
            catch (DockerContainerNotFoundException)
            {
                return null;
            }
        }

        public async Task<bool> StartContainerAsync(string containerId)
        {
            try
            {
                await _dockerClient.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
                _logger.LogInformation("Container started: {Id}", containerId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start container: {Id}", containerId);
                return false;
            }
        }

        public async Task<bool> StopContainerAsync(string containerId)
        {
            try
            {
                await _dockerClient.Containers.StopContainerAsync(containerId, new ContainerStopParameters());
                _logger.LogInformation("Container stopped: {Id}", containerId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop container: {Id}", containerId);
                return false;
            }
        }

        public async Task<bool> RemoveContainerAsync(string containerId)
        {
            try
            {
                await _dockerClient.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters { Force = true });
                _logger.LogInformation("Container removed: {Id}", containerId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove container: {Id}", containerId);
                return false;
            }
        }

        public async Task<ContainerHealth> GetContainerHealthAsync(string containerId)
        {
            try
            {
                var container = await _dockerClient.Containers.InspectContainerAsync(containerId);
                
                var health = new ContainerHealth
                {
                    CheckedAt = DateTime.UtcNow
                };

                if (container.State.Health != null)
                {
                    health.Status = container.State.Health.Status.ToLowerInvariant();
                    health.Message = container.State.Health.Log?.LastOrDefault()?.Output;
                }
                else
                {
                    health.Status = container.State.Running ? "healthy" : "unhealthy";
                    health.Message = container.State.Status;
                }

                return health;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get container health: {Id}", containerId);
                return new ContainerHealth
                {
                    Status = "unknown",
                    Message = ex.Message
                };
            }
        }

        public async Task<Stream> StreamContainerLogsAsync(string containerId)
        {
            var stream = await _dockerClient.Containers.GetContainerLogsAsync(
                containerId,
                new ContainerLogsParameters
                {
                    ShowStdout = true,
                    ShowStderr = true,
                    Follow = true,
                    Tail = "100"
                });

            return stream;
        }

        public async Task StreamContainerLogsAsync(string containerId, Func<string, Task> onLogLine, CancellationToken cancellationToken)
        {
            try
            {
                var stream = await _dockerClient.Containers.GetContainerLogsAsync(
                    containerId,
                    new ContainerLogsParameters
                    {
                        ShowStdout = true,
                        ShowStderr = true,
                        Follow = true,
                        Tail = "all"
                    },
                    cancellationToken);

                using var reader = new StreamReader(stream);
                var buffer = new byte[8]; // Docker log format: 8 bytes header + payload

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // Read Docker log header (8 bytes)
                        var headerBytes = new byte[8];
                        var bytesRead = await stream.ReadAsync(headerBytes, 0, 8, cancellationToken);
                        
                        if (bytesRead < 8)
                            break;

                        // Parse payload size from header (bytes 4-7, big-endian)
                        var payloadSize = (headerBytes[4] << 24) | (headerBytes[5] << 16) | (headerBytes[6] << 8) | headerBytes[7];
                        
                        if (payloadSize > 0 && payloadSize < 1024 * 1024) // Sanity check: max 1MB per line
                        {
                            var payloadBytes = new byte[payloadSize];
                            var payloadRead = await stream.ReadAsync(payloadBytes, 0, payloadSize, cancellationToken);
                            
                            if (payloadRead > 0)
                            {
                                var logLine = System.Text.Encoding.UTF8.GetString(payloadBytes, 0, payloadRead).TrimEnd('\n', '\r');
                                if (!string.IsNullOrWhiteSpace(logLine))
                                {
                                    await onLogLine(logLine);
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error reading log line from container {ContainerId}", containerId);
                        // Continue reading despite errors
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stream logs for container: {ContainerId}", containerId);
                throw;
            }
        }

        private async Task EnsureImageExistsAsync(string image)
        {
            try
            {
                await _dockerClient.Images.InspectImageAsync(image);
                _logger.LogInformation("Image already exists: {Image}", image);
            }
            catch (DockerImageNotFoundException)
            {
                _logger.LogInformation("Pulling image: {Image}", image);
                await _dockerClient.Images.CreateImageAsync(
                    new ImagesCreateParameters { FromImage = image },
                    null,
                    new Progress<JSONMessage>());
            }
        }

        private List<string> BuildEnvironmentVariables(
            Dictionary<string, string>? customEnv,
            Dictionary<string, string> defaultEnv)
        {
            var env = new Dictionary<string, string>(defaultEnv);
            
            if (customEnv != null)
            {
                foreach (var kvp in customEnv)
                {
                    env[kvp.Key] = kvp.Value;
                }
            }

            return env.Select(kvp => $"{kvp.Key}={kvp.Value}").ToList();
        }

        private ContainerInfo MapToContainerInfo(ContainerListResponse container)
        {
            var labels = container.Labels?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>();
            
            return new ContainerInfo
            {
                Id = container.ID,
                Name = container.Names?.FirstOrDefault()?.TrimStart('/') ?? string.Empty,
                Image = container.Image ?? string.Empty,
                Status = container.Status ?? string.Empty,
                State = container.State ?? string.Empty,
                Created = container.Created,
                Ports = container.Ports?
                    .Where(p => p.PublicPort > 0)
                    .GroupBy(p => $"{p.PrivatePort}/{p.Type}")
                    .ToDictionary(
                        g => g.Key,
                        g => g.First().PublicPort.ToString()
                    ) ?? new Dictionary<string, string>(),
                Labels = labels,
                Workspace = labels.ContainsKey("workspace") ? labels["workspace"] : null
            };
        }

        private string SanitizeContainerName(string name)
        {
            return System.Text.RegularExpressions.Regex.Replace(name, @"[^a-zA-Z0-9_.-]", "-").ToLowerInvariant();
        }
    }
}
