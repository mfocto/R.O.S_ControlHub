using ROS_ControlHub.Application.State;

namespace ROS_ControlHub.Application.Workflow;

/// <summary>
/// Job/Workflow 상태
/// TODO : Command API + Safety Gate + Timeout/Retry/Lock
/// </summary>
public static class JobStateMachine
{
    public static JobPhase Next(JobPhase phase)
    {
        return phase switch
        {
            JobPhase.None => JobPhase.Detect,
            JobPhase.Detect => JobPhase.Pick,
            JobPhase.Pick => JobPhase.Place,
            JobPhase.Place => JobPhase.CallAGV,
            JobPhase.CallAGV => JobPhase.Dispatch,
            JobPhase.Dispatch => JobPhase.Done,
            JobPhase.Done => JobPhase.None,
            _ => JobPhase.None
        };
    }
}