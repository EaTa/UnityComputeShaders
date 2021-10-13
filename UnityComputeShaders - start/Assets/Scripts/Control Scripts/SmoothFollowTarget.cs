using UnityEngine;

public class SmoothFollowTarget : MonoBehaviour
{
    public GameObject target;
    public float[] limitsX;


    bool b;
    Vector3 offset;

    void LateUpdate()
    {
        if (target == null)
        {
            target = GameObject.FindGameObjectWithTag("Player");
            return;
        }

        if (!b)
        {
            offset = transform.position - target.transform.position;
            b = true;
        }

        var pos = target.transform.position + offset;
        if (limitsX != null && limitsX.Length == 2)
            pos.x = Mathf.Clamp(pos.x, limitsX[0], limitsX[1]);
        //Debug.Log("pos.x clamped to " + pos.x);
        transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime * 5);
        transform.LookAt(target.transform);
    }
}