using Microsoft.AspNetCore.Mvc;
using ROS_ControlHub.Application.State;
using ROS_ControlHub.Contracts.Dto;

namespace ROS_ControlHub.Api.Controllers;

[ApiController]
[Route("state")]
public class StateController(InMemoryStateStore store) : ControllerBase
{
    private readonly InMemoryStateStore _store = store;

    public ActionResult<SystemStateDto> Get()
    {
        /**
         * dto 변수 값
         * timestampUtc : 상태 스냅샷 생성 시간
         * systemMode : "Idle/Auto/Manual/Error" 등의 모드
         * jobPhase : 워크플로우 단계
         * emergencyStop : 비상정지 여부
         * extensions : 확장용 슬롯
         */
        var snapshot = _store.GetSnapshot();
        var dto = SystemStateDto.From(snapshot);

        return Ok(dto);
    }
    
}