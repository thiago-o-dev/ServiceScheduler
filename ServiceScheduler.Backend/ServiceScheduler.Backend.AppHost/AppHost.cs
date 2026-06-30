var builder = DistributedApplication.CreateBuilder(args);

// pra quem ta lendo, isso daqui é o nosso lindo orquestrador, ele fala oq vai rodar, como, e esperar por quem.
// n vou usar um env bonitão pq env commitado em github publico faz bot scrapper vir zaralhar, vou só criar o banco por imagem docker msm e sucesso

var postgresPassword = builder.AddParameter("postgres-password", "keycloak", secret: true);
var keycloakPassword = builder.AddParameter("keycloak-password", "leila", secret: true); // senha default do admin

// Databases

var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithDataVolume();

var keycloakDb = postgres.AddDatabase("keycloak-db");
var schedulerDb = postgres.AddDatabase("scheduler-db");

// Infrastructure

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin()
    .WithDataVolume();

var keycloak = builder.AddKeycloak("keycloak", 8081)
    .WithReference(keycloakDb)
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", keycloakPassword)
    .WithDataVolume()
    .WithRealmImport("./KeycloakRealms") // definimos o gerenciador de contas
    .WaitFor(keycloakDb);

// Services

var scheduler = builder.AddProject<Projects.ServiceScheduler_Api>("servicescheduler-api")
    .WithReference(rabbitmq)
    .WithReference(schedulerDb)
    .WithReference(keycloak)
    .WaitFor(rabbitmq)
    .WaitFor(schedulerDb);
    //.WaitFor(keycloak);

var gateway = builder.AddProject<Projects.Api_Gateway>("api-gateway")
    .WithReference(keycloak)
    .WithReference(scheduler)
    .WaitFor(keycloak)
    .WaitFor(scheduler);

var frontendPath = "../../ServiceScheduler.Frontend/cabeleleira-leila-app";

var frontend = builder.AddDockerfile("cabeleleira-leila-app", frontendPath)
    .WithHttpEndpoint(port: 5173, targetPort: 5173, name: "http")
    .WithEnvironment("CHOKIDAR_USEPOLLING", "true")
    .WithBindMount(frontendPath, "/app")
    .WithVolume("cabeleleira-leila-app-node-modules", "/app/node_modules");
    // não precisa de wait for pq react demora 2 mil anos

builder.Build().Run();
