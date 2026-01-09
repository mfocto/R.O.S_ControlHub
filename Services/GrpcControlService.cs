using Grpc.Core;
using ROS_ControlHub.Adapters.Abstractions;
using Control; // generated namespace from proto

namespace ROS_ControlHub.Services;

public class GrpcControlService : ControlService.ControlServiceBase
{
    private readonly IOpcUaAdapter _opcAdapter;
    private readonly ILogger<GrpcControlService> _logger;

    public GrpcControlService(IOpcUaAdapter opcAdapter, ILogger<GrpcControlService> logger)
    {
        _opcAdapter = opcAdapter;
        _logger = logger;
    }

    public override async Task<DeviceResult> SetDeviceState(DeviceCommand request, ServerCallContext context)
    {
        _logger.LogInformation("SetDeviceState: DeviceId={DeviceId}, Command={Command}", request.DeviceId, request.Command);

        try
        {
            // Command와 Payload를 합쳐서 State JSON 생성 (단순화)
            // 실제로는 Command에 따라 State JSON 구조를 다르게 만들어야 함
            var stateJson = $"{{\"status\": \"{request.Command}\", \"payload\": {request.PayloadJson}}}";
            
            await _opcAdapter.WriteStateAsync(request.DeviceId, stateJson);

            return new DeviceResult { Success = true, Message = "Command executed" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set device state");
            return new DeviceResult { Success = false, Message = ex.Message };
        }
    }

    public override async Task<GlobalResult> SetAllDevicesState(GlobalCommand request, ServerCallContext context)
    {
         _logger.LogInformation("SetAllDevicesState: Command={Command}", request.Command);
         // TODO: 모든 디바이스 목록을 조회해서 루프 돌며 WriteStateAsync 호출 필요
         // 여기서는 Mock
         return new GlobalResult { Success = true, Message = "Global command executed", AffectedCount = 0 };
    }

    public override async Task<DeviceResult> MoveAgv(AgvMoveCommand request, ServerCallContext context)
    {
        _logger.LogInformation("MoveAgv: DeviceId={DeviceId}, TargetType={TargetType}", request.DeviceId, request.TargetType);
        
        try
        {
            string stateJson;
            if (request.TargetType == "coordinate")
            {
                stateJson = $"{{\"target\": {{\"x\": {request.X}, \"y\": {request.Y}}}}}";
            }
            else
            {
                stateJson = $"{{\"target\": \"{request.PointName}\"}}";
            }

            await _opcAdapter.WriteStateAsync(request.DeviceId, stateJson);
            return new DeviceResult { Success = true, Message = "AGV moving" };
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Failed to move AGV");
             return new DeviceResult { Success = false, Message = ex.Message };
        }
    }
}
