
// using System;
// using System.Collections.Concurrent;
// using System.Collections.Generic;
// using System.Linq;
// using System.Runtime.InteropServices;
// using System.Threading.Tasks;
// using UnityEngine;

// [RequireComponent(typeof(MeshFilter))]
// public class MassSpring : MonoBehaviour
// {
//     [Header("Shader Reference")]
//     [Tooltip("اسحب ملف SoftBodyCompute.compute إلى هنا")]
//     public ComputeShader softBodyShader;

//     public enum SoftBodyType { Permanent, Elastic, Rigid }

//     [Header("Body Type")]
//     public SoftBodyType bodyType = SoftBodyType.Elastic;

//     [Tooltip("مقدار قوة الاندفاع (Impulse) اللازم لكسر الجسم الصلب وتحويله إلى جسم قابل للتشوه")]
//     public float rigidityBreakThreshold = 500f;

//     [Header("Physics Settings")]
//     public float stiffness = 100f;
//     public float damping = 2f;
//     public float maxStretchFactor = 1.5f;
//     public float mass = 1f;
//     public Vector3 externalForce = new Vector3(0, 0, 0);

//     [Header("References")]
//     public EnvironmentParameters env;

//     [Header("Elastic Recovery")]
//     [Range(0.1f, 10f)] public float recoveryForce = 5f;
//     [Range(0.01f, 1f)] public float recoveryDamping = 0.3f;

//     [Header("Plasticity Settings")]
//     [Range(0.1f, 2f)] public float deformationSensitivity = 1f;
//     [Range(0f, 1f)] public float plasticDeformationThreshold = 0.8f;

//     [Header("Ground Collision")]
//     public float groundHeight = 0f;
//     public float bounceFactor = 0.3f;
//     public float friction = 0.9f;

//     [Header("Spring Generation")]
//     public float extraConnectionRadius = 0.2f;
//     public float spatialGridCellSize = 0.2f;
//     public int constraintIterations = 5;

//     [Header("Internal Points")]
//     [Range(0, 500)] public int internalPointsCount = 20;
//     [Range(0.01f, 1f)] public float internalPointsDensity = 0.5f;
//     public float internalSpringConnectionRadius = 0.3f;
//     public int maxInternalConnectionsPerSurfacePoint = 3;

//     [Header("Performance")]
//     [Range(1, 60)] public int meshUpdateRate = 5;

//     private struct GPUVertexPoint
//     {
//         public Vector3 current;
//         public Vector3 previous;
//         public Vector3 original;
//         public Vector3 velocity;
//         public Vector3 permanentOffset;
//         public int isFixed;
//     }

//     private struct GPUSpring
//     {
//         public int a;
//         public int b;
//         public float restLength;
//         public float originalRest;
//     }

//     private List<VertexPoint> vpoints_cpu = new List<VertexPoint>();
//     private List<Spring> springs_cpu = new List<Spring>();
//     private Mesh mesh;
//     private int[] triangles;
//     private Vector3[] originalVertices, deformedVertices;

//     private ComputeBuffer _pointBuffer;
//     private ComputeBuffer _springBuffer;
//     private int kernelForces, kernelSprings, kernelGround, kernelUpdateVelocities;
//      public int PointsCount() => vpoints_cpu.Count;
//     public int SpringsCount() => springs_cpu.Count;
//     public int TriangleCount() => triangles.Length / 3;

//     class VertexPoint
//     {
//         public Vector3 current, previous, original, permanentOffset;
//         public bool isFixed, isInternal;
//         public int idx;
//         public List<int> neighbors = new List<int>();

//         public VertexPoint(Vector3 pos, int index, bool internalPt = false)
//         {
//             original = current = previous = pos;
//             permanentOffset = Vector3.zero;
//             isInternal = internalPt;
//             isFixed = false;
//             idx = index;
//         }
//     }

//     class Spring
//     {
//         public int a, b;
//         public float restLength, originalRest;

//         public Spring(int i, int j, float rest)
//         {
//             a = i; b = j; restLength = originalRest = rest;
//         }
//     }

//     class SpatialGrid
//     {
//         float cellSize;
//         Vector3 minB, maxB;
//         ConcurrentDictionary<Vector3Int, ConcurrentBag<int>> grid =
//             new ConcurrentDictionary<Vector3Int, ConcurrentBag<int>>();

//         public SpatialGrid(float cs, Vector3 min, Vector3 max)
//         {
//             cellSize = Mathf.Max(cs, 0.001f);
//             minB = min; maxB = max;
//         }
//         public Vector3Int Key(Vector3 p) => new Vector3Int(Mathf.FloorToInt((p.x - minB.x) / cellSize), Mathf.FloorToInt((p.y - minB.y) / cellSize), Mathf.FloorToInt((p.z - minB.z) / cellSize));
//         public void Add(Vector3 p, int idx) => grid.GetOrAdd(Key(p), _ => new ConcurrentBag<int>()).Add(idx);
//         public List<int> Radius(Vector3 p, float r)
//         {
//             var center = Key(p); int cr = Mathf.CeilToInt(r / cellSize); var res = new List<int>();
//             for (int dx = -cr; dx <= cr; dx++) for (int dy = -cr; dy <= cr; dy++) for (int dz = -cr; dz <= cr; dz++)
//             {
//                 if (grid.TryGetValue(new Vector3Int(center.x + dx, center.y + dy, center.z + dz), out var bag)) res.AddRange(bag);
//             }
//             return res;
//         }
//         public void Clear() => grid.Clear();
//         public void Build(Vector3[] pts) { Clear(); Parallel.For(0, pts.Length, i => Add(pts[i], i)); }
//     }

//     private readonly object _springsLock = new object();
//     private readonly object _neighborsLock = new object();


//     void Start()
//     {
//         mesh = GetComponent<MeshFilter>().mesh;
//         mesh.MarkDynamic();

//         originalVertices = mesh.vertices;
//         deformedVertices = new Vector3[originalVertices.Length];
//         triangles = mesh.triangles;

//         GeneratePointsAndSpringsOnCPU();
//         InitializeComputeShader();
//     }

//     void FixedUpdate()
//     {
//         if (_pointBuffer == null || softBodyShader == null) return;
//         DispatchShader(Time.fixedDeltaTime);

//         if (Time.frameCount % meshUpdateRate == 0)
//         {
//             UpdateMeshFromGPU();
//         }
//     }

//     private void OnDisable()
//     {
//         _pointBuffer?.Release();
//         _pointBuffer = null;
//         _springBuffer?.Release();
//         _springBuffer = null;
//     }

//     void InitializeComputeShader()
//     {
//         if (softBodyShader == null || vpoints_cpu.Count == 0)
//         {
//             Debug.LogError("Compute Shader غير معين أو لا توجد نقاط للجسم!", this);
//             this.enabled = false;
//             return;
//         }
//         var gpuPoints = new GPUVertexPoint[vpoints_cpu.Count];
//         for (int i = 0; i < vpoints_cpu.Count; i++)
//         {
//             gpuPoints[i] = new GPUVertexPoint
//             {
//                 current = vpoints_cpu[i].current,
//                 previous = vpoints_cpu[i].previous,
//                 original = vpoints_cpu[i].original,
//                 velocity = Vector3.zero,
//                 permanentOffset = vpoints_cpu[i].permanentOffset,
//                 isFixed = vpoints_cpu[i].isFixed ? 1 : 0
//             };
//         }
//         var gpuSprings = new GPUSpring[springs_cpu.Count];
//         for (int i = 0; i < springs_cpu.Count; i++)
//         {
//             gpuSprings[i] = new GPUSpring
//             {
//                 a = springs_cpu[i].a,
//                 b = springs_cpu[i].b,
//                 restLength = springs_cpu[i].restLength,
//                 originalRest = springs_cpu[i].originalRest,
//             };
//         }

//         _pointBuffer = new ComputeBuffer(vpoints_cpu.Count, Marshal.SizeOf(typeof(GPUVertexPoint)));
//         _springBuffer = new ComputeBuffer(Mathf.Max(1, springs_cpu.Count), Marshal.SizeOf(typeof(GPUSpring)));
//         _pointBuffer.SetData(gpuPoints);
//         if (springs_cpu.Count > 0) _springBuffer.SetData(gpuSprings);

//         kernelForces = softBodyShader.FindKernel("Kernel_Forces");
//         kernelSprings = softBodyShader.FindKernel("Kernel_Springs");
//         kernelGround = softBodyShader.FindKernel("Kernel_GroundCollision");
//         kernelUpdateVelocities = softBodyShader.FindKernel("Kernel_UpdateVelocities");

//         softBodyShader.SetBuffer(kernelForces, "_PointBuffer", _pointBuffer);
//         softBodyShader.SetBuffer(kernelSprings, "_PointBuffer", _pointBuffer);
//         softBodyShader.SetBuffer(kernelSprings, "_SpringBuffer", _springBuffer);
//         softBodyShader.SetBuffer(kernelGround, "_PointBuffer", _pointBuffer);
//         softBodyShader.SetBuffer(kernelUpdateVelocities, "_PointBuffer", _pointBuffer);
//     }

//     void DispatchShader(float dt)
//     {
//         softBodyShader.SetFloat("_DeltaTime", dt);
//         softBodyShader.SetFloat("_Mass", mass);
//         softBodyShader.SetFloat("_Damping", damping);
//         softBodyShader.SetVector("_ExternalForce", externalForce);
//         softBodyShader.SetVector("_Gravity", env != null ? env.gravity : Vector3.zero);
//         softBodyShader.SetFloat("_RecoveryForce", recoveryForce);
//         softBodyShader.SetFloat("_RecoveryDamping", recoveryDamping);
//         softBodyShader.SetFloat("_PlasticityThreshold", plasticDeformationThreshold);
//         softBodyShader.SetFloat("_DeformationSensitivity", deformationSensitivity);
//         softBodyShader.SetFloat("_GroundHeight", groundHeight);
//         softBodyShader.SetFloat("_BounceFactor", bounceFactor);
//         softBodyShader.SetFloat("_Friction", friction);
//         softBodyShader.SetInt("_BodyType", (int)bodyType);

//         int pointThreadGroups = (vpoints_cpu.Count + 63) / 64;
//         int springThreadGroups = (springs_cpu.Count + 63) / 64;

//         softBodyShader.Dispatch(kernelForces, pointThreadGroups, 1, 1);
//         if (springs_cpu.Count > 0)
//         {
//             for (int i = 0; i < constraintIterations; i++)
//             {
//                 softBodyShader.Dispatch(kernelSprings, springThreadGroups, 1, 1);
//             }
//         }
//         softBodyShader.Dispatch(kernelGround, pointThreadGroups, 1, 1);
//         softBodyShader.Dispatch(kernelUpdateVelocities, pointThreadGroups, 1, 1);
//     }

//     void UpdateMeshFromGPU()
//     {
//         var gpuPoints = new GPUVertexPoint[vpoints_cpu.Count];
//         _pointBuffer.GetData(gpuPoints);

//         for (int i = 0; i < originalVertices.Length; i++)
//         {
//             deformedVertices[i] = gpuPoints[i].current;
//         }

//         mesh.vertices = deformedVertices;
//         mesh.RecalculateNormals();
//         mesh.RecalculateBounds();
//     }

//     public void ApplyCollisionResponseCPU(Vector3 worldImpactPoint, float impactRadius, Vector3 positionalCorrection, Vector3 impulse)
//     {
//         float dt = Time.fixedDeltaTime;
//         if (dt <= 0 || _pointBuffer == null || vpoints_cpu.Count == 0) return;

//         var gpuPoints = new GPUVertexPoint[vpoints_cpu.Count];
//         _pointBuffer.GetData(gpuPoints);

//         Vector3 deltaV = (this.mass > 0) ? impulse / this.mass : Vector3.zero;
//         Vector3 velocityCorrectionForVerlet = deltaV * dt;

//         if (this.bodyType == SoftBodyType.Rigid)
//         {
//             for (int i = 0; i < gpuPoints.Length; i++)
//             {
//                 if (gpuPoints[i].isFixed == 1) continue;
//                 gpuPoints[i].current += positionalCorrection;
//                 gpuPoints[i].previous += positionalCorrection - velocityCorrectionForVerlet;
//             }
//         }
//         else
//         {
//             float sqrImpactRadius = impactRadius > 0 ? impactRadius * impactRadius : 1f;
//             for (int i = 0; i < gpuPoints.Length; i++)
//             {
//                 if (gpuPoints[i].isFixed == 1) continue;
//                 Vector3 pointWorldPos = transform.TransformPoint(gpuPoints[i].current);
//                 float sqrDist = (pointWorldPos - worldImpactPoint).sqrMagnitude;
//                 if (sqrDist < sqrImpactRadius)
//                 {
//                     float falloff = 1f - (sqrDist / sqrImpactRadius);
//                     gpuPoints[i].current += positionalCorrection * falloff;
//                     gpuPoints[i].previous += (positionalCorrection - velocityCorrectionForVerlet) * falloff;
//                 }
//             }
//         }
//         _pointBuffer.SetData(gpuPoints);
//     }

//     #region CPU Point Generation
//     void GeneratePointsAndSpringsOnCPU()
//     {
//         for (int i = 0; i < originalVertices.Length; i++)
//             vpoints_cpu.Add(new VertexPoint(originalVertices[i], i, false));

//         Vector3 minB = Vector3.positiveInfinity, maxB = Vector3.negativeInfinity;
//         foreach (var v in originalVertices) { minB = Vector3.Min(minB, v); maxB = Vector3.Max(maxB, v); }

//         GenerateInternalPoints(minB, maxB);

//         var grid = new SpatialGrid(spatialGridCellSize, minB, maxB);
//         grid.Build(vpoints_cpu.Select(vp => vp.original).ToArray());

//         GenerateSurfaceSpringsParallel(grid);
//         GenerateExtraSpringsParallel(grid);
//         ConnectSurfaceInternalParallel(grid);

//         springs_cpu.Sort((x, y) => x.a != y.a ? x.a - y.a : x.b - y.b);
//     }
//     void GenerateInternalPoints(Vector3 minB, Vector3 maxB)
//     {
//         int count = Mathf.FloorToInt(internalPointsCount * internalPointsDensity);
//         if (count <= 0) return;
//         int start = vpoints_cpu.Count;
//         Vector3 size = maxB - minB;
//         var internalPointsToAdd = new ConcurrentBag<VertexPoint>();
//         Parallel.For(0, count, i =>
//         {
//             var rnd = new System.Random(i);
//             Vector3 p = new Vector3(minB.x + (float)rnd.NextDouble() * size.x, minB.y + (float)rnd.NextDouble() * size.y, minB.z + (float)rnd.NextDouble() * size.z);
//             internalPointsToAdd.Add(new VertexPoint(p, start + i, true));
//         });
//         vpoints_cpu.AddRange(internalPointsToAdd);
//     }
//     void GenerateSurfaceSpringsParallel(SpatialGrid grid)
//     {
//         var used = new ConcurrentDictionary<long, bool>(); var bags = new ConcurrentBag<ConcurrentBag<Spring>>();
//         Parallel.For(0, triangles.Length / 3, () => new ConcurrentBag<Spring>(),
//             (t, s, l) => { int i0 = t * 3; TryCollect(triangles[i0], triangles[i0 + 1], used, l); TryCollect(triangles[i0 + 1], triangles[i0 + 2], used, l); TryCollect(triangles[i0 + 2], triangles[i0], used, l); return l; },
//             l => bags.Add(l));
//         foreach (var b in bags) springs_cpu.AddRange(b);
//     }
//     void GenerateExtraSpringsParallel(SpatialGrid grid)
//     {
//         var used = new ConcurrentDictionary<long, bool>(); var bags = new ConcurrentBag<ConcurrentBag<Spring>>();
//         Parallel.For(0, vpoints_cpu.Count, () => new ConcurrentBag<Spring>(),
//             (i, s, l) => { var vp = vpoints_cpu[i]; var cands = grid.Radius(vp.original, extraConnectionRadius).Where(j => j > i).Select(j => new { j, d = (vpoints_cpu[j].original - vp.original).sqrMagnitude }).Where(x => x.d > 0.0001f && x.d <= extraConnectionRadius * extraConnectionRadius).Take(8); foreach (var x in cands) TryCollect(i, x.j, used, l); return l; },
//             l => bags.Add(l));
//         foreach (var b in bags) springs_cpu.AddRange(b);
//     }
//     void ConnectSurfaceInternalParallel(SpatialGrid grid)
//     {
//         var used = new ConcurrentDictionary<long, bool>(); int surfCount = originalVertices.Length; var bags = new ConcurrentBag<ConcurrentBag<Spring>>();
//         Parallel.For(0, surfCount, () => new ConcurrentBag<Spring>(),
//             (i, s, l) => { var vp = vpoints_cpu[i]; var cands = grid.Radius(vp.original, internalSpringConnectionRadius).Where(j => vpoints_cpu[j].isInternal).Select(j => new { j, d = (vpoints_cpu[j].original - vp.original).sqrMagnitude }).Where(x => x.d > 0.0001f && x.d <= internalSpringConnectionRadius * internalSpringConnectionRadius).OrderBy(x => x.d).Take(maxInternalConnectionsPerSurfacePoint); foreach (var x in cands) TryCollect(i, x.j, used, l); return l; },
//             l => bags.Add(l));
//         foreach (var b in bags) springs_cpu.AddRange(b);
//     }
//     private void TryCollect(int i, int j, ConcurrentDictionary<long, bool> used, ConcurrentBag<Spring> localBag)
//     {
//         int a = Mathf.Min(i, j), b = Mathf.Max(i, j); long key = ((long)a << 32) | (uint)b;
//         if (!used.TryAdd(key, true)) return;
//         float rest = Vector3.Distance(vpoints_cpu[a].original, vpoints_cpu[b].original);
//         localBag.Add(new Spring(a, b, rest));
//         lock (_neighborsLock) { if (!vpoints_cpu[a].neighbors.Contains(b)) vpoints_cpu[a].neighbors.Add(b); if (!vpoints_cpu[b].neighbors.Contains(a)) vpoints_cpu[b].neighbors.Add(a); }
//     }
//     #endregion
// }