using UnityEngine;

public class RiderController : MonoBehaviour
{
    public Transform boat;
    public float steerSpeed = 6f;
    public float maxOffset = 4f;
    public float towDistance = 4f;

    float lateralOffset = 0f;

    void Update()
    {
        if (boat == null) return;

        if (Input.GetKey(KeyCode.LeftArrow)) lateralOffset -= steerSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.RightArrow)) lateralOffset += steerSpeed * Time.deltaTime;
        lateralOffset = Mathf.Clamp(lateralOffset, -maxOffset, maxOffset);

        Vector3 targetPos = boat.position - boat.forward * towDistance + boat.right * lateralOffset;
        targetPos.y = transform.position.y;
        transform.position = targetPos;

        float tilt = Mathf.Clamp(-lateralOffset * 6f, -30f, 30f);
        transform.rotation = Quaternion.Euler(0f, boat.eulerAngles.y, tilt);
    }
}
