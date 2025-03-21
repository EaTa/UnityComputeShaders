﻿using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class GirlController : MonoBehaviour
{
    public Material material;
    NavMeshAgent agent;

    Animator anim;
    Camera cam;
    Vector2 smoothDeltaPosition = Vector2.zero;
    Vector2 velocity = Vector2.zero;

    void Start()
    {
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        cam = Camera.main;
        // Don’t update position automatically
        agent.updatePosition = false;

        FindAndSelectMaterial();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out var hit))
            {
                agent.destination = hit.point;
                if (material)
                {
                    var pos = new Vector4(hit.point.x, hit.point.y, hit.point.z, 0);
                    material.SetVector("_Position", pos);
                }
            }
        }

        var worldDeltaPosition = agent.nextPosition - transform.position;

        // Map 'worldDeltaPosition' to local space
        var dx = Vector3.Dot(transform.right, worldDeltaPosition);
        var dy = Vector3.Dot(transform.forward, worldDeltaPosition);
        var deltaPosition = new Vector2(dx, dy);

        // Low-pass filter the deltaMove
        var smooth = Mathf.Min(1.0f, Time.deltaTime / 0.15f);
        smoothDeltaPosition = Vector2.Lerp(smoothDeltaPosition, deltaPosition, smooth);

        // Update velocity if time advances
        if (Time.deltaTime > 1e-5f)
            velocity = smoothDeltaPosition / Time.deltaTime;

        var speed = velocity.magnitude;
        var shouldMove = speed > 0.5f; // && agent.remainingDistance > agent.radius;

        // Update animation parameters
        anim.SetFloat("speed", speed);

        //GetComponent<LookAt>().lookAtTargetPosition = agent.steeringTarget + transform.forward;
    }

    void OnAnimatorMove()
    {
        // Update position to agent position
        transform.position = agent.nextPosition;
    }

    void FindAndSelectMaterial()
    {
        var go = GameObject.Find("Plane");
        if (go)
        {
            var renderer = go.GetComponent<Renderer>();
            material = renderer.material;

            if (material)
            {
                var pos = new Vector4(transform.position.x, transform.position.y, transform.position.z, 0);
                material.SetVector("_Position", pos);
            }
        }
    }
}