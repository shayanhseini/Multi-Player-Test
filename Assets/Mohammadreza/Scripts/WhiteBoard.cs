using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.Netcode;

public class WhiteBoard : NetworkBehaviour
{
    [Header("Board")]
    public RenderTexture renderTexture;

    [Header("Brush Settings")]
    public float maxDistance = 0.2f;
    public float minBrushDistance = 2f;

    [Header("Network Drawing")]
    [SerializeField, Min(0.01f)]
    private float batchInterval = 0.04f;

    [SerializeField, Range(2, 64)]
    private int maxPointsPerBatch = 24;

    public Color backGroundColor = Color.white;

    [Range(0, 1)]
    public float markerAlpha = 0.7f;

    [Header("Collider")]
    public bool useBoxCollider = true;

    private Material brushMaterial;
    private readonly Vector2[] drawVertices = new Vector2[4];
    private readonly List<Vector2> pendingDrawPoints = new List<Vector2>(32);
    private readonly List<DrawBatchRecord> drawHistory = new List<DrawBatchRecord>();

    private Color pendingColor;
    private float pendingSizeX;
    private float pendingSizeY;
    private float pendingRotation;
    private float nextBatchSendTime;

    private sealed class DrawBatchRecord
    {
        public Vector2[] Positions;
        public Color Color;
        public float SizeX;
        public float SizeY;
        public float Rotation;
    }


    [System.Serializable]
    public class BrushSettings
    {
        [Header("XR")]
        public XRGrabInteractable brushInteractable;

        [Header("Brush Tip")]
        public Transform brushTransform;

        [Header("Appearance")]
        public Color color = Color.black;

        public int sizeY = 20;
        public int sizeX = 20;

        public bool isEraser = false;

        [HideInInspector]
        public Vector2 lastPosition;

        [HideInInspector]
        public bool isFirstDraw = true;

        [HideInInspector]
        public bool isDrawing = false;

        [HideInInspector]
        public bool wasSelected = false;
    }


    [Header("Brushes")]
    public List<BrushSettings> brushes = new List<BrushSettings>();


    private void Start()
    {
        Debug.Log("===== WHITEBOARD START =====");

        if (renderTexture == null)
        {
            Debug.LogError("[WhiteBoard] RenderTexture is NULL!");
            return;
        }

        Debug.Log(
            $"[WhiteBoard] RenderTexture: {renderTexture.name} " +
            $"Size: {renderTexture.width}x{renderTexture.height}"
        );


        // ================================
        // Create Brush Material
        // ================================

        Shader shader = Shader.Find("Hidden/Internal-Colored");

        if (shader == null)
        {
            Debug.LogError(
                "[WhiteBoard] Hidden/Internal-Colored shader NOT FOUND!"
            );
        }
        else
        {
            brushMaterial = new Material(shader);

            Debug.Log(
                "[WhiteBoard] Brush material created."
            );
        }


        // ================================
        // Assign RenderTexture
        // ================================

        Renderer renderer = GetComponent<Renderer>();

        if (renderer == null)
        {
            Debug.LogError(
                "[WhiteBoard] No Renderer found on WhiteBoard!"
            );
        }
        else
        {
            renderer.material.mainTexture = renderTexture;

            Debug.Log(
                "[WhiteBoard] RenderTexture assigned to board."
            );
        }


        // ================================
        // Clear Board
        // ================================

        ClearRenderTexture();


        // ================================
        // Configure Brushes
        // ================================

        foreach (BrushSettings brush in brushes)
        {
            if (brush.brushInteractable == null)
            {
                Debug.LogError(
                    "[WhiteBoard] Brush Interactable is NULL!"
                );
            }

            if (brush.brushTransform == null)
            {
                Debug.LogError(
                    "[WhiteBoard] Brush Transform is NULL!"
                );
            }

            brush.color.a = markerAlpha;

            if (brush.isEraser)
            {
                brush.color = backGroundColor;
            }
        }


        Debug.Log(
            $"[WhiteBoard] Brushes registered: {brushes.Count}"
        );

        Debug.Log(
            "===== WHITEBOARD READY ====="
        );
    }


    private void Update()
    {
        if (renderTexture == null)
            return;

        foreach (BrushSettings brush in brushes)
        {
            if (brush.brushInteractable == null)
                continue;


            bool selected =
                brush.brushInteractable.isSelected;


            // ================================
            // Detect Grab
            // ================================

            if (selected && !brush.wasSelected)
            {
                Debug.Log(
                    $"[WhiteBoard] BRUSH GRABBED: " +
                    $"{brush.brushInteractable.name}"
                );

                brush.wasSelected = true;
            }


            // ================================
            // Detect Release
            // ================================

            if (!selected && brush.wasSelected)
            {
                Debug.Log(
                    $"[WhiteBoard] BRUSH RELEASED: " +
                    $"{brush.brushInteractable.name}"
                );

                brush.wasSelected = false;
                brush.isDrawing = false;
                brush.isFirstDraw = true;
            }


            if (selected)
            {
                DrawBrushOnTexture(brush);
            }
        }

        if (pendingDrawPoints.Count > 0 &&
            Time.unscaledTime >= nextBatchSendTime)
        {
            FlushPendingDrawBatch();
        }
    }


    private void DrawBrushOnTexture(
        BrushSettings brush)
    {
        if (brush.brushTransform == null)
        {
            Debug.LogError(
                "[WhiteBoard] Brush Transform is NULL!"
            );

            return;
        }


        // ================================
        // Create Ray
        // ================================

        Ray ray = new Ray(
            brush.brushTransform.position,
            brush.brushTransform.forward
        );


        Debug.DrawRay(
            ray.origin,
            ray.direction * maxDistance,
            Color.red
        );


        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            maxDistance))
        {
            if (hit.collider.gameObject != gameObject)
            {
                return;
            }


            // ================================
            // Calculate UV
            // ================================

            Vector2 uv;


            if (useBoxCollider)
            {
                BoxCollider boxCollider =
                    GetComponent<BoxCollider>();

                if (boxCollider == null)
                {
                    Debug.LogError(
                        "[WhiteBoard] BoxCollider NOT FOUND!"
                    );

                    return;
                }


                Vector3 localHitPoint =
                    transform.InverseTransformPoint(
                        hit.point
                    );


                uv = new Vector2(
                    (localHitPoint.x /
                     boxCollider.size.x) + 0.5f,

                    1f -
                    (
                        (localHitPoint.y /
                         boxCollider.size.y) + 0.5f
                    )
                );
            }
            else
            {
                uv = hit.textureCoord;

                uv.y = 1f - uv.y;
            }


            // ================================
            // Check UV
            // ================================

            if (uv.x < 0 ||
                uv.x > 1 ||
                uv.y < 0 ||
                uv.y > 1)
            {
                return;
            }


            // ================================
            // Texture Position
            // ================================

            int x =
                (int)(uv.x * renderTexture.width);

            int y =
                (int)(uv.y * renderTexture.height);


            Vector2 currentPosition =
                new Vector2(x, y);


            // ================================
            // Start Drawing
            // ================================

            if (!brush.isDrawing)
            {
                brush.isFirstDraw = true;
                brush.isDrawing = true;
            }


            // ================================
            // First Draw
            // ================================

            if (brush.isFirstDraw)
            {
                QueueDraw(
                    currentPosition,
                    brush.color,
                    brush.sizeX,
                    brush.sizeY,
                    brush.brushTransform
                        .rotation
                        .eulerAngles
                        .z
                );


                brush.lastPosition =
                    currentPosition;

                brush.isFirstDraw = false;

                return;
            }


            // ================================
            // Interpolation
            // ================================

            float distance =
                Vector2.Distance(
                    currentPosition,
                    brush.lastPosition
                );


            int steps =
                Mathf.Max(
                    1,
                    Mathf.CeilToInt(
                        distance /
                        minBrushDistance
                    )
                );


            for (int i = 1; i <= steps; i++)
            {
                Vector2 position =
                    Vector2.Lerp(
                        brush.lastPosition,
                        currentPosition,
                        i / (float)steps
                    );


                QueueDraw(
                    position,
                    brush.color,
                    brush.sizeX,
                    brush.sizeY,
                    brush.brushTransform
                        .rotation
                        .eulerAngles
                        .z
                );
            }


            brush.lastPosition =
                currentPosition;
        }
        else
        {
            brush.isDrawing = false;
        }

    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
            NetworkObject.DontDestroyWithOwner = true;

        if (IsOwner && NetworkManager != null)
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
    }

    public override void OnNetworkDespawn()
    {
        pendingDrawPoints.Clear();

        if (NetworkManager != null)
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;

        base.OnNetworkDespawn();
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsOwner ||
            clientId == NetworkManager.LocalClientId ||
            drawHistory.Count == 0)
        {
            return;
        }

        BeginSnapshotRpc(
            RpcTarget.Single(clientId, RpcTargetUse.Temp)
        );

        for (int i = 0; i < drawHistory.Count; i++)
        {
            DrawBatchRecord batch = drawHistory[i];

            ReceiveSnapshotBatchRpc(
                batch.Positions,
                batch.Color,
                batch.SizeX,
                batch.SizeY,
                batch.Rotation,
                RpcTarget.Single(clientId, RpcTargetUse.Temp)
            );
        }
    }

    [Rpc(
        SendTo.SpecifiedInParams,
        InvokePermission = RpcInvokePermission.Owner
    )]
    private void BeginSnapshotRpc(RpcParams rpcParams = default)
    {
        drawHistory.Clear();
        ClearRenderTexture();
    }

    [Rpc(
        SendTo.SpecifiedInParams,
        InvokePermission = RpcInvokePermission.Owner
    )]
    private void ReceiveSnapshotBatchRpc(
        Vector2[] positions,
        Color color,
        float sizeX,
        float sizeY,
        float rotationAngle,
        RpcParams rpcParams = default)
    {
        StoreHistory(
            positions,
            color,
            sizeX,
            sizeY,
            rotationAngle
        );

        DrawBatch(
            positions,
            color,
            sizeX,
            sizeY,
            rotationAngle
        );
    }

    public void ClearBoard()
    {
        if (!IsSpawned)
        {
            ClearRenderTexture();
            return;
        }

        RequestClearBoardRpc();
    }

    [Rpc(
        SendTo.Authority,
        InvokePermission = RpcInvokePermission.Everyone
    )]
    private void RequestClearBoardRpc(RpcParams rpcParams = default)
    {
        if (!NetworkRoleUtility.HasRole(
                rpcParams.Receive.SenderClientId,
                PlayerRole.Presenter))
        {
            return;
        }

        drawHistory.Clear();
        BroadcastClearBoardRpc();
    }

    [Rpc(
        SendTo.Everyone,
        InvokePermission = RpcInvokePermission.Owner
    )]
    private void BroadcastClearBoardRpc()
    {
        drawHistory.Clear();
        pendingDrawPoints.Clear();
        ClearRenderTexture();
    }


    // =====================================================
    // SEND DRAW
    // =====================================================

    private void QueueDraw(
        Vector2 position,
        Color color,
        float sizeX,
        float sizeY,
        float rotationAngle)
    {
        // اول Local رسم می‌کنیم
        // تا Presenter هیچ Delay احساس نکند.

        DrawAtPosition(
            position,
            color,
            sizeX,
            sizeY,
            rotationAngle
        );


        // اگر Network فعال نیست، فقط Local Drawing انجام شود.
        if (!IsSpawned)
            return;

        bool styleChanged = pendingDrawPoints.Count > 0 &&
            (pendingColor != color ||
             !Mathf.Approximately(pendingSizeX, sizeX) ||
             !Mathf.Approximately(pendingSizeY, sizeY) ||
             !Mathf.Approximately(pendingRotation, rotationAngle));

        if (styleChanged)
            FlushPendingDrawBatch();

        if (pendingDrawPoints.Count == 0)
        {
            pendingColor = color;
            pendingSizeX = sizeX;
            pendingSizeY = sizeY;
            pendingRotation = rotationAngle;
            nextBatchSendTime = Time.unscaledTime + batchInterval;
        }

        pendingDrawPoints.Add(position);

        if (pendingDrawPoints.Count >= maxPointsPerBatch)
            FlushPendingDrawBatch();
    }

    private void FlushPendingDrawBatch()
    {
        if (pendingDrawPoints.Count == 0)
            return;

        Vector2[] positions = pendingDrawPoints.ToArray();
        pendingDrawPoints.Clear();

        if (!IsSpawned)
            return;

        StoreHistory(
            positions,
            pendingColor,
            pendingSizeX,
            pendingSizeY,
            pendingRotation
        );

        RequestDrawBatchRpc(
            positions,
            pendingColor,
            pendingSizeX,
            pendingSizeY,
            pendingRotation
        );
    }


    // =====================================================
    // CLIENT -> AUTHORITY
    // =====================================================

    [Rpc(
        SendTo.Authority,
        InvokePermission = RpcInvokePermission.Everyone
    )]
    private void RequestDrawBatchRpc(
        Vector2[] positions,
        Color color,
        float sizeX,
        float sizeY,
        float rotationAngle,
        RpcParams rpcParams = default)
    {
        if (!NetworkRoleUtility.HasRole(
                rpcParams.Receive.SenderClientId,
                PlayerRole.Presenter))
        {
            Debug.LogWarning(
                "[WhiteBoard] Rejected drawing from a non-presenter."
            );
            return;
        }


        if (positions == null || positions.Length == 0)
            return;

        if (rpcParams.Receive.SenderClientId !=
            NetworkManager.LocalClientId)
        {
            StoreHistory(
                positions,
                color,
                sizeX,
                sizeY,
                rotationAngle
            );
        }

        BroadcastDrawBatchRpc(
            positions,
            color,
            sizeX,
            sizeY,
            rotationAngle,
            rpcParams.Receive.SenderClientId
        );
    }


    // =====================================================
    // AUTHORITY -> EVERYONE
    // =====================================================

    [Rpc(
        SendTo.Everyone,
        InvokePermission = RpcInvokePermission.Owner
    )]
    private void BroadcastDrawBatchRpc(
        Vector2[] positions,
        Color color,
        float sizeX,
        float sizeY,
        float rotationAngle,
        ulong originalSender)
    {
        // کسی که خودش Draw را ارسال کرده
        // قبلاً Local رسم کرده است.
        // دوباره روی Texture خودش رسم نکند.

        if (NetworkManager.Singleton.LocalClientId ==
            originalSender)
        {
            return;
        }

        if (!IsOwner)
        {
            StoreHistory(
                positions,
                color,
                sizeX,
                sizeY,
                rotationAngle
            );
        }


        DrawBatch(
            positions,
            color,
            sizeX,
            sizeY,
            rotationAngle
        );
    }

    private void DrawBatch(
        Vector2[] positions,
        Color color,
        float sizeX,
        float sizeY,
        float rotationAngle)
    {
        if (positions == null)
            return;

        for (int i = 0; i < positions.Length; i++)
        {
            DrawAtPosition(
                positions[i],
                color,
                sizeX,
                sizeY,
                rotationAngle
            );
        }
    }

    private void StoreHistory(
        Vector2[] positions,
        Color color,
        float sizeX,
        float sizeY,
        float rotationAngle)
    {
        if (positions == null || positions.Length == 0)
            return;

        drawHistory.Add(new DrawBatchRecord
        {
            Positions = (Vector2[])positions.Clone(),
            Color = color,
            SizeX = sizeX,
            SizeY = sizeY,
            Rotation = rotationAngle
        });
    }


    // =====================================================
    // LOCAL DRAW
    // =====================================================

    private void DrawAtPosition(
        Vector2 position,
        Color color,
        float sizeX,
        float sizeY,
        float rotationAngle)
    {
        if (brushMaterial == null)
        {
            Debug.LogError(
                "[WhiteBoard] Brush Material is NULL!"
            );

            return;
        }


        if (renderTexture == null)
            return;


        // چون این متد ممکن است از RPC هم اجرا شود،
        // RenderTexture را همینجا Active می‌کنیم.

        RenderTexture previous =
            RenderTexture.active;

        RenderTexture.active =
            renderTexture;


        GL.PushMatrix();


        GL.LoadPixelMatrix(
            0,
            renderTexture.width,
            renderTexture.height,
            0
        );


        brushMaterial.SetPass(0);

        GL.Begin(GL.QUADS);

        GL.Color(color);


        float radians =
            rotationAngle *
            Mathf.Deg2Rad;


        float cos =
            Mathf.Cos(radians);

        float sin =
            Mathf.Sin(radians);


        drawVertices[0] =
            new Vector2(
                -sizeX,
                -sizeY
            );

        drawVertices[1] =
            new Vector2(
                sizeX,
                -sizeY
            );

        drawVertices[2] =
            new Vector2(
                sizeX,
                sizeY
            );

        drawVertices[3] =
            new Vector2(
                -sizeX,
                sizeY
            );


        for (int i = 0;
             i < drawVertices.Length;
             i++)
        {
            float rotatedX =
                drawVertices[i].x * cos +
                drawVertices[i].y * sin;


            float rotatedY =
                -drawVertices[i].x * sin +
                drawVertices[i].y * cos;


            GL.Vertex3(
                position.x + rotatedX,
                position.y + rotatedY,
                0
            );
        }


        GL.End();

        GL.PopMatrix();


        RenderTexture.active =
            previous;
    }


    // =====================================================
    // COLOR
    // =====================================================

    public void SetBrushColor(
        Color newColor)
    {
        foreach (BrushSettings brush in brushes)
        {
            if (brush.isEraser)
                continue;


            newColor.a =
                markerAlpha;


            brush.color =
                newColor;
        }


        Debug.Log(
            $"[WhiteBoard] Brush Color Changed To: " +
            $"{newColor}"
        );
    }


    // =====================================================
    // CLEAR LOCAL
    // =====================================================

    private void ClearRenderTexture()
    {
        if (renderTexture == null)
            return;


        RenderTexture previous =
            RenderTexture.active;


        RenderTexture.active =
            renderTexture;


        GL.Clear(
            true,
            true,
            backGroundColor
        );


        RenderTexture.active =
            previous;
    }

    public override void OnDestroy()
    {
        if (brushMaterial != null)
            Destroy(brushMaterial);

        base.OnDestroy();
    }
}
