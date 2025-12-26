

using ROS_ControlHub.Adapters.Abstractions;
using ROS_ControlHub.Adapters.Stubs;
using ROS_ControlHub.Api.Hubs;
using ROS_ControlHub.Application.State;
using ROS_ControlHub.Contracts.Grpc;
using ROS_ControlHub.Workers;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------
// 기본 프레임워크
// -------------------------------------

builder.Services.AddControllers(); // 컨트롤러 사용

builder.Services.AddSignalR();  // SignalR: web - unity 용 통신채널

builder.Services.AddGrpc(); // gRPC : ROS 연동용

// -------------------------------------
// state store
// -------------------------------------

// Inmemory 방식 
builder.Services.AddSingleton<InMemoryStateStore>();

// -------------------------------------
// Adapter
// -------------------------------------

// ROS
builder.Services.AddSingleton<IRosAdapter, RosStubAdapter>();

// OPC-UA
builder.Services.AddSingleton<IOpcUaAdapter, OpcUaStubAdapter>();

// -------------------------------------
// 상태 수집/집계
// -------------------------------------

builder.Services.AddHostedService<StatePollingWorker>();

var app = builder.Build();
// -------------------------------------
// HTTP Endpoints
// -------------------------------------

app.MapControllers();

// signalR hub
app.MapHub<StateHub>("hubs/state");

// gRPC
app.MapGrpcService<PlaceholderGrpcService>();

app.Run();
