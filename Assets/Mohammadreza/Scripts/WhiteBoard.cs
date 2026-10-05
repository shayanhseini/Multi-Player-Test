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

    public Color backGroundColor = Color.white;

    [Range(0, 1)]
    public float markerAlpha = 0.7f;

    [Header("Collider")]
    public bool useBoxCollider = true;

    private Material brushMaterial;


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
                SendDraw(
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


                SendDraw(
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


    // =====================================================
    // SEND DRAW
    // =====================================================

    private void SendDraw(
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


        // اگر Network فعال نیست
        // فقط Local Drawing انجام شود.

        if (!IsSpawned)
            return;


        // اطلاعات Draw را برای Server می‌فرستیم.

        RequestDrawRpc(
            position.x,
            position.y,

            color.r,
            color.g,
            color.b,
            color.a,

            sizeX,
            sizeY,

            rotationAngle
        );
    }


    // =====================================================
    // CLIENT -> SERVER
    // =====================================================

    [Rpc(
        SendTo.Server,
        RequireOwnership = false
    )]
    private void RequestDrawRpc(
        float x,
        float y,

        float r,
        float g,
        float b,
        float a,

        float sizeX,
        float sizeY,

        float rotationAngle,

        RpcParams rpcParams = default)
    {
        // بعداً اینجا می‌توانیم چک کنیم
        // Sender واقعاً Presenter هست یا نه.


        // Server فرمان Draw را
        // برای Clientها ارسال می‌کند.

        BroadcastDrawRpc(
            x,
            y,

            r,
            g,
            b,
            a,

            sizeX,
            sizeY,

            rotationAngle,

            rpcParams.Receive.SenderClientId
        );
    }


    // =====================================================
    // SERVER -> CLIENTS
    // =====================================================

    [Rpc(SendTo.NotServer)]
    private void BroadcastDrawRpc(
        float x,
        float y,

        float r,
        float g,
        float b,
        float a,

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


        Vector2 position =
            new Vector2(x, y);


        Color color =
            new Color(
                r,
                g,
                b,
                a
            );


        DrawAtPosition(
            position,
            color,
            sizeX,
            sizeY,
            rotationAngle
        );
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


        Vector2[] vertices =
            new Vector2[4];


        vertices[0] =
            new Vector2(
                -sizeX,
                -sizeY
            );

        vertices[1] =
            new Vector2(
                sizeX,
                -sizeY
            );

        vertices[2] =
            new Vector2(
                sizeX,
                sizeY
            );

        vertices[3] =
            new Vector2(
                -sizeX,
                sizeY
            );


        for (int i = 0;
             i < vertices.Length;
             i++)
        {
            float rotatedX =
                vertices[i].x * cos +
                vertices[i].y * sin;


            float rotatedY =
                -vertices[i].x * sin +
                vertices[i].y * cos;


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
}