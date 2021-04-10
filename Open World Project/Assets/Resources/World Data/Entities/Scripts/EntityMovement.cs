using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EntityMovement : MonoBehaviour
{
    protected Animator entity_animator;
    protected NavMeshAgent entity_agent;

    protected bool has_destination;
    protected float speed = 0;

    protected Vector3 previousPosition;
    protected float current_speed;

    bool is_moving = false;

    // Start is called before the first frame update
    void Start()
    {
        entity_animator = this.GetComponent<Animator>();
        entity_agent = this.GetComponent<NavMeshAgent>();
    }

    bool RandomPoint(Vector3 center, float range, out Vector3 result)
    {
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * range;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }
        result = Vector3.zero;
        return false;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 curMove = transform.position - previousPosition;
        current_speed = curMove.magnitude / Time.deltaTime;
        previousPosition = transform.position;

        /*

        if (!is_moving)
        {
            StartCoroutine("PassiveMove");
        }

        */

        entity_animator.SetFloat("Speed", current_speed);

    }

    IEnumerator PassiveMove()
    {
        is_moving = true;
        GetRandomLoc();
        yield return new WaitForSeconds(Random.Range(3, 8));
        is_moving = false;
    }

    public void GetRandomLoc()
    {
        Vector3 point;
        if (RandomPoint(transform.position, 10.0f, out point))
        {
            MoveEntity(point);
        }
    }

    public void MoveEntity(Vector3 target)
    {
        entity_agent.SetDestination(target);
    }
}
