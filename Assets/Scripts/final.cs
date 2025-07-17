using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MassSpring : MonoBehaviour
{
    public enum SoftBodyType { Permanent, Elastic, Rigid }

    [Header("Body Type")]
    public SoftBodyType bodyType = SoftBodyType.Elastic;

    [Tooltip("مقدار قوة الاندفاع (Impulse) اللازم لكسر الجسم الصلب وتحويله إلى جسم قابل للتشوه")]
    public float rigidityBreakThreshold = 3000f;

    [Header("Physics Settings")]
    public float stiffness = 100f;
    public float damping = 2f;
    public float maxStretchFactor = 1.5f;
    public float mass = 1f;
    public Vector3 externalForce = new Vector3(0, 0, 0);

    [Header("References")]
    public EnvironmentParameters env;

    [Header("Elastic Recovery")]
    [Range(0.1f, 10f)] public float recoveryForce = 5f;
    [Range(0.01f, 1f)] public float recoveryDamping = 0.3f;

    [Header("Plasticity Settings")]
    [Range(0.1f, 2f)] public float deformationSensitivity = 1f;
    [Range(0f, 1f)] public float plasticDeformationThreshold = 0.8f;

    [Header("Ground Collision")]
    public float groundHeight = 0f;
    public float bounceFactor = 0.3f;
    public float friction = 0.9f;

    [Header("Spring Generation")]
    public float extraConnectionRadius = 0.2f;
    public float spatialGridCellSize = 0.2f;
    public int constraintIterations = 5;

    [Header("Internal Points")]
    [Range(0, 500)] public int internalPointsCount = 20;
    [Range(0.01f, 1f)] public float internalPointsDensity = 0.5f;
    public float internalSpringConnectionRadius = 0.3f;
    public int maxInternalConnectionsPerSurfacePoint = 3;

    [Header("Performance")]
    public bool useParallelProcessing = true;
    public int threadCount = 8;
    [Range(1, 60)] public int meshUpdateRate = 5;

    private List<VertexPoint> vpoints = new List<VertexPoint>();
    private List<Spring> springs = new List<Spring>();
    private int[] triangles;
    private Mesh mesh;
    private Vector3[] originalVertices, deformedVertices;

    public int PointsCount() => vpoints.Count;
    public int SpringsCount() => springs.Count;
    public int TriangleCount() => triangles.Length / 3;

    private Vector3 centerOfMassLocal;
    private Vector3 averageVelocityLocal;

    public Vector3 CenterOfMass => transform.TransformPoint(centerOfMassLocal);
    public Vector3 AverageVelocity => transform.TransformDirection(averageVelocityLocal);

    private readonly object _springsLock = new object();
    private readonly object _neighborsLock = new object();

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        mesh.MarkDynamic();

        originalVertices = mesh.vertices;
        deformedVertices = new Vector3[originalVertices.Length];
        triangles = mesh.triangles;

        for (int i = 0; i < originalVertices.Length; i++)
            vpoints.Add(new VertexPoint(originalVertices[i], i));

        Vector3 minB = Vector3.positiveInfinity, maxB = Vector3.negativeInfinity;
        foreach (var v in originalVertices)
        {
            minB = Vector3.Min(minB, v);
            maxB = Vector3.Max(maxB, v);
        }

        GenerateInternalPoints(minB, maxB);

        var grid = new SpatialGrid(spatialGridCellSize, minB, maxB);
        grid.Build(vpoints.Select(vp => vp.original).ToArray());

        GenerateSurfaceSpringsParallel(grid);
        GenerateExtraSpringsParallel(grid);
        ConnectSurfaceInternalParallel(grid);

        springs.Sort((x, y) => x.a != y.a ? x.a - y.a : x.b - y.b);
    }

    void GenerateInternalPoints(Vector3 minB, Vector3 maxB)
    {
        int count = Mathf.FloorToInt(internalPointsCount * internalPointsDensity);
        if (count <= 0) return;
        int start = vpoints.Count;
        Vector3 size = maxB - minB;

        int[] seeds = new int[count];
        for (int i = 0; i < count; i++)
            seeds[i] = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        var internalPointsToAdd = new ConcurrentBag<VertexPoint>();
        Parallel.For(0, count, i =>
        {
            var rnd = new System.Random(seeds[i]);
            Vector3 p = new Vector3(
                minB.x + (float)rnd.NextDouble() * size.x,
                minB.y + (float)rnd.NextDouble() * size.y,
                minB.z + (float)rnd.NextDouble() * size.z
            );
            internalPointsToAdd.Add(new VertexPoint(p, start + i, true));
        });

        vpoints.AddRange(internalPointsToAdd);
    }

    void GenerateSurfaceSpringsParallel(SpatialGrid grid)
    {
        var used = new ConcurrentDictionary<long, bool>();
        var bags = new ConcurrentBag<ConcurrentBag<Spring>>();

        Parallel.For(0, triangles.Length / 3,
            () => new ConcurrentBag<Spring>(),
            (t, state, localBag) =>
            {
                int i0 = t * 3;
                TryCollect(triangles[i0], triangles[i0 + 1], used, localBag);
                TryCollect(triangles[i0 + 1], triangles[i0 + 2], used, localBag);
                TryCollect(triangles[i0 + 2], triangles[i0], used, localBag);
                return localBag;
            },
            localBag => bags.Add(localBag)
        );

        foreach (var bag in bags) springs.AddRange(bag);
    }

    void GenerateExtraSpringsParallel(SpatialGrid grid)
    {
        var used = new ConcurrentDictionary<long, bool>();
        var bags = new ConcurrentBag<ConcurrentBag<Spring>>();

        Parallel.For(0, vpoints.Count,
            () => new ConcurrentBag<Spring>(),
            (i, state, localBag) =>
            {
                var vp = vpoints[i];
                var candidates = grid.Radius(vp.original, extraConnectionRadius)
                                     .Where(j => j > i)
                                     .Select(j => new { j, d = (vpoints[j].original - vp.original).sqrMagnitude })
                                     .Where(x => x.d > 0 && x.d <= extraConnectionRadius * extraConnectionRadius)
                                     .Take(8);

                foreach (var x in candidates)
                    TryCollect(i, x.j, used, localBag);

                return localBag;
            },
            localBag => bags.Add(localBag)
        );

        foreach (var bag in bags) springs.AddRange(bag);
    }

    void ConnectSurfaceInternalParallel(SpatialGrid grid)
    {
        var used = new ConcurrentDictionary<long, bool>();
        int surfCount = originalVertices.Length;
        var bags = new ConcurrentBag<ConcurrentBag<Spring>>();

        Parallel.For(0, surfCount,
            () => new ConcurrentBag<Spring>(),
            (i, state, localBag) =>
            {
                var vp = vpoints[i];
                var candidates = grid.Radius(vp.original, internalSpringConnectionRadius)
                                     .Where(j => vpoints[j].isInternal)
                                     .Select(j => new { j, d = (vpoints[j].original - vp.original).sqrMagnitude })
                                     .Where(x => x.d > 0 && x.d <= internalSpringConnectionRadius * internalSpringConnectionRadius)
                                     .OrderBy(x => x.d)
                                     .Take(maxInternalConnectionsPerSurfacePoint);

                foreach (var x in candidates)
                    TryCollect(i, x.j, used, localBag);

                return localBag;
            },
            localBag => bags.Add(localBag)
        );

        foreach (var bag in bags) springs.AddRange(bag);
    }

    private void TryCollect(int i, int j, ConcurrentDictionary<long, bool> used, ConcurrentBag<Spring> localBag)
    {
        int a = Mathf.Min(i, j), b = Mathf.Max(i, j);
        long key = ((long)a << 32) | (uint)b;
        if (!used.TryAdd(key, true)) return;

        float rest = Vector3.Distance(vpoints[a].original, vpoints[b].original);
        var sp = new Spring(a, b, rest, maxStretchFactor);
        localBag.Add(sp);

        lock (_neighborsLock)
        {
            if (!vpoints[a].neighbors.Contains(b)) vpoints[a].neighbors.Add(b);
            if (!vpoints[b].neighbors.Contains(a)) vpoints[b].neighbors.Add(a);
        }
    }


    void FixedUpdate()
    {
        if (vpoints.Count == 0) return;
        float dt = Time.fixedDeltaTime;

        SimulateStep(dt);
        UpdateBodyMetrics(dt);

        if (Time.frameCount % meshUpdateRate == 0)
        {
            UpdateMesh();
        }
    }

    void UpdateBodyMetrics(float dt)
    {
        if (vpoints.Count == 0 || dt <= 0) return;

        Vector3 com = Vector3.zero;
        Vector3 vel = Vector3.zero;
        foreach (var vp in vpoints)
        {
            com += vp.current;
            vel += (vp.current - vp.previous);
        }

        centerOfMassLocal = com / vpoints.Count;
        averageVelocityLocal = (vel / vpoints.Count) / dt;
    }

    void SimulateStep(float dt)
    {
        if (useParallelProcessing)
            SimulateStepParallel(dt);
        else
            SimulateStepSingleThread(dt);
    }

    void SimulateStepParallel(float dt)
    {
        Parallel.ForEach(vpoints, vp =>
        {
            if (vp.isFixed) return;

            Vector3 force = externalForce * mass - damping * vp.velocity;

            if (env != null)
            {
                force += env.gravity * mass;
                Vector3 v = vp.velocity;
                if (v.sqrMagnitude > 1e-4f)
                {
                    Vector3 dragForce = -0.5f * env.airDensity * v.sqrMagnitude * env.dragCoefficient * env.crossSectionalArea * v.normalized;
                    force += dragForce * (1 + env.humidityFactor);
                }
            }

            if (bodyType == SoftBodyType.Elastic)
            {
                Vector3 rec = (vp.original + vp.permanentOffset - vp.current);
                force += rec * recoveryForce - vp.velocity * recoveryDamping;
            }

            Vector3 acceleration = (mass > 0) ? force / mass : Vector3.zero;
            Vector3 tmp = vp.current;
            vp.current += vp.current - vp.previous + acceleration * dt * dt;
            vp.previous = tmp;
        });

        for (int iter = 0; iter < constraintIterations; iter++)
        {
            Parallel.ForEach(springs, s =>
            {
                var A = vpoints[s.a];
                var B = vpoints[s.b];
                Vector3 delta = B.current - A.current;
                float len = delta.magnitude;
                if (len < 1e-5f) return;
                float diff = len - s.restLength;
                s.stress = Mathf.Abs(diff) / s.originalRest;

                if (bodyType == SoftBodyType.Permanent && s.stress > plasticDeformationThreshold)
                {
                    float plast = (s.stress - plasticDeformationThreshold) * deformationSensitivity;
                    s.restLength = Mathf.Lerp(s.restLength, len, plast * dt);
                }

                Vector3 corr = delta.normalized * diff * 0.5f;
                if (!A.isFixed) A.current += corr;
                if (!B.isFixed) B.current -= corr;
            });
        }

        Matrix4x4 l2w = transform.localToWorldMatrix;
        Matrix4x4 w2l = transform.worldToLocalMatrix;
        Parallel.ForEach(vpoints, vp =>
        {
            if (vp.isFixed) return;
            Vector3 wcur = l2w.MultiplyPoint3x4(vp.current);
            if (wcur.y < groundHeight)
            {
                Vector3 wprev = l2w.MultiplyPoint3x4(vp.previous);
                Vector3 wvel = (wcur - wprev) / dt;

                wcur.y = groundHeight;
                wvel.y *= -bounceFactor;
                wvel.x *= friction;
                wvel.z *= friction;

                vp.current = w2l.MultiplyPoint3x4(wcur);
                vp.previous = w2l.MultiplyPoint3x4(wcur - wvel * dt);
            }
        });

        Parallel.ForEach(vpoints, vp =>
        {
            if (!vp.isFixed)
                vp.velocity = (vp.current - vp.previous) / dt;
        });
    }
    

    void SimulateStepSingleThread(float dt)
{
  
    foreach (var vp in vpoints)
    {
        if (vp.isFixed) continue;

        Vector3 force = externalForce * mass - damping * vp.velocity;

        if (env != null)
        {
            force += env.gravity * mass;
            Vector3 v = vp.velocity;
            if (v.sqrMagnitude > 1e-4f)
            {
                Vector3 dragForce = -0.5f * env.airDensity * v.sqrMagnitude * env.dragCoefficient * env.crossSectionalArea * v.normalized;
                force += dragForce * (1 + env.humidityFactor);
            }
        }

        if (bodyType == SoftBodyType.Elastic)
        {
            Vector3 rec = (vp.original + vp.permanentOffset - vp.current);
            force += rec * recoveryForce - vp.velocity * recoveryDamping;
        }

        Vector3 acceleration = (mass > 0) ? force / mass : Vector3.zero;
        Vector3 tmp = vp.current;
        vp.current += vp.current - vp.previous + acceleration * dt * dt;
        vp.previous = tmp;
    }


    for (int iter = 0; iter < constraintIterations; iter++)
    {
        foreach (var s in springs)
        {
            var A = vpoints[s.a];
            var B = vpoints[s.b];
            Vector3 delta = B.current - A.current;
            float len = delta.magnitude;
            if (len < 1e-5f) continue;
            float diff = len - s.restLength;
            s.stress = Mathf.Abs(diff) / s.originalRest;

            if (bodyType == SoftBodyType.Permanent && s.stress > plasticDeformationThreshold)
            {
                float plast = (s.stress - plasticDeformationThreshold) * deformationSensitivity;
                s.restLength = Mathf.Lerp(s.restLength, len, plast * dt);
            }

            Vector3 corr = delta.normalized * diff * 0.5f;
            if (!A.isFixed) A.current += corr;
            if (!B.isFixed) B.current -= corr;
        }
    }


    Matrix4x4 l2w = transform.localToWorldMatrix;
    Matrix4x4 w2l = transform.worldToLocalMatrix;
    foreach (var vp in vpoints)
    {
        if (vp.isFixed) continue;
        Vector3 wcur = l2w.MultiplyPoint3x4(vp.current);
        if (wcur.y < groundHeight)
        {
            Vector3 wprev = l2w.MultiplyPoint3x4(vp.previous);
            Vector3 wvel = (wcur - wprev) / dt;

            wcur.y = groundHeight;
            wvel.y *= -bounceFactor;
            wvel.x *= friction;
            wvel.z *= friction;

            vp.current = w2l.MultiplyPoint3x4(wcur);
            vp.previous = w2l.MultiplyPoint3x4(wcur - wvel * dt);
        }
    }


    foreach (var vp in vpoints)
    {
        if (!vp.isFixed)
            vp.velocity = (vp.current - vp.previous) / dt;
    }
}

    void UpdateMesh()
    {
        if (deformedVertices == null || vpoints.Count < originalVertices.Length) return;

        for (int i = 0; i < originalVertices.Length; i++)
            deformedVertices[i] = vpoints[i].current;

        mesh.vertices = deformedVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    public void ApplyCollisionResponse(Vector3 worldImpactPoint, float impactRadius, Vector3 positionalCorrection, Vector3 impulse)
    {
        float dt = Time.fixedDeltaTime;
        if (dt <= 0 || vpoints.Count == 0) return;

        Vector3 deltaV = (this.mass > 0) ? impulse / this.mass : Vector3.zero;
        Vector3 velocityCorrectionForVerlet = deltaV * dt;

        if (this.bodyType == SoftBodyType.Rigid)
        {
          
            foreach (var vp in vpoints)
            {
                if (vp.isFixed) continue;
                vp.current += positionalCorrection;
                vp.previous += positionalCorrection - velocityCorrectionForVerlet;
            }
        }
        else
        {
          
            float sqrImpactRadius = impactRadius > 0 ? impactRadius * impactRadius : 1f;
            foreach (var vp in vpoints)
            {
                if (vp.isFixed) continue;

                Vector3 pointWorldPos = transform.TransformPoint(vp.current);
                float sqrDist = (pointWorldPos - worldImpactPoint).sqrMagnitude;

                if (sqrDist < sqrImpactRadius)
                {
                    float falloff = 1f - (sqrDist / sqrImpactRadius);
                    
                    Vector3 pointPositionalCorrection = positionalCorrection * falloff;
                    Vector3 pointVelocityCorrection = velocityCorrectionForVerlet * falloff;
                    
                    vp.current += pointPositionalCorrection;
                    vp.previous += pointPositionalCorrection - pointVelocityCorrection;
                }
            }
        }
    }
}