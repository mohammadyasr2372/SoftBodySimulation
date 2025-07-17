
// using System.Collections.Generic;
// using UnityEngine;

// public struct CollisionEventData
// {
//     public SoftBodySimple bodyA, bodyB;
//     public Vector3 impactPoint;
//     public Vector3 relativeVelocity;
//     public float impulseMagnitude;
//     public float overlapDepth;
//     public Vector3 velocityA_After, velocityB_After;

//     public override string ToString()
//     {
//         return $"--- Collision Event ---\n" +
//                $"Objects: {bodyA.name} & {bodyB.name}\n" +
//                $"Impact Point (World): {impactPoint}\n" +
//                $"Overlap Depth: {overlapDepth:F4} m\n" +
//                $"Relative Velocity @ Impact: {relativeVelocity.magnitude:F2} m/s ({relativeVelocity})\n" +
//                $"Impulse Applied: {impulseMagnitude:F2} Ns\n" +
//                $"Resulting Velocities: \n" +
//                $"  - {bodyA.name}: {velocityA_After.magnitude:F2} m/s ({velocityA_After})\n" +
//                $"  - {bodyB.name}: {velocityB_After.magnitude:F2} m/s ({velocityB_After})\n" +
//                $"----------------------";
//     }
// }

// [DisallowMultipleComponent]
// [RequireComponent(typeof(MassSpring))]
// public class SoftBodySimple : MonoBehaviour
// {
//     [Header("Collision Properties")]
//     [Tooltip("نصف قطر الكرة الافتراضية لهذا الجسم للاصطدام")]
//     public float collisionRadius = 1f;
//     [Tooltip("معامل الارتداد (0 = لا يوجد ارتداد, 1 = ارتداد كامل)")]
//     [Range(0, 1)] public float restitution = 0.5f;

//     private static readonly List<SoftBodySimple> allColliders = new List<SoftBodySimple>();
//     public static readonly List<CollisionEventData> LastFrameCollisions = new List<CollisionEventData>();

//     private MassSpring softBody;
//     public float Mass => (softBody != null) ? softBody.mass : 1f;

//     public Vector3 Velocity { get; private set; }
//     private Vector3 lastPosition;

//     void OnEnable()
//     {
//         allColliders.Add(this);
//         softBody = GetComponent<MassSpring>();
//         lastPosition = transform.position; // تهيئة الموضع الأولي
//     }

//     void OnDisable()
//     {
//         allColliders.Remove(this);
//     }

//     void FixedUpdate()
//     {

//         float dt = Time.fixedDeltaTime;
//         if (dt > 0)
//         {
//             Velocity = (transform.position - lastPosition) / dt;
//             lastPosition = transform.position;
//         }

//         if (allColliders.Count > 1 && allColliders[0] == this)
//         {
//             LastFrameCollisions.Clear();
//             CheckAllCollisions();
//         }
//     }

//     private static void CheckAllCollisions()
//     {
//         for (int i = 0; i < allColliders.Count; i++)
//         {
//             for (int j = i + 1; j < allColliders.Count; j++)
//             {
//                 var bodyA = allColliders[i];
//                 var bodyB = allColliders[j];

//                 Vector3 posA = bodyA.transform.position;
//                 Vector3 posB = bodyB.transform.position;
//                 Vector3 velA = bodyA.Velocity;
//                 Vector3 velB = bodyB.Velocity;

//                 float combinedRadius = bodyA.collisionRadius + bodyB.collisionRadius;
//                 Vector3 delta = posA - posB;
//                 float distSqr = delta.sqrMagnitude;

//                 if (distSqr > 0 && distSqr < combinedRadius * combinedRadius)
//                 {
//                     float dist = Mathf.Sqrt(distSqr);
//                     Vector3 normal = delta / dist;
//                     float overlap = combinedRadius - dist;
                    
//                     Vector3 impactPoint = posA - normal * bodyA.collisionRadius;

//                     float totalMass = bodyA.Mass + bodyB.Mass;
//                     float moveRatioA = (totalMass > 0) ? bodyB.Mass / totalMass : 0.5f;
//                     float moveRatioB = (totalMass > 0) ? bodyA.Mass / totalMass : 0.5f;
                    
//                     Vector3 correctionA = normal * overlap * moveRatioA;
//                     Vector3 correctionB = -normal * overlap * moveRatioB;

//                     Vector3 relativeVelocity = velA - velB;
//                     float velocityAlongNormal = Vector3.Dot(relativeVelocity, normal);

//                     Vector3 impulse = Vector3.zero;
//                     float impulseMagnitude = 0;

//                     if (velocityAlongNormal <= 0)
//                     {
//                         float e = Mathf.Min(bodyA.restitution, bodyB.restitution);
//                         impulseMagnitude = -(1 + e) * velocityAlongNormal;
//                         if (bodyA.Mass > 0 && bodyB.Mass > 0)
//                         {
//                             impulseMagnitude /= (1 / bodyA.Mass) + (1 / bodyB.Mass);
//                         }
//                         else
//                         {
//                             impulseMagnitude = 0;
//                         }
//                         impulse = impulseMagnitude * normal;
//                     }
                    
//                     if (bodyA.softBody.bodyType == MassSpring.SoftBodyType.Rigid && 
//                         impulseMagnitude > bodyA.softBody.rigidityBreakThreshold)
//                     {
//                         bodyA.softBody.bodyType = MassSpring.SoftBodyType.Permanent;
//                         Debug.LogWarning($"<color=red>BODY BROKEN:</color> {bodyA.name} has shattered! Impulse: {impulseMagnitude:F2}");
//                     }

//                     if (bodyB.softBody.bodyType == MassSpring.SoftBodyType.Rigid && 
//                         impulseMagnitude > bodyB.softBody.rigidityBreakThreshold)
//                     {
//                         bodyB.softBody.bodyType = MassSpring.SoftBodyType.Permanent;
//                         Debug.LogWarning($"<color=red>BODY BROKEN:</color> {bodyB.name} has shattered! Impulse: {impulseMagnitude:F2}");
//                     }
                    
//                     bodyA.softBody.ApplyCollisionResponseCPU(impactPoint, bodyA.collisionRadius, correctionA, impulse);
//                     bodyB.softBody.ApplyCollisionResponseCPU(impactPoint, bodyB.collisionRadius, correctionB, -impulse);
                    
//                     var eventData = new CollisionEventData
//                     {
//                         bodyA = bodyA,
//                         bodyB = bodyB,
//                         impactPoint = impactPoint,
//                         relativeVelocity = relativeVelocity,
//                         impulseMagnitude = impulseMagnitude,
//                         overlapDepth = overlap,
//                         velocityA_After = velA + ((bodyA.Mass > 0) ? (impulse / bodyA.Mass) : Vector3.zero),
//                         velocityB_After = velB - ((bodyB.Mass > 0) ? (impulse / bodyB.Mass) : Vector3.zero)
//                     };
//                     LastFrameCollisions.Add(eventData);
//                     Debug.Log(eventData.ToString());
//                 }
//             }
//         }
//     }

//     void OnDrawGizmosSelected()
//     {
//         Gizmos.color = Color.green;
//         Gizmos.DrawWireSphere(transform.position, collisionRadius);
//     }
// }