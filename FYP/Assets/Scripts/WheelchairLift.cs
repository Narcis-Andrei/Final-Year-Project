using UnityEngine;

public class WheelchairLift : MonoBehaviour
{
    public void RequestLiftUp()
    {
        Debug.Log("REMOTE: RequestLiftUp()");
    }

    public void RequestLiftDown()
    {
        Debug.Log("REMOTE: RequestLiftDown()");
    }

    public void RequestDeploy()
    {
        Debug.Log("REMOTE: RequestDeploy()");
    }

    public void RequestStow()
    {
        Debug.Log("REMOTE: RequestStow()");
    }

    public void OnRemoteButton(RemoteButton.ButtonId id)
    {
        switch (id)
        {
            case RemoteButton.ButtonId.TopLeft: RequestDeploy(); break;
            case RemoteButton.ButtonId.TopRight: RequestStow(); break;
            case RemoteButton.ButtonId.BottomLeft: RequestLiftDown(); break;
            case RemoteButton.ButtonId.BottomRight: RequestLiftUp(); break;
        }
    }
}
