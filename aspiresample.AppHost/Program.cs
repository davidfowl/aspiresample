var builder = DistributedApplication.CreateBuilder(args);

var tempo = builder.AddContainer("tempo", "grafana/tempo", "2.3.1")
    .WithServiceBinding(containerPort: 3200, hostPort: 3200, name: "http", scheme: "http")
    .WithServiceBinding(containerPort: 4317, hostPort: 4007, name: "otlp", scheme: "http")
    .WithVolumeMount("../config/tempo.yml", "/etc/tempo.yaml", VolumeMountType.Bind)
    .WithVolumeMount("tempo", "/tmp/tempo", VolumeMountType.Named)
    .WithArgs("-config.file=/etc/tempo.yaml");

var otel = builder.AddContainer("otel", "otel/opentelemetry-collector-contrib", "0.91.0")
    .WithServiceBinding(containerPort: 4317, hostPort: 4317, name: "grpc", scheme: "http") // Have to put the schema to HTTP otherwise the C# will complain about the OTEL_EXPORTER_OTLP_ENDPOINT variable
    .WithServiceBinding(containerPort: 55679, hostPort: 9200, name: "zpages-http", scheme: "http")
    .WithVolumeMount("../config/otel.yml", "/etc/otel-collector-config.yaml", VolumeMountType.Bind)
    .WithArgs("--config=/etc/otel-collector-config.yaml")
    .WithEnvironment("TEMPO_URL", tempo.GetEndpoint("otlp"))
    .WithDashboardEndpoint("DASHBOARD_URL");

var apiService = builder.AddProject<Projects.aspiresample_ApiService>("apiservice")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otel.GetEndpoint("grpc"));

builder.AddProject<Projects.aspiresample_Web>("webfrontend")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otel.GetEndpoint("grpc"))
    .WithReference(apiService);

builder.Build().Run();
