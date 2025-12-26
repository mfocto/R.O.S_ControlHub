namespace ROS_ControlHub.Application.State;

public enum SystemMode
{
    Idle,
    Auto,
    Manual,
    Error
}

public enum JobPhase
{
    None,
    Detect ,
    Pick, 
    Place, 
    CallAGV, 
    Dispatch, 
    Done
}