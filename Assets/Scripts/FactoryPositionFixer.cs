using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Attach this script to the ROOT GameObject of the imported factory FBX in the Unity scene.
/// Run the component menu option: Apply Factory Position Fixes.
///
/// Recommended order:
/// 1) Run FactoryFBXSetup first.
/// 2) Run this FactoryPositionFixer.
///
/// This script fixes common FBX/procedural factory problems:
/// - roof gaps above walls;
/// - skylights/exhausts floating after roof adjustment;
/// - floating control room by adding supports and a landing platform;
/// - thin floor markings z-fighting or floating;
/// - pallets, pallet truck and loose props not touching the floor;
/// - cardboard boxes/containers floating away from shelves, pallets or conveyors;
/// - overly strong lights/emissive materials.
/// </summary>
[ExecuteAlways]
public class FactoryPositionFixer : MonoBehaviour
{
    [Header("Main Fixes")]
    public bool removeOldFixObjectsBeforeApplying = true;
    public bool fixRoofAndSkylights = true;
    public bool addRoofGapTrims = true;
    public bool addControlRoomSupports = true;
    public bool addControlRoomLanding = true;
    public bool snapFloorObjects = true;
    public bool snapFloatingBoxesAndContainers = true;
    public bool tameExcessiveLights = true;

    [Header("Snap Settings")]
    [Tooltip("Extra height above surfaces to avoid z-fighting.")]
    public float surfaceOffset = 0.015f;

    [Tooltip("How much horizontal tolerance is accepted when checking if a box is above a support.")]
    public float horizontalSupportPadding = 0.18f;

    [Tooltip("If a loose box is higher than this and no support is found below, it will fall back to the floor.")]
    public float maxDropDistanceToSupport = 12f;

    [Header("Roof Settings")]
    [Tooltip("How much the roof overlaps into the top of the walls to hide tiny seams.")]
    public float roofOverlapIntoWalls = 0.015f;

    [Tooltip("Height of the dark trim pieces added around the roof seam.")]
    public float roofTrimHeight = 0.28f;

    [Header("Control Room Settings")]
    public float controlSupportThickness = 0.28f;
    public float controlLandingWidth = 3.3f;
    public float controlLandingDepth = 1.45f;
    public float controlLandingThickness = 0.16f;
    public float railingHeight = 0.95f;

    [Header("Light Tuning")]
    public float maxPointLightIntensity = 1.7f;
    public float maxPointLightRange = 6.5f;
    public float maxSpotLightIntensity = 2.3f;
    public float maxSpotLightRange = 8f;
    public float whiteEmissionStrength = 0.8f;
    public float coloredEmissionStrength = 0.9f;

    [Header("Created Object Settings")]
    public bool addCollidersToCreatedFixObjects = true;
    public bool markCreatedFixObjectsStatic = true;

    private const string FixPrefix = "Fix_";
    private Transform fixGroup;

    private Material concreteMat;
    private Material darkMetalMat;
    private Material roofMetalMat;
    private Material safetyYellowMat;

    private struct SupportSurface
    {
        public Transform transform;
        public Bounds bounds;
        public float topY;
        public int priority;
        public string name;
    }

    [ContextMenu("Apply Factory Position Fixes")]
    public void ApplyFactoryPositionFixes()
    {
        if (transform == null)
            return;

        BuildFixMaterials();

        if (removeOldFixObjectsBeforeApplying)
            RemoveGeneratedFixObjects();

        fixGroup = GetOrCreateFixGroup();

        Bounds floorBounds;
        if (!TryFindFloorBounds(out floorBounds))
        {
            Debug.LogWarning("FactoryPositionFixer could not find Factory_Floor or Factory_Base. Some snap fixes were skipped.", this);
            floorBounds = new Bounds(transform.position, new Vector3(30f, 0.1f, 40f));
        }

        int movedObjects = 0;
        int createdObjects = 0;
        int changedLights = 0;

        if (fixRoofAndSkylights)
            movedObjects += FixRoofGapAndTopObjects();

        if (addRoofGapTrims)
            createdObjects += CreateRoofGapTrims();

        if (addControlRoomSupports)
            createdObjects += CreateControlRoomSupports(floorBounds);

        if (addControlRoomLanding)
            createdObjects += CreateControlRoomLandingAndRailings(floorBounds);

        if (snapFloorObjects)
            movedObjects += SnapCommonFloorObjects(floorBounds);

        if (snapFloatingBoxesAndContainers)
            movedObjects += SnapBoxesAndContainersToBestSupports(floorBounds);

        if (tameExcessiveLights)
            changedLights += TameLightsAndEmissiveMaterials();

        Debug.Log($"Factory position fixes applied to '{name}'. Moved objects: {movedObjects}. Created fix objects: {createdObjects}. Tuned lights/materials: {changedLights}.", this);
    }

    [ContextMenu("Remove Factory Position Fix Objects")]
    public void RemoveGeneratedFixObjects()
    {
        List<GameObject> toRemove = new List<GameObject>();

        Transform[] all = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == transform)
                continue;

            if (t.name.StartsWith(FixPrefix) || t.name == "Factory_Position_Fixes")
                toRemove.Add(t.gameObject);
        }

        for (int i = 0; i < toRemove.Count; i++)
            SafeDestroy(toRemove[i]);
    }

    // ------------------------------------------------------------
    // Roof / skylights / exhausts
    // ------------------------------------------------------------

    private int FixRoofGapAndTopObjects()
    {
        int moved = 0;

        float wallTopY;
        if (!TryGetWallTopY(out wallTopY))
            return moved;

        Transform roof = FindFirstByExactOrContains("Roof_Main", "roof_main");
        if (roof != null && TryGetWorldBounds(roof, out Bounds roofBounds))
        {
            float targetRoofBottom = wallTopY - roofOverlapIntoWalls;
            moved += MoveBoundsMinYTo(roof, targetRoofBottom);

            if (TryGetWorldBounds(roof, out roofBounds))
            {
                moved += MoveTopObjectsAboveRoof(roofBounds.max.y);
            }
        }

        return moved;
    }

    private int MoveTopObjectsAboveRoof(float roofTopY)
    {
        int moved = 0;
        Transform[] all = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == transform)
                continue;

            string n = LowerName(t.name);

            if (n.Contains("skylight"))
            {
                moved += MoveBoundsMinYTo(t, roofTopY + 0.012f);
            }
            else if (n.Contains("exhaust"))
            {
                moved += MoveBoundsMinYTo(t, roofTopY + 0.025f);
            }
        }

        return moved;
    }

    private int CreateRoofGapTrims()
    {
        if (!TryGetBuildingBounds(out Bounds buildingBounds))
            return 0;

        float wallTopY;
        if (!TryGetWallTopY(out wallTopY))
            return 0;

        int created = 0;
        float y = wallTopY + roofTrimHeight * 0.5f - 0.02f;
        float thickness = 0.22f;
        float width = buildingBounds.size.x + 0.35f;
        float depth = buildingBounds.size.z + 0.35f;

        created += CreateCube("Fix_Roof_Trim_Front", new Vector3(buildingBounds.center.x, y, buildingBounds.min.z - thickness * 0.5f), new Vector3(width, roofTrimHeight, thickness), roofMetalMat) != null ? 1 : 0;
        created += CreateCube("Fix_Roof_Trim_Back", new Vector3(buildingBounds.center.x, y, buildingBounds.max.z + thickness * 0.5f), new Vector3(width, roofTrimHeight, thickness), roofMetalMat) != null ? 1 : 0;
        created += CreateCube("Fix_Roof_Trim_Left", new Vector3(buildingBounds.min.x - thickness * 0.5f, y, buildingBounds.center.z), new Vector3(thickness, roofTrimHeight, depth), roofMetalMat) != null ? 1 : 0;
        created += CreateCube("Fix_Roof_Trim_Right", new Vector3(buildingBounds.max.x + thickness * 0.5f, y, buildingBounds.center.z), new Vector3(thickness, roofTrimHeight, depth), roofMetalMat) != null ? 1 : 0;

        return created;
    }

    // ------------------------------------------------------------
    // Control room supports / platform / railings
    // ------------------------------------------------------------

    private int CreateControlRoomSupports(Bounds floorBounds)
    {
        Transform room = FindFirstByExactOrContains("Control_Room", "control_room");
        if (room == null || !TryGetWorldBounds(room, out Bounds roomBounds))
            return 0;

        float floorTop = floorBounds.max.y;
        float supportBottom = floorTop;
        float supportTop = roomBounds.min.y - 0.02f;
        float height = Mathf.Max(0.2f, supportTop - supportBottom);
        float y = supportBottom + height * 0.5f;
        float inset = 0.45f;
        float t = controlSupportThickness;

        int created = 0;

        Vector3[] corners = new Vector3[]
        {
            new Vector3(roomBounds.min.x + inset, y, roomBounds.min.z + inset),
            new Vector3(roomBounds.max.x - inset, y, roomBounds.min.z + inset),
            new Vector3(roomBounds.min.x + inset, y, roomBounds.max.z - inset),
            new Vector3(roomBounds.max.x - inset, y, roomBounds.max.z - inset)
        };

        for (int i = 0; i < corners.Length; i++)
        {
            if (CreateCube($"Fix_ControlRoom_Support_{i + 1}", corners[i], new Vector3(t, height, t), darkMetalMat) != null)
                created++;
        }

        float beamY = supportTop - 0.12f;
        created += CreateCube("Fix_ControlRoom_Front_Beam", new Vector3(roomBounds.center.x, beamY, roomBounds.min.z + inset), new Vector3(roomBounds.size.x - inset * 1.2f, 0.18f, 0.18f), darkMetalMat) != null ? 1 : 0;
        created += CreateCube("Fix_ControlRoom_Back_Beam", new Vector3(roomBounds.center.x, beamY, roomBounds.max.z - inset), new Vector3(roomBounds.size.x - inset * 1.2f, 0.18f, 0.18f), darkMetalMat) != null ? 1 : 0;
        created += CreateCube("Fix_ControlRoom_Left_Beam", new Vector3(roomBounds.min.x + inset, beamY, roomBounds.center.z), new Vector3(0.18f, 0.18f, roomBounds.size.z - inset * 1.2f), darkMetalMat) != null ? 1 : 0;
        created += CreateCube("Fix_ControlRoom_Right_Beam", new Vector3(roomBounds.max.x - inset, beamY, roomBounds.center.z), new Vector3(0.18f, 0.18f, roomBounds.size.z - inset * 1.2f), darkMetalMat) != null ? 1 : 0;

        return created;
    }

    private int CreateControlRoomLandingAndRailings(Bounds floorBounds)
    {
        Transform room = FindFirstByExactOrContains("Control_Room", "control_room");
        if (room == null || !TryGetWorldBounds(room, out Bounds roomBounds))
            return 0;

        float landingTopY = roomBounds.min.y + 0.08f;

        Transform floor = FindFirstByExactOrContains("Control_Room_Floor", "control_room_floor");
        if (floor != null && TryGetWorldBounds(floor, out Bounds controlFloorBounds))
            landingTopY = controlFloorBounds.max.y;

        bool frontIsMinZ = true;
        float frontZ = roomBounds.min.z;

        List<Transform> windows = FindAllContaining("control_window");
        if (windows.Count > 0)
        {
            float avgZ = 0f;
            int count = 0;
            for (int i = 0; i < windows.Count; i++)
            {
                if (TryGetWorldBounds(windows[i], out Bounds wb))
                {
                    avgZ += wb.center.z;
                    count++;
                }
            }

            if (count > 0)
            {
                avgZ /= count;
                float distToMin = Mathf.Abs(avgZ - roomBounds.min.z);
                float distToMax = Mathf.Abs(avgZ - roomBounds.max.z);
                frontIsMinZ = distToMin <= distToMax;
                frontZ = frontIsMinZ ? roomBounds.min.z : roomBounds.max.z;
            }
        }

        float platformCenterZ = frontIsMinZ
            ? frontZ - controlLandingDepth * 0.5f - 0.04f
            : frontZ + controlLandingDepth * 0.5f + 0.04f;

        float platformCenterX = roomBounds.max.x - controlLandingWidth * 0.52f;
        float platformCenterY = landingTopY - controlLandingThickness * 0.5f;

        int created = 0;
        GameObject landing = CreateCube("Fix_ControlRoom_Landing_Platform", new Vector3(platformCenterX, platformCenterY, platformCenterZ), new Vector3(controlLandingWidth, controlLandingThickness, controlLandingDepth), darkMetalMat);
        if (landing != null)
            created++;

        if (landing != null && TryGetWorldBounds(landing.transform, out Bounds landingBounds))
        {
            float railY = landingBounds.max.y + railingHeight;
            float postY = landingBounds.max.y + railingHeight * 0.5f;
            float railThickness = 0.09f;
            float postThickness = 0.11f;

            float outsideZ = frontIsMinZ ? landingBounds.min.z : landingBounds.max.z;
            float insideZ = frontIsMinZ ? landingBounds.max.z : landingBounds.min.z;
            float sideX = landingBounds.max.x;

            // Front rail, side rail and back short rail. Leave part of the side open for stairs.
            created += CreateCube("Fix_ControlRoom_Landing_Front_Rail", new Vector3(landingBounds.center.x, railY, outsideZ), new Vector3(landingBounds.size.x, railThickness, railThickness), safetyYellowMat) != null ? 1 : 0;
            created += CreateCube("Fix_ControlRoom_Landing_Right_Rail", new Vector3(sideX, railY, landingBounds.center.z), new Vector3(railThickness, railThickness, landingBounds.size.z), safetyYellowMat) != null ? 1 : 0;
            created += CreateCube("Fix_ControlRoom_Landing_Back_Rail", new Vector3(landingBounds.min.x + landingBounds.size.x * 0.35f, railY, insideZ), new Vector3(landingBounds.size.x * 0.7f, railThickness, railThickness), safetyYellowMat) != null ? 1 : 0;

            Vector3[] posts = new Vector3[]
            {
                new Vector3(landingBounds.min.x, postY, outsideZ),
                new Vector3(landingBounds.max.x, postY, outsideZ),
                new Vector3(landingBounds.max.x, postY, insideZ),
                new Vector3(landingBounds.min.x, postY, insideZ)
            };

            for (int i = 0; i < posts.Length; i++)
            {
                if (CreateCube($"Fix_ControlRoom_Landing_Post_{i + 1}", posts[i], new Vector3(postThickness, railingHeight, postThickness), safetyYellowMat) != null)
                    created++;
            }

            // Small bridge piece to visually connect the top of the stairs to the landing.
            float bridgeZ = frontIsMinZ ? landingBounds.min.z + 0.25f : landingBounds.max.z - 0.25f;
            created += CreateCube("Fix_ControlRoom_Stair_Top_Bridge", new Vector3(landingBounds.max.x - 0.45f, landingBounds.max.y + 0.035f, bridgeZ), new Vector3(0.95f, 0.07f, 0.65f), darkMetalMat) != null ? 1 : 0;
        }

        return created;
    }

    // ------------------------------------------------------------
    // Snapping / loose objects
    // ------------------------------------------------------------

    private int SnapCommonFloorObjects(Bounds floorBounds)
    {
        int moved = 0;
        float floorTop = floorBounds.max.y;
        Transform[] all = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == transform || t.name.StartsWith(FixPrefix))
                continue;

            string n = LowerName(t.name);
            if (!TryGetWorldBounds(t, out Bounds b))
                continue;

            if (n.Contains("safety_line") || n.Contains("safety_path") || n.Contains("parking_line"))
            {
                moved += MoveBoundsMinYTo(t, floorTop + surfaceOffset * 0.35f);
                continue;
            }

            if (n.Contains("pallet_") || n.StartsWith("pallet") || n.Contains("hand_pallet_truck"))
            {
                if (Mathf.Abs(b.min.y - floorTop) > 0.035f)
                    moved += MoveBoundsMinYTo(t, floorTop);
                continue;
            }

            if (n.Contains("rack_left") || n.Contains("rack_right"))
            {
                // Only snap full rack structural pieces, not small boxes/containers placed on shelves.
                if (!IsLooseBoxOrContainer(t) && Mathf.Abs(b.min.y - floorTop) > 0.06f && b.size.y > 1.5f)
                    moved += MoveBoundsMinYTo(t, floorTop);
                continue;
            }

            if (n.Contains("extinguisher"))
            {
                // Extinguishers can be wall-mounted. Only fix them if they are clearly floating too high or sinking.
                if (b.min.y < floorTop - 0.08f || b.min.y > floorTop + 0.65f)
                    moved += MoveBoundsMinYTo(t, floorTop + 0.05f);
            }
        }

        return moved;
    }

    private int SnapBoxesAndContainersToBestSupports(Bounds floorBounds)
    {
        List<SupportSurface> supports = BuildSupportSurfaces(floorBounds);
        if (supports.Count == 0)
            return 0;

        int moved = 0;
        Transform[] all = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < all.Length; i++)
        {
            Transform item = all[i];
            if (item == transform || item.name.StartsWith(FixPrefix))
                continue;

            if (!IsLooseBoxOrContainer(item))
                continue;

            if (!TryGetWorldBounds(item, out Bounds itemBounds))
                continue;

            SupportSurface best = supports[0];
            bool hasBest = false;

            for (int s = 0; s < supports.Count; s++)
            {
                SupportSurface support = supports[s];

                if (support.transform == item)
                    continue;

                if (!HasHorizontalOverlap(itemBounds, support.bounds, horizontalSupportPadding))
                    continue;

                // Ignore supports that are clearly above the object.
                if (support.topY > itemBounds.center.y + 0.15f)
                    continue;

                float drop = itemBounds.min.y - support.topY;
                if (drop > maxDropDistanceToSupport)
                    continue;

                if (!hasBest)
                {
                    best = support;
                    hasBest = true;
                    continue;
                }

                // Prefer higher supports. If almost equal, prefer pallets/racks/conveyors over floor.
                if (support.topY > best.topY + 0.02f || (Mathf.Abs(support.topY - best.topY) <= 0.02f && support.priority > best.priority))
                {
                    best = support;
                    hasBest = true;
                }
            }

            if (!hasBest)
            {
                // If no valid shelf/pallet/conveyor is under it, place it on the floor instead of leaving it floating.
                best = new SupportSurface
                {
                    transform = null,
                    bounds = floorBounds,
                    topY = floorBounds.max.y,
                    priority = 0,
                    name = "Factory_Floor"
                };
            }

            float targetMinY = best.topY + surfaceOffset;
            if (Mathf.Abs(itemBounds.min.y - targetMinY) > 0.025f)
                moved += MoveBoundsMinYTo(item, targetMinY);
        }

        return moved;
    }

    private List<SupportSurface> BuildSupportSurfaces(Bounds floorBounds)
    {
        List<SupportSurface> supports = new List<SupportSurface>();

        supports.Add(new SupportSurface
        {
            transform = null,
            bounds = floorBounds,
            topY = floorBounds.max.y,
            priority = 0,
            name = "Factory_Floor"
        });

        Transform[] all = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == transform || t.name.StartsWith(FixPrefix))
                continue;

            if (!TryGetWorldBounds(t, out Bounds b))
                continue;

            string n = LowerName(t.name);
            bool isSupport = false;
            int priority = 1;

            if (n.Contains("pallet") && !n.Contains("truck"))
            {
                isSupport = true;
                priority = 5;
            }
            else if ((n.Contains("conveyor") && n.Contains("belt")) || n.Contains("conveyor_main_belt") || n.Contains("conveyor_left_belt") || n.Contains("conveyor_right_belt"))
            {
                isSupport = true;
                priority = 4;
            }
            else if (n.Contains("shelf") || n.Contains("prateleira") || n.Contains("rack_shelf") || n.Contains("rack_level"))
            {
                isSupport = true;
                priority = 6;
            }
            else if (n.Contains("rack") && b.size.y < 0.35f && (b.size.x > 0.7f || b.size.z > 0.7f))
            {
                // Horizontal rack boards/beams can support containers.
                isSupport = true;
                priority = 6;
            }
            else if (n.Contains("machine") && b.size.y > 0.6f)
            {
                // Low priority fallback for small boxes accidentally placed on machine tops.
                isSupport = true;
                priority = 2;
            }

            if (isSupport)
            {
                supports.Add(new SupportSurface
                {
                    transform = t,
                    bounds = b,
                    topY = b.max.y,
                    priority = priority,
                    name = t.name
                });
            }
        }

        return supports;
    }

    private bool IsLooseBoxOrContainer(Transform t)
    {
        if (t == null)
            return false;

        string n = LowerName(t.name);

        if (!(n.Contains("box") || n.Contains("caixa") || n.Contains("package") || n.Contains("cardboard") || n.Contains("container")))
            return false;

        if (n.Contains("control") || n.Contains("panel") || n.Contains("window") || n.Contains("sign"))
            return false;

        if (!TryGetWorldBounds(t, out Bounds b))
            return false;

        // Avoid moving big structural pieces that happen to have "box" in the name.
        if (b.size.x > 2.2f || b.size.y > 1.7f || b.size.z > 2.2f)
            return false;

        return true;
    }

    private bool HasHorizontalOverlap(Bounds a, Bounds b, float padding)
    {
        bool overlapX = a.max.x >= b.min.x - padding && a.min.x <= b.max.x + padding;
        bool overlapZ = a.max.z >= b.min.z - padding && a.min.z <= b.max.z + padding;
        return overlapX && overlapZ;
    }

    // ------------------------------------------------------------
    // Light and emission cleanup
    // ------------------------------------------------------------

    private int TameLightsAndEmissiveMaterials()
    {
        int changed = 0;

        Light[] lights = GetComponentsInChildren<Light>(true);
        for (int i = 0; i < lights.Length; i++)
        {
            Light l = lights[i];
            if (l == null)
                continue;

            if (l.type == LightType.Point)
            {
                if (l.intensity > maxPointLightIntensity)
                {
                    l.intensity = maxPointLightIntensity;
                    changed++;
                }

                if (l.range > maxPointLightRange)
                {
                    l.range = maxPointLightRange;
                    changed++;
                }
            }
            else if (l.type == LightType.Spot)
            {
                if (l.intensity > maxSpotLightIntensity)
                {
                    l.intensity = maxSpotLightIntensity;
                    changed++;
                }

                if (l.range > maxSpotLightRange)
                {
                    l.range = maxSpotLightRange;
                    changed++;
                }
            }
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        HashSet<Material> touched = new HashSet<Material>();

        for (int r = 0; r < renderers.Length; r++)
        {
            Material[] mats = renderers[r].sharedMaterials;
            if (mats == null)
                continue;

            for (int m = 0; m < mats.Length; m++)
            {
                Material mat = mats[m];
                if (mat == null || touched.Contains(mat))
                    continue;

                string matName = LowerName(mat.name);
                string objName = LowerName(renderers[r].name);
                string text = matName + " " + objName;

                if (text.Contains("emission") || text.Contains("status_light") || text.Contains("exit_sign") || text.Contains("light_"))
                {
                    Color baseColor = Color.white;
                    if (mat.HasProperty("_BaseColor"))
                        baseColor = mat.GetColor("_BaseColor");
                    else if (mat.HasProperty("_Color"))
                        baseColor = mat.GetColor("_Color");

                    float strength = text.Contains("white") || text.Contains("light_") ? whiteEmissionStrength : coloredEmissionStrength;
                    SetEmission(mat, baseColor * strength);
                    touched.Add(mat);
                    changed++;
                }
            }
        }

        return changed;
    }

    // ------------------------------------------------------------
    // Find helpers / bounds helpers
    // ------------------------------------------------------------

    private Transform FindFirstByExactOrContains(params string[] namesOrTokens)
    {
        Transform[] all = GetComponentsInChildren<Transform>(true);

        // Exact-ish pass.
        for (int i = 0; i < all.Length; i++)
        {
            string n = LowerName(all[i].name);
            for (int k = 0; k < namesOrTokens.Length; k++)
            {
                string token = LowerName(namesOrTokens[k]);
                if (n == token || n.StartsWith(token + ".") || n.StartsWith(token + "_") || n.StartsWith(token + " "))
                    return all[i];
            }
        }

        // Contains pass.
        for (int i = 0; i < all.Length; i++)
        {
            string n = LowerName(all[i].name);
            for (int k = 0; k < namesOrTokens.Length; k++)
            {
                string token = LowerName(namesOrTokens[k]);
                if (n.Contains(token))
                    return all[i];
            }
        }

        return null;
    }

    private List<Transform> FindAllContaining(params string[] tokens)
    {
        List<Transform> result = new List<Transform>();
        Transform[] all = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == transform)
                continue;

            string n = LowerName(t.name);
            bool matched = false;
            for (int k = 0; k < tokens.Length; k++)
            {
                if (n.Contains(LowerName(tokens[k])))
                {
                    matched = true;
                    break;
                }
            }

            if (matched)
                result.Add(t);
        }

        return result;
    }

    private bool TryFindFloorBounds(out Bounds floorBounds)
    {
        Transform floor = FindFirstByExactOrContains("Factory_Floor", "factory_floor");
        if (floor != null && TryGetWorldBounds(floor, out floorBounds))
            return true;

        Transform baseObj = FindFirstByExactOrContains("Factory_Base", "factory_base");
        if (baseObj != null && TryGetWorldBounds(baseObj, out floorBounds))
            return true;

        floorBounds = default;
        return false;
    }

    private bool TryGetWallTopY(out float wallTopY)
    {
        bool found = false;
        wallTopY = float.MinValue;

        Transform[] all = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            string n = LowerName(t.name);

            if (!n.Contains("wall"))
                continue;

            if (n.Contains("window"))
                continue;

            if (TryGetWorldBounds(t, out Bounds b))
            {
                wallTopY = Mathf.Max(wallTopY, b.max.y);
                found = true;
            }
        }

        return found;
    }

    private bool TryGetBuildingBounds(out Bounds buildingBounds)
    {
        bool found = false;
        buildingBounds = default;

        Transform[] all = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            string n = LowerName(t.name);

            bool isMainStructure = n.Contains("wall") || n.Contains("factory_floor") || n.Contains("roof_main");
            if (!isMainStructure || n.Contains("window"))
                continue;

            if (TryGetWorldBounds(t, out Bounds b))
            {
                if (!found)
                {
                    buildingBounds = b;
                    found = true;
                }
                else
                {
                    buildingBounds.Encapsulate(b);
                }
            }
        }

        return found;
    }

    private bool TryGetWorldBounds(Transform t, out Bounds bounds)
    {
        bounds = default;
        if (t == null)
            return false;

        Renderer[] renderers = t.GetComponentsInChildren<Renderer>(true);
        bool found = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null)
                continue;

            if (!found)
            {
                bounds = r.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        return found;
    }

    private int MoveBoundsMinYTo(Transform t, float targetMinY)
    {
        if (t == null || !TryGetWorldBounds(t, out Bounds b))
            return 0;

        float delta = targetMinY - b.min.y;
        if (Mathf.Abs(delta) < 0.002f)
            return 0;

#if UNITY_EDITOR
        Undo.RecordObject(t, "Move factory object");
#endif
        t.position += Vector3.up * delta;
        return 1;
    }

    // ------------------------------------------------------------
    // Created objects / materials
    // ------------------------------------------------------------

    private Transform GetOrCreateFixGroup()
    {
        Transform existing = transform.Find("Factory_Position_Fixes");
        if (existing != null)
            return existing;

        GameObject group = new GameObject("Factory_Position_Fixes");
#if UNITY_EDITOR
        Undo.RegisterCreatedObjectUndo(group, "Create factory fix group");
#endif
        group.transform.SetParent(transform, false);
        group.transform.localPosition = Vector3.zero;
        group.transform.localRotation = Quaternion.identity;
        group.transform.localScale = Vector3.one;
        return group.transform;
    }

    private GameObject CreateCube(string objectName, Vector3 worldCenter, Vector3 worldSize, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = objectName;

#if UNITY_EDITOR
        Undo.RegisterCreatedObjectUndo(go, "Create factory fix object");
#endif

        if (fixGroup == null)
            fixGroup = GetOrCreateFixGroup();

        go.transform.SetParent(fixGroup, true);
        go.transform.position = worldCenter;
        go.transform.rotation = Quaternion.identity;
        SetApproxWorldScale(go.transform, worldSize);

        Renderer r = go.GetComponent<Renderer>();
        if (r != null && mat != null)
            r.sharedMaterial = mat;

        if (!addCollidersToCreatedFixObjects)
        {
            Collider c = go.GetComponent<Collider>();
            if (c != null)
                SafeDestroy(c);
        }

        if (markCreatedFixObjectsStatic)
            go.isStatic = true;

        return go;
    }

    private void SetApproxWorldScale(Transform t, Vector3 desiredWorldScale)
    {
        if (t.parent == null)
        {
            t.localScale = desiredWorldScale;
            return;
        }

        Vector3 parentScale = t.parent.lossyScale;
        t.localScale = new Vector3(
            SafeDivide(desiredWorldScale.x, parentScale.x),
            SafeDivide(desiredWorldScale.y, parentScale.y),
            SafeDivide(desiredWorldScale.z, parentScale.z)
        );
    }

    private float SafeDivide(float value, float divisor)
    {
        if (Mathf.Abs(divisor) < 0.0001f)
            return value;
        return value / divisor;
    }

    private void BuildFixMaterials()
    {
        concreteMat = MakeMaterial("Fix_Concrete", new Color(0.50f, 0.50f, 0.48f), 0f, 0.35f);
        darkMetalMat = MakeMaterial("Fix_Dark_Metal", new Color(0.10f, 0.11f, 0.12f), 0.6f, 0.32f);
        roofMetalMat = MakeMaterial("Fix_Roof_Metal", new Color(0.25f, 0.28f, 0.30f), 0.5f, 0.28f);
        safetyYellowMat = MakeMaterial("Fix_Safety_Yellow", new Color(1f, 0.76f, 0.04f), 0f, 0.32f);
    }

    private Material MakeMaterial(string materialName, Color color, float metallic, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = materialName;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", smoothness);

        return mat;
    }

    private void SetEmission(Material mat, Color emissionColor)
    {
        if (mat == null)
            return;

        mat.EnableKeyword("_EMISSION");
        if (mat.HasProperty("_EmissionColor"))
            mat.SetColor("_EmissionColor", emissionColor);
    }

    // ------------------------------------------------------------
    // Utility
    // ------------------------------------------------------------

    private string LowerName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.ToLowerInvariant()
            .Replace("(clone)", "")
            .Replace(" ", "_");
    }

    private void SafeDestroy(Object obj)
    {
        if (obj == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            Undo.DestroyObjectImmediate(obj);
        else
            Destroy(obj);
#else
        Destroy(obj);
#endif
    }
}
