using UnityEngine;

public class WheelchairLift : MonoBehaviour
{
    [SerializeField] private WheelchairLiftSystem lift;

    public void OnRemoteButton(RemoteButton.ButtonId id)
    {
        if (lift == null)
        {
            Debug.LogWarning("REMOTE: lift reference not set!");
            return;
        }

        switch (id)
        {
            // Top Left = "Resting top"
            case RemoteButton.ButtonId.TopLeft:
                Debug.Log("REMOTE: Resting Top");
                lift.RequestStowFromBusLevel();
                break;

            // Bottom Left = "Bus level"
            case RemoteButton.ButtonId.BottomLeft:
                Debug.Log("REMOTE: Deploy to BusLevel");
                lift.RequestDeployToBusLevel();
                break;

            // Top Right = "Up from floor"
            case RemoteButton.ButtonId.TopRight:
                Debug.Log("REMOTE: Raise to BusLevel");
                lift.RequestRaiseToBusLevel();
                break;

            // Bottom Right = "Down to floor"
            case RemoteButton.ButtonId.BottomRight:
                Debug.Log("REMOTE: Lower to Ground");
                lift.RequestLowerToGround();
                break;
        }
    }
}
