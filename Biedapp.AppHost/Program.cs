IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ProjectResource> biedappApi = builder.AddProject<Projects.Biedapp_API>("Backend-API");

IResourceBuilder<ProjectResource> frontendServer = builder.AddProject<Projects.Biedapp_Frontend_Server>("Frontend-Server");

IResourceBuilder<NodeAppResource> frontendClient = builder
    .AddNpmApp("Frontend-Client", "..\\src\\Biedapp.Frontend\\biedapp.frontend.client")
    .WithEnvironment("BROWSER", "chrome")
    .WithReference(frontendServer)
    .WithHttpsEndpoint(port: 4200, targetPort: 4200, isProxied: false)
    .WithNpmPackageInstallation();

builder.Build().Run();
