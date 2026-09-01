using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Nox.UI
{
    [AddComponentMenu("Layout/Circle Layout")]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class CircleLayout : MonoBehaviour
    {
        private struct ChildData
        {
            public RectTransform Transform;
            public Vector2 Size;
            public Vector2 OriginalPivot;
        }

        [Header("Circle")]
        [SerializeField] private float m_Radius = 100f;
        [SerializeField] private bool m_AutoRadius = true;
        [SerializeField, Range(0f, 1f)] private float m_Fill = 1f;
        [SerializeField] private Vector2 m_CenterOffset = Vector2.zero;

        [Header("Angles")]
        [SerializeField] private float m_StartAngle = 90f;
        [SerializeField] private bool m_Clockwise = true;

        [Header("Children")]
        [SerializeField] private bool m_RotateChildren = false;

        [Header("Gizmos Debug")]
        [SerializeField] private bool m_ShowGizmos = true;
        [SerializeField] private Color m_ColorBoundingBox = Color.green;
        [SerializeField] private Color m_ColorDistanceCentre = Color.cyan;
        [SerializeField] private Color m_ColorDistanceVoisin = Color.yellow;
        [SerializeField] private bool m_AfficherDistances = true;

        private readonly DrivenRectTransformTracker m_Tracker = new DrivenRectTransformTracker();
        private RectTransform _rectTransform;
        private int _lastChildCount = -1;
        private long _lastStateHash;

        private RectTransform RectTransform => _rectTransform != null ? _rectTransform : (_rectTransform = (RectTransform)transform);

        public float Radius { get => m_Radius; set { m_Radius = Mathf.Max(0f, value); Arrange(); } }
        public bool AutoRadius { get => m_AutoRadius; set { m_AutoRadius = value; Arrange(); } }
        public float Fill { get => m_Fill; set { m_Fill = Mathf.Clamp01(value); Arrange(); } }
        public Vector2 CenterOffset { get => m_CenterOffset; set { m_CenterOffset = value; Arrange(); } }
        public float StartAngle { get => m_StartAngle; set { m_StartAngle = value; Arrange(); } }
        public bool Clockwise { get => m_Clockwise; set { m_Clockwise = value; Arrange(); } }
        public bool RotateChildren { get => m_RotateChildren; set { m_RotateChildren = value; Arrange(); } }

        private void OnEnable() { _rectTransform = (RectTransform)transform; Arrange(); }
        private void OnDisable() => m_Tracker.Clear();
        private void OnValidate() => Arrange();
        private void OnRectTransformDimensionsChange() { if (isActiveAndEnabled) Arrange(); }
        private void OnTransformChildrenChanged() { if (isActiveAndEnabled) Arrange(); }
        private void Update() { if (NeedsArrange()) Arrange(); }

        [ContextMenu("Arrange")]
        public void Arrange()
        {
            m_Tracker.Clear();

            List<RectTransform> rawChildren = GetActiveChildren();
            int n = rawChildren.Count;
            if (n == 0) return;

            List<ChildData> children = SnapshotChildrenData(rawChildren);
            SolveAndApplyLayout(children, n);

            _lastChildCount = RectTransform.childCount;
            _lastStateHash = ComputeStateHash();
        }

        private List<ChildData> SnapshotChildrenData(List<RectTransform> rawChildren)
        {
            int n = rawChildren.Count;
            List<ChildData> list = new List<ChildData>(n);
            for (int i = 0; i < n; i++)
            {
                RectTransform c = rawChildren[i];
                if (c.GetComponent<ContentSizeFitter>() != null || c.GetComponent<ILayoutGroup>() != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(c);
                }

                list.Add(new ChildData
                {
                    Transform = c,
                    Size = c.rect.size,
                    OriginalPivot = c.pivot
                });
            }
            return list;
        }

        private void SolveAndApplyLayout(List<ChildData> children, int n)
        {
            float targetRadius = GetRadiusBase();

            if (n == 1)
            {
                ApplyChildTransform(children[0], m_StartAngle, targetRadius);
                return;
            }

            if (n == 2)
            {
                float step = (m_Fill >= 1f) ? 180f : (360f * m_Fill);
                float angle0 = m_StartAngle;
                float angle1 = m_StartAngle + (m_Clockwise ? -step : step);
                ApplyChildTransform(children[0], angle0, targetRadius);
                ApplyChildTransform(children[1], angle1, targetRadius);
                return;
            }

            float minGap = -500f;
            float maxGap = 2000f;
            float[] finalAngles = new float[n];
            float[] bestAngles = new float[n];

            bool isFullCircle = m_Fill >= 1f;
            float targetSpan = 360f * m_Fill;

            for (int iter = 0; iter < 50; iter++)
            {
                float candidateGap = (minGap + maxGap) * 0.5f;

                finalAngles[0] = m_StartAngle;
                float accumulatedAngle = 0f;
                bool overflow = false;

                for (int i = 0; i < n - 1; i++)
                {
                    float currentAngle = finalAngles[i];
                    float nextAngle = FindNextAngle(children[i], currentAngle, children[i + 1], candidateGap, targetRadius);

                    float angleStep = Mathf.Abs(nextAngle - currentAngle);
                    accumulatedAngle += angleStep;

                    if (accumulatedAngle >= (isFullCircle ? 360f : targetSpan))
                    {
                        overflow = true;
                        for (int j = i + 1; j < n; j++) finalAngles[j] = nextAngle;
                        break;
                    }

                    finalAngles[i + 1] = nextAngle;
                }

                if (overflow)
                {
                    maxGap = candidateGap;
                }
                else if (isFullCircle)
                {
                    GetChildPositionAndCorners(children[n - 1], finalAngles[n - 1], targetRadius, out _, out Vector3[] cornersLast);
                    GetChildPositionAndCorners(children[0], finalAngles[0], targetRadius, out _, out Vector3[] cornersFirst);

                    float closingGap = GetMinDistanceBetweenRects(cornersLast, cornersFirst, out _, out _);

                    if (closingGap > candidateGap)
                        minGap = candidateGap;
                    else
                        maxGap = candidateGap;
                }
                else
                {
                    if (accumulatedAngle > targetSpan)
                        maxGap = candidateGap;
                    else
                        minGap = candidateGap;
                }

                System.Array.Copy(finalAngles, bestAngles, n);
            }

            for (int i = 0; i < n; i++)
            {
                ApplyChildTransform(children[i], bestAngles[i], targetRadius);
            }
        }

        private float FindNextAngle(ChildData a, float angleA, ChildData b, float targetGap, float radius)
        {
            GetChildPositionAndCorners(a, angleA, radius, out _, out Vector3[] cornersA);

            float low = 0.001f;
            float high = 180f;

            for (int k = 0; k < 20; k++)
            {
                float midStep = (low + high) * 0.5f;
                float candidateAngleB = angleA + (m_Clockwise ? -midStep : midStep);

                GetChildPositionAndCorners(b, candidateAngleB, radius, out _, out Vector3[] cornersB);

                float dist = GetMinDistanceBetweenRects(cornersA, cornersB, out _, out _);

                if (dist > targetGap)
                    high = midStep;
                else
                    low = midStep;
            }

            float step = (low + high) * 0.5f;
            return angleA + (m_Clockwise ? -step : step);
        }

        private Vector2 GetChildPositionAndCorners(ChildData child, float angleDeg, float radius, out Quaternion rot, out Vector3[] corners)
        {
            float normalizedAngle = (angleDeg % 360f + 360f) % 360f;
            Vector2 dir = DirectionOf(normalizedAngle);
            rot = RotationFor(dir);
            Vector2 pivot = m_RotateChildren ? child.OriginalPivot : new Vector2(0.5f, 0.5f);
            Vector2 localCenter = RectTransform.rect.center + m_CenterOffset;

            float inwardOffset = CalculateInward(child.Size, pivot, rot, dir);
            Vector2 position = localCenter + dir * (radius + inwardOffset);

            Vector2[] bodyCorners = GetBodyCornersFromSize(child.Size, pivot, rot);
            corners = new Vector3[4];
            for (int k = 0; k < 4; k++)
            {
                corners[k] = (Vector3)bodyCorners[k] + (Vector3)position;
            }

            return position;
        }

        private void ApplyChildTransform(ChildData childData, float angleDeg, float radius)
        {
            RectTransform child = childData.Transform;
            float normalizedAngle = (angleDeg % 360f + 360f) % 360f;

            Vector2 pivot = m_RotateChildren ? childData.OriginalPivot : new Vector2(0.5f, 0.5f);
            child.pivot = pivot;

            Vector2 position = GetChildPositionAndCorners(childData, normalizedAngle, radius, out Quaternion rot, out _);

            child.anchorMin = Vector2.one * 0.5f;
            child.anchorMax = Vector2.one * 0.5f;

            DrivenTransformProperties driven = DrivenTransformProperties.Anchors | DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.Rotation | DrivenTransformProperties.Pivot;
            m_Tracker.Add(this, child, driven);

            child.anchoredPosition = position;
            child.localRotation = rot;
        }

        private float CalculateInward(Vector2 size, Vector2 pivot, Quaternion rot, Vector2 dir)
        {
            Vector2[] corners = GetBodyCornersFromSize(size, pivot, rot);
            float inward = 0f;
            for (int k = 0; k < corners.Length; k++)
            {
                inward = Mathf.Max(inward, -Vector2.Dot(corners[k], dir));
            }
            return inward;
        }

        private static Vector2[] GetBodyCornersFromSize(Vector2 size, Vector2 pivot, Quaternion rotation)
        {
            Vector2[] corners = {
                new Vector2(-pivot.x * size.x, -pivot.y * size.y),            // BL
                new Vector2((1f - pivot.x) * size.x, -pivot.y * size.y),      // BR
                new Vector2((1f - pivot.x) * size.x, (1f - pivot.y) * size.y), // TR
                new Vector2(-pivot.x * size.x, (1f - pivot.y) * size.y)       // TL
            };
            if (rotation != Quaternion.identity)
            {
                for (int k = 0; k < corners.Length; k++) corners[k] = rotation * corners[k];
            }
            return corners;
        }

        private float GetRadiusBase()
        {
            if (m_AutoRadius)
            {
                Rect rect = RectTransform.rect;
                return Mathf.Min(rect.width * 0.5f, rect.height * 0.5f) * Mathf.Clamp01(m_Fill);
            }
            return Mathf.Max(0f, m_Radius);
        }

        private List<RectTransform> GetActiveChildren()
        {
            List<RectTransform> children = new List<RectTransform>();
            for (int i = 0; i < RectTransform.childCount; i++)
            {
                RectTransform c = RectTransform.GetChild(i) as RectTransform;
                if (c != null && c.gameObject.activeInHierarchy) children.Add(c);
            }
            return children;
        }

        private static Vector2 DirectionOf(float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        private Quaternion RotationFor(Vector2 dir)
        {
            if (!m_RotateChildren) return Quaternion.identity;
            float upAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            return Quaternion.Euler(0f, 0f, upAngle);
        }

        private bool NeedsArrange()
        {
            if (RectTransform.childCount != _lastChildCount) return true;
            return ComputeStateHash() != _lastStateHash;
        }

        private long ComputeStateHash()
        {
            long hash = 17;
            for (int i = 0; i < RectTransform.childCount; i++)
            {
                var child = RectTransform.GetChild(i) as RectTransform;
                if (child == null) continue;
                Vector2 size = child.rect.size;
                hash = hash * 31 + (long)(size.x * 100f);
                hash = hash * 31 + (long)(size.y * 100f);
                hash = hash * 31 + (child.gameObject.activeInHierarchy ? 1 : 0);
            }
            return hash;
        }

        private Vector3[] GetChildLocalCorners(RectTransform child)
        {
            Vector3[] localCorners = new Vector3[4];
            Vector2 size = child.rect.size;
            Vector2 pivot = child.pivot;

            Vector3 p0 = new Vector3(-pivot.x * size.x, -pivot.y * size.y, 0f);            // BL
            Vector3 p1 = new Vector3((1f - pivot.x) * size.x, -pivot.y * size.y, 0f);      // BR
            Vector3 p2 = new Vector3((1f - pivot.x) * size.x, (1f - pivot.y) * size.y, 0f); // TR
            Vector3 p3 = new Vector3(-pivot.x * size.x, (1f - pivot.y) * size.y, 0f);       // TL

            Quaternion rot = child.localRotation;
            Vector3 pos = child.anchoredPosition;

            localCorners[0] = rot * p0 + pos;
            localCorners[1] = rot * p1 + pos;
            localCorners[2] = rot * p2 + pos;
            localCorners[3] = rot * p3 + pos;

            return localCorners;
        }

        private Vector3 GetClosestPointOnRect(Vector3[] corners, Vector3 targetPoint)
        {
            Vector3 closest = corners[0];
            float minDistance = float.MaxValue;
            for (int i = 0; i < 4; i++)
            {
                Vector3 p1 = corners[i];
                Vector3 p2 = corners[(i + 1) % 4];
                Vector3 pointOnSegment = ClosestPointOnSegment(p1, p2, targetPoint);
                float dist = Vector3.Distance(targetPoint, pointOnSegment);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = pointOnSegment;
                }
            }
            return closest;
        }

        private float GetMinDistanceBetweenRects(Vector3[] cornersA, Vector3[] cornersB, out Vector3 pointA, out Vector3 pointB)
        {
            float overlapDepth = GetOverlapDepthSAT(cornersA, cornersB);
            if (overlapDepth > 0f)
            {
                pointA = cornersA[0];
                pointB = cornersB[0];
                return -overlapDepth;
            }

            float minDistance = float.MaxValue;
            pointA = cornersA[0];
            pointB = cornersB[0];

            for (int i = 0; i < 4; i++)
            {
                Vector3 edgeA1 = cornersA[i];
                Vector3 edgeA2 = cornersA[(i + 1) % 4];

                for (int j = 0; j < 4; j++)
                {
                    Vector3 edgeB1 = cornersB[j];
                    Vector3 edgeB2 = cornersB[(j + 1) % 4];

                    Vector3 ptB = ClosestPointOnSegment(edgeB1, edgeB2, edgeA1);
                    float d1 = Vector3.Distance(edgeA1, ptB);
                    if (d1 < minDistance)
                    {
                        minDistance = d1;
                        pointA = edgeA1;
                        pointB = ptB;
                    }

                    Vector3 ptA = ClosestPointOnSegment(edgeA1, edgeA2, edgeB1);
                    float d2 = Vector3.Distance(edgeB1, ptA);
                    if (d2 < minDistance)
                    {
                        minDistance = d2;
                        pointA = ptA;
                        pointB = edgeB1;
                    }
                }
            }
            return minDistance;
        }

        private float GetOverlapDepthSAT(Vector3[] polyA, Vector3[] polyB)
        {
            float minOverlap = float.MaxValue;

            Vector3[] axes = new Vector3[]
            {
                Vector3.Cross(polyA[1] - polyA[0], Vector3.forward).normalized,
                Vector3.Cross(polyA[2] - polyA[1], Vector3.forward).normalized,
                Vector3.Cross(polyB[1] - polyB[0], Vector3.forward).normalized,
                Vector3.Cross(polyB[2] - polyB[1], Vector3.forward).normalized
            };

            foreach (Vector3 axis in axes)
            {
                if (axis == Vector3.zero) continue;

                ProjectPolygon(polyA, axis, out float minA, out float maxA);
                ProjectPolygon(polyB, axis, out float minB, out float maxB);

                if (minA >= maxB || minB >= maxA)
                {
                    return 0f;
                }

                float overlap = Mathf.Min(maxA - minB, maxB - minA);
                if (overlap < minOverlap)
                {
                    minOverlap = overlap;
                }
            }

            return minOverlap;
        }

        private static void ProjectPolygon(Vector3[] poly, Vector3 axis, out float min, out float max)
        {
            min = Vector3.Dot(poly[0], axis);
            max = min;
            for (int i = 1; i < poly.Length; i++)
            {
                float val = Vector3.Dot(poly[i], axis);
                if (val < min) min = val;
                if (val > max) max = val;
            }
        }

        private static Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a;
            float t = Vector3.Dot(p - a, ab) / Vector3.Dot(ab, ab);
            return a + Mathf.Clamp01(t) * ab;
        }

        private void OnDrawGizmos()
        {
            if (!m_ShowGizmos) return;

            List<RectTransform> children = GetActiveChildren();
            int n = children.Count;
            if (n == 0) return;

            Vector3 centerPosition = RectTransform.TransformPoint(RectTransform.rect.center + m_CenterOffset);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(centerPosition, 4f);

            List<Vector3[]> allCorners = new List<Vector3[]>(n);
            for (int i = 0; i < n; i++)
            {
                Vector3[] localCorners = GetChildLocalCorners(children[i]);
                Vector3[] worldCorners = new Vector3[4];
                for (int k = 0; k < 4; k++) worldCorners[k] = RectTransform.TransformPoint(localCorners[k]);
                allCorners.Add(worldCorners);
            }

            for (int i = 0; i < n; i++)
            {
                Vector3[] corners = allCorners[i];

                Gizmos.color = m_ColorBoundingBox;
                for (int k = 0; k < 4; k++)
                    Gizmos.DrawLine(corners[k], corners[(k + 1) % 4]);

                Vector3 innerPointOnRect = GetClosestPointOnRect(corners, centerPosition);
                Gizmos.color = m_ColorDistanceCentre;
                Gizmos.DrawLine(innerPointOnRect, centerPosition);
                float distToCenter = Vector3.Distance(innerPointOnRect, centerPosition);

                int neighborIndex = (i + 1) % n;
                Vector3 pointA = Vector3.zero, pointB = Vector3.zero;
                float distToNeighbor = 0f;

                if (n > 1)
                {
                    distToNeighbor = GetMinDistanceBetweenRects(corners, allCorners[neighborIndex], out pointA, out pointB);
                    Gizmos.color = m_ColorDistanceVoisin;
                    Gizmos.DrawLine(pointA, pointB);
                }

#if UNITY_EDITOR
                if (m_AfficherDistances)
                {
                    Vector3 textPos = children[i].position;
                    string labelText = $"Centre: {distToCenter:F2}px";
                    if (n > 1) labelText += $"\nVoisin: {distToNeighbor:F2}px";

                    GUIStyle style = new GUIStyle();
                    style.normal.textColor = Color.white;
                    style.fontSize = 11;
                    style.fontStyle = FontStyle.Bold;

                    Handles.Label(textPos, labelText, style);
                }
#endif
            }
        }

#if UNITY_EDITOR
        [MenuItem("Tools/Nox UI/Auditer CircleLayout (Log)", false, 10)]
        private static void AuditCircleLayout()
        {
            CircleLayout layout = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponent<CircleLayout>()
                : FindObjectOfType<CircleLayout>();

            if (layout == null)
            {
                Debug.LogError("[CircleLayout Audit] Aucun composant CircleLayout trouvé !");
                return;
            }

            List<RectTransform> children = layout.GetActiveChildren();
            int n = children.Count;
            if (n == 0) return;

            Vector3 localCenter = layout.RectTransform.rect.center + layout.m_CenterOffset;

            List<Vector3[]> allLocalCorners = new List<Vector3[]>(n);
            for (int i = 0; i < n; i++)
                allLocalCorners.Add(layout.GetChildLocalCorners(children[i]));

            System.Text.StringBuilder report = new System.Text.StringBuilder();
            report.AppendLine($"=== 📊 RAPPORT D'AUDIT EXPLICITE : '{layout.gameObject.name}' ({n} éléments) ===");

            float tolerance = 1.0f; // Tolérance adaptée aux arrondis sub-pixels de l'UI
            float sumCenterDist = 0f;
            float sumGapDist = 0f;

            float[] centerDistances = new float[n];
            float[] gapDistances = new float[n];
            Vector2[] relPositions = new Vector2[n];
            bool overlapDetected = false;

            int numGaps = (layout.m_Fill >= 1f) ? n : n - 1;

            for (int i = 0; i < n; i++)
            {
                Vector3 childPos = children[i].anchoredPosition;
                relPositions[i] = (Vector2)childPos - (Vector2)localCenter;

                Vector3 dirFromCenter = (childPos - localCenter).normalized;
                Vector3 innerPoint = layout.GetClosestPointOnRect(allLocalCorners[i], localCenter);
                
                if (!layout.m_RotateChildren)
                {
                    float inward = layout.CalculateInward(children[i].rect.size, children[i].pivot, children[i].localRotation, dirFromCenter);
                    centerDistances[i] = Vector3.Distance(childPos - (Vector3)(dirFromCenter * inward), localCenter);
                }
                else
                {
                    centerDistances[i] = Vector3.Distance(innerPoint, localCenter);
                }

                sumCenterDist += centerDistances[i];

                if (n > 1 && i < numGaps)
                {
                    int nextIdx = (i + 1) % n;
                    gapDistances[i] = layout.GetMinDistanceBetweenRects(allLocalCorners[i], allLocalCorners[nextIdx], out _, out _);
                    sumGapDist += gapDistances[i];
                }
            }

            float avgCenterDist = sumCenterDist / n;
            float avgGapDist = numGaps > 0 ? sumGapDist / numGaps : 0f;

            for (int i = 0; i < n; i++)
            {
                int nextIdx = (i + 1) % n;
                report.AppendLine($"• [{i}] '{children[i].name}' ({children[i].rect.width:F1}×{children[i].rect.height:F1}px)");
                report.AppendLine($"   ├── Position / Centre : ({relPositions[i].x:+0.0;-0.0}px, {relPositions[i].y:+0.0;-0.0}px)");
                report.AppendLine($"   ├── Distance Bord → Centre : {centerDistances[i]:F2}px (Écart moy: {centerDistances[i] - avgCenterDist:+0.00;-0.00}px)");
                
                if (n > 1 && (i < numGaps))
                    report.AppendLine($"   ├── Distance Bord → Voisin Direct [{nextIdx}] : {gapDistances[i]:F2}px (Écart moy: {gapDistances[i] - avgGapDist:+0.00;-0.00}px)");

                if (n > 3)
                {
                    for (int j = i + 2; j < n; j++)
                    {
                        if (i == 0 && j == n - 1) continue;
                        float nonAdjDist = layout.GetMinDistanceBetweenRects(allLocalCorners[i], allLocalCorners[j], out _, out _);
                        report.AppendLine($"   └── Distance Non-Adjacents [{i}] ↔ [{j}] : {nonAdjDist:F2}px");
                        if (nonAdjDist < 0f) overlapDetected = true;
                    }
                }
            }

            report.AppendLine("-------------------------------------------------------------------------");
            bool hasErrors = overlapDetected;

            for (int i = 0; i < n; i++)
            {
                if (Mathf.Abs(centerDistances[i] - avgCenterDist) > tolerance) hasErrors = true;
                if (n > 1 && i < numGaps && Mathf.Abs(gapDistances[i] - avgGapDist) > tolerance) hasErrors = true;
            }

            if (overlapDetected)
                report.AppendLine("❌ [CHEVAUCHEMENT DÉTECTÉ] Des éléments non-adjacents se touchent ou se croisent !");

            if (!hasErrors)
                report.AppendLine("✅ [TOUS LES AXIOMES SONT STRICTEMENT RESPECTÉS]");

            if (hasErrors)
                Debug.LogError(report.ToString(), layout.gameObject);
            else
                Debug.Log(report.ToString(), layout.gameObject);
        }
#endif
    }
}