using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    public event System.Action<CollisionEventData> OnCollision;

    void OnCollisionEnter(Collision collision)
    {
        // إنشاء بيانات التصادم
        CollisionEventData data = new CollisionEventData
        {
            bodyA = this.GetComponent<SoftBodySimple>(),
            bodyB = collision.gameObject.GetComponent<SoftBodySimple>(),
            impactPoint = collision.contacts[0].point,
            relativeVelocity = collision.relativeVelocity,
            impulseMagnitude = collision.impulse.magnitude,
            overlapDepth = collision.contacts[0].separation,
            velocityA_After = this.GetComponent<Rigidbody>().linearVelocity,
            velocityB_After = collision.rigidbody ? collision.rigidbody.linearVelocity : Vector3.zero
        };

        // إطلاق الحدث
        OnCollision?.Invoke(data);
    }
}