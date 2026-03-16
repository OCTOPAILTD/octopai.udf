namespace NiFiMetadataPlatform.API.Models
{
    public class ContainerInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public Dictionary<string, string> Ports { get; set; } = new();
        public DateTime Created { get; set; }
        public Dictionary<string, string> Labels { get; set; } = new();
        public string? Workspace { get; set; }
    }

    public class ContainerHealth
    {
        public string Status { get; set; } = "unknown";
        public string? Message { get; set; }
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    }

    public class CreateNiFiContainerRequest
    {
        public string Name { get; set; } = "nifi";
        public string Version { get; set; } = "1.12.1";
        public int HttpPort { get; set; } = 8080;
        public int HttpsPort { get; set; } = 8443;
        public string? Workspace { get; set; }
        public Dictionary<string, string>? Environment { get; set; }
    }

    public class CreateKafkaContainerRequest
    {
        public string Name { get; set; } = "kafka";
        public string Version { get; set; } = "latest";
        public int Port { get; set; } = 9092;
        public string? Workspace { get; set; }
        public Dictionary<string, string>? Environment { get; set; }
    }

    public class CreateHiveContainerRequest
    {
        public string Name { get; set; } = "hive";
        public string Version { get; set; } = "latest";
        public int Port { get; set; } = 10000;
        public string? Workspace { get; set; }
        public Dictionary<string, string>? Environment { get; set; }
    }

    public class CreateTrinoContainerRequest
    {
        public string Name { get; set; } = "trino";
        public string Version { get; set; } = "latest";
        public int Port { get; set; } = 8080;
        public string? Workspace { get; set; }
        public Dictionary<string, string>? Environment { get; set; }
    }

    public class CreateImpalaContainerRequest
    {
        public string Name { get; set; } = "impala";
        public string Version { get; set; } = "latest";
        public int Port { get; set; } = 21000;
        public string? Workspace { get; set; }
        public Dictionary<string, string>? Environment { get; set; }
    }

    public class CreateHBaseContainerRequest
    {
        public string Name { get; set; } = "hbase";
        public string Version { get; set; } = "latest";
        public int Port { get; set; } = 16010;
        public string? Workspace { get; set; }
        public Dictionary<string, string>? Environment { get; set; }
    }

    public class CreateDataHubContainerRequest
    {
        public string Name { get; set; } = "datahub";
        public string Version { get; set; } = "latest";
        public int Port { get; set; } = 9002;
        public string? Workspace { get; set; }
        public Dictionary<string, string>? Environment { get; set; }
    }
}
