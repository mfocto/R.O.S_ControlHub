using ROS_ControlHub.Adapters.Abstractions;
using ROS_ControlHub.Services; 

using ROS_ControlHub.Adapters.Stubs;
using ROS_ControlHub.Adapters.Real;
using ROS_ControlHub.Api.Hubs;
using ROS_ControlHub.Application.State;
using ROS_ControlHub.Contracts.Grpc;
using ROS_ControlHub.Workers;
using ROS_ControlHub.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------
// 기본 프레임워크
// -------------------------------------

builder.Services.AddControllers(); // 컨트롤러 사용

builder.Services.AddSignalR();  // SignalR: web - unity 용 통신채널

builder.Services.AddGrpc(); // gRPC : ROS 연동용

builder.Services.AddInfrastructure(builder.Configuration);

// -------------------------------------
// state store
// -------------------------------------

// Inmemory 방식 
builder.Services.AddSingleton<InMemoryStateStore>();

// -------------------------------------
// Adapter
// -------------------------------------

// ROS
// builder.Services.AddSingleton<IRosAdapter, RosStubAdapter>(); // 제거됨

// OPC-UA
// builder.Services.AddSingleton<IOpcUaAdapter, OpcUaStubAdapter>();
builder.Services.AddSingleton<IOpcUaAdapter, OpcUaClientAdapter>();

// -------------------------------------
// 상태 수집/집계
// -------------------------------------

builder.Services.AddHostedService<StatePollingWorker>();

// 시작 시 복구 서비스
builder.Services.AddHostedService<StartupRecoveryService>();


builder.WebHost.UseUrls("http://0.0.0.0:5178");

var app = builder.Build();
// -------------------------------------
// HTTP Endpoints
// -------------------------------------
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

// signalR hub
app.MapHub<StateHub>("hubs/state");

app.MapHub<WebRtcHub>("/hubs/webrtc");

// gRPC
app.MapGrpcService<GrpcControlService>();


app.Run();
