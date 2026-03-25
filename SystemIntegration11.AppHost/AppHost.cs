var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("username", "guest");
var password = builder.AddParameter("password", "guest");
var rabbitmq = builder.AddRabbitMQ("messaging", username, password)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithManagementPlugin();

var pgpassword = builder.AddParameter("postgresql-password", "1234asdf");
var postgres = builder.AddPostgres("postgres", password: pgpassword)
    .WithPgWeb()
    .WithDataVolume(isReadOnly: false)
    .WithLifetime(ContainerLifetime.Persistent);
var ordersDb = postgres.AddDatabase("ordersdb");
var shippingDb = postgres.AddDatabase("shippingdb");

builder.AddProject<Projects.Orders_Api>("order-api")
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq)
    .WithReference(ordersDb)
    .WaitFor(ordersDb);

builder.AddProject<Projects.Shipping_Api>("shipping")
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq)
    .WithReference(shippingDb)
    .WaitFor(shippingDb);

builder.Build().Run();
