﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InverseKinematicsController : MonoBehaviour
{
    [SerializeField] bool m_showGizmos;
    [SerializeField] float m_gizmoRadius = 0.2f;
    [SerializeField] Transform m_rootTransform;
    [SerializeField] Transform m_target;
    [SerializeField] RobotJoint[] Joints;
    [SerializeField] float SamplingDistance = 1f;
    public float DistanceThreshold = 0.1f;
    public float LearningRate = 10f;

    private Vector3 m_startOffsetFromRoot;
    private float m_startRootRotationY;
    private Vector3[] m_angles;
    private Vector3 testPoint;
    private Quaternion rotation;
    private float m_distanceFromTarget;


    void Awake()
    {
        m_startRootRotationY = m_rootTransform.rotation.eulerAngles.y;
        m_startOffsetFromRoot = Joints[0].transform.position - m_rootTransform.position;
    }


    void LateUpdate()
    {
        if (m_target != null)
        {
            m_angles = new Vector3[Joints.Length];

            for (int i = 0; i < m_angles.Length; i++)
                m_angles[i] = Joints[i].Angle;

            InverseKinematics(m_target.position, m_angles);

            for (int i = 0; i < Joints.Length; i++)
            {
                var joint = Joints[i];
                var angle = m_angles[i];

                joint.Angle = angle;
            }
        }
    }


    private Vector3 ForwardKinematics(Vector3[] angles)
    {
        Vector3 prevPoint = m_rootTransform.position;

        float rootRotationY = m_rootTransform.rotation.eulerAngles.y;
        float rootRotationSinceStart = rootRotationY - m_startRootRotationY;

        Quaternion rotation = Quaternion.identity;

        // This doesn't quite work perfectly because of the idle animation moves the root 
        // of the claws relative to the root transform, but it's pretty close.
        // It'll do for now untilI can work out a better way.
        rotation *= Quaternion.Euler(0, rootRotationSinceStart, 0);
        prevPoint += rotation * m_startOffsetFromRoot;

        for (int i = 1; i < Joints.Length; i++)
        {
            // Rotates around a new axis
            rotation *= Quaternion.Euler(angles[i - 1]);
            Vector3 nextPoint = prevPoint + rotation * Joints[i].StartOffset;

            prevPoint = nextPoint;
        }

        testPoint = prevPoint;

        return prevPoint;
    }


    public float FindDistanceFromTartget(Transform target)
    {
        return FindDistanceFromTarget(transform.position, m_angles);
    }


    private float FindDistanceFromTarget(Vector3 target, Vector3[] angles)
    {
        Vector3 point = ForwardKinematics(angles);
        return Vector3.Distance(point, target);
    }


    public float DistanceFromTarget
    {
        get { return m_distanceFromTarget; }
    }


    private float PartialGradient(Vector3 target, Vector3[] angles, int i)
    {
        // Saves the angle,
        // it will be restored later
        var angle = angles[i];

        // Gradient : [F(x+SamplingDistance) - F(x)] / h
        float f_x = FindDistanceFromTarget(target, angles);

        angles[i] += SamplingDistance * Joints[i].Axis;
        float f_x_plus_d = FindDistanceFromTarget(target, angles);

        float gradient = (f_x_plus_d - f_x) / SamplingDistance;

        // Restores
        angles[i] = angle;

        return gradient;
    }


    private void InverseKinematics(Vector3 target, Vector3[] angles)
    {
        m_distanceFromTarget = FindDistanceFromTarget(target, angles);

        //print(distanceFromTarget);

        if (m_distanceFromTarget < DistanceThreshold)
            return;

        for (int i = Joints.Length - 1; i >= 0; i--)
        {
            // Gradient descent
            // Update : Solution -= LearningRate * Gradient
            float gradient = PartialGradient(target, angles, i);
            angles[i] -= Joints[i].Axis * LearningRate * gradient * Time.deltaTime * 60f;

            // Clamp
            float axisAngle = AxisAngle(angles[i], Joints[i].Axis);
            axisAngle = Mathf.Clamp(axisAngle, Joints[i].MinAngle, Joints[i].MaxAngle);
            angles[i] = SetAxisAngle(axisAngle, angles[i], Joints[i].Axis);

            m_distanceFromTarget = FindDistanceFromTarget(target, angles);

            // Early termination
            if (m_distanceFromTarget < DistanceThreshold)
                return;
        }
    }


    private float AxisAngle(Vector3 rotation, Vector3 axis)
    {
        //print("rotation: " + rotation);
        var angle = Vector3.Dot(rotation, axis);
        angle = angle > 180f ? angle - 360f : angle;
        //print("angle: " + angle);
        return angle;
    }


    private Vector3 SetAxisAngle(float axisAngle, Vector3 angle, Vector3 axis)
    {
        var angleAlongAxis = axis * axisAngle;
        var otherAngles = Vector3.Scale(Vector3.one - axis, angle);

        //print("Initial angle: " + angle);
        //print("axisAngle: " + axisAngle);
        //print("axis:" + axis);
        //print("angleAlongAxis: " + angleAlongAxis);
        //print("otherAngles: " + otherAngles);

        angle = angleAlongAxis + otherAngles;

        //print("Final angle: " + angle);

        return angle;
    }


    void OnDrawGizmos()
    {
        if (!m_showGizmos)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(testPoint, m_gizmoRadius);
    }
}
