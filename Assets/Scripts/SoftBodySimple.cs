using System.Collections.Generic;
using UnityEngine;

public struct CollisionEventData
{
    public SoftBodySimple bodyA, bodyB;
    public Vector3 impactPoint;
    public Vector3 relativeVelocity;
    public float impulseMagnitude;
    public float overlapDepth;
    public Vector3 velocityA_After, velocityB_After;

    public override string ToString()
    {   string info = $"--- Collision Event ---\n" +
                      $"Objects: {bodyA.name} & {bodyB.name}\n" +
                      $"Impact Point (World): {impactPoint}\n" +
                      $"Overlap Depth: {overlapDepth:F4} m\n" +
                      $"Relative Velocity @ Impact: {relativeVelocity.magnitude:F2} m/s ({relativeVelocity})\n" +
                      $"Impulse Applied: {impulseMagnitude:F2} Ns\n" +
                      $"Resulting Velocities: \n" +
                      $"  - {bodyA.name}: {velocityA_After.magnitude:F2} m/s ({velocityA_After})\n" +
                      $"  - {bodyB.name}: {velocityB_After.magnitude:F2} m/s ({velocityB_After})\n" +
                      $"----------------------";
        
         if (CollisionInfoDisplay.Instance != null)
        {
            CollisionInfoDisplay.Instance.ShowCollisionInfo(info);
        }
        else
        {
            Debug.LogWarning("CollisionInfoDisplay غير موجود في المشهد!");
        }



        return info;
    }
}

[DisallowMultipleComponent]
[RequireComponent(typeof(MassSpring))]
public class SoftBodySimple : MonoBehaviour
{
    [Header("Collision Properties")]
    [Tooltip("نصف قطر الكرة الافتراضية لهذا الجسم للاصطدام")]
    public float collisionRadius = 1f;
    [Tooltip("معامل الارتداد (0 = لا يوجد ارتداد, 1 = ارتداد كامل)")]
    [Range(0, 1)] public float restitution = 0.5f;

    private static readonly List<SoftBodySimple> allColliders = new List<SoftBodySimple>();
    public static readonly List<CollisionEventData> LastFrameCollisions = new List<CollisionEventData>();

    private MassSpring softBody;
    public float Mass => (softBody != null) ? softBody.mass : 1f;

    void OnEnable()
    {
        allColliders.Add(this);
        softBody = GetComponent<MassSpring>();
    }

    void OnDisable()
    {
        allColliders.Remove(this);
    }

    void FixedUpdate()
    {
        if (allColliders.Count > 1 && allColliders[0] == this)
        {
            LastFrameCollisions.Clear();
            CheckAllCollisions();
        }
    }

    private static void CheckAllCollisions()
    {
        for (int i = 0; i < allColliders.Count; i++)
        {
            for (int j = i + 1; j < allColliders.Count; j++)
            {
                var bodyA = allColliders[i];
                var bodyB = allColliders[j];

                Vector3 posA = bodyA.softBody.CenterOfMass;
                Vector3 posB = bodyB.softBody.CenterOfMass;
                Vector3 velA = bodyA.softBody.AverageVelocity;
                Vector3 velB = bodyB.softBody.AverageVelocity;

                float combinedRadius = bodyA.collisionRadius + bodyB.collisionRadius;
                Vector3 delta = posA - posB;
                float distSqr = delta.sqrMagnitude;

                if (distSqr > 0 && distSqr < combinedRadius * combinedRadius)
                {
                    float dist = Mathf.Sqrt(distSqr);
                    Vector3 normal = delta / dist;
                    float overlap = combinedRadius - dist;
                    
                    Vector3 impactPoint = posA - normal * bodyA.collisionRadius;

                    float totalMass = bodyA.Mass + bodyB.Mass;
                    float moveRatioA = (totalMass > 0) ? bodyB.Mass / totalMass : 0.5f;
                    float moveRatioB = (totalMass > 0) ? bodyA.Mass / totalMass : 0.5f;
                    
                    Vector3 correctionA = normal * overlap * moveRatioA;
                    Vector3 correctionB = -normal * overlap * moveRatioB;

                    Vector3 relativeVelocity = velA - velB;
                    float velocityAlongNormal = Vector3.Dot(relativeVelocity, normal);

                    Vector3 impulse = Vector3.zero;
                    float impulseMagnitude = 0;

                    if (velocityAlongNormal <= 0)
                    {
                        float e = Mathf.Min(bodyA.restitution, bodyB.restitution);
                        impulseMagnitude = -(1 + e) * velocityAlongNormal;
                        if (bodyA.Mass > 0 && bodyB.Mass > 0)
                        {
                            impulseMagnitude /= (1 / bodyA.Mass) + (1 / bodyB.Mass);
                        }
                        else
                        {
                            impulseMagnitude = 0;
                        }
                        impulse = impulseMagnitude * normal;
                    }
                    
             
                 
                    if (bodyA.softBody.bodyType == MassSpring.SoftBodyType.Rigid && 
                        impulseMagnitude > bodyA.softBody.rigidityBreakThreshold)
                    {
                   
                        bodyA.softBody.bodyType = MassSpring.SoftBodyType.Permanent;
                        Debug.LogWarning($"<color=red>BODY BROKEN:</color> {bodyA.name} has shattered due to high impact! Impulse: {impulseMagnitude:F2}");
                    }

               
                    if (bodyB.softBody.bodyType == MassSpring.SoftBodyType.Rigid && 
                        impulseMagnitude > bodyB.softBody.rigidityBreakThreshold)
                    {
                        bodyB.softBody.bodyType = MassSpring.SoftBodyType.Permanent;
                        Debug.LogWarning($"<color=red>BODY BROKEN:</color> {bodyB.name} has shattered due to high impact! Impulse: {impulseMagnitude:F2}");
                    }
                    
             
                    bodyA.softBody.ApplyCollisionResponse(impactPoint, bodyA.collisionRadius, correctionA, impulse);
                    bodyB.softBody.ApplyCollisionResponse(impactPoint, bodyB.collisionRadius, correctionB, -impulse);
                    
                    var eventData = new CollisionEventData
                    {
                        bodyA = bodyA,
                        bodyB = bodyB,
                        impactPoint = impactPoint,
                        relativeVelocity = relativeVelocity,
                        impulseMagnitude = impulseMagnitude,
                        overlapDepth = overlap,
                        velocityA_After = velA + ((bodyA.Mass > 0) ? (impulse / bodyA.Mass) : Vector3.zero),
                        velocityB_After = velB - ((bodyB.Mass > 0) ? (impulse / bodyB.Mass) : Vector3.zero)
                    };
                    LastFrameCollisions.Add(eventData);
                    Debug.Log(eventData.ToString());
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (softBody != null && Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(softBody.CenterOfMass, collisionRadius);
        }
        else
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, collisionRadius);
        }
    }
}