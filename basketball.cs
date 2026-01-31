using MelonLoader;
using UnityEngine;
using System.Collections.Generic;

[assembly: MelonInfo(typeof(FractalUzayModu.MyMod), "BasketballSimple", "1.0.0", "Vuln")]
[assembly: MelonGame(null, null)]

namespace FractalUzayModu
{
    public class MyMod : MelonMod
    {
        private string[] objectList = {
            "AmmoPack", "Health", "Bloc",
            "Plateforme", "BuiltInSphere", "BuiltInBasketballHoopHome", "BuiltInBasketballHoopAway",
            "BuiltInGlassWall", "BuiltInFloor", "BuiltInScoreboard", "BuiltInCourtWall"
        };

        private Dictionary<string, GameObject> objectCache = new Dictionary<string, GameObject>();
        private List<GameObject> hoopList = new List<GameObject>();

        private int selectedIndex = 0;
        private string selectedObjectName = "AmmoPack";
        private List<GameObject> objectsInHand = new List<GameObject>();
        private List<GameObject> placedObjects = new List<GameObject>();
        private bool flightMode = false;
        private Transform movingObject;
        private float objectDistance = 3.0f;
        private Vector3 currentRotation = Vector3.zero;
        private float currentScale = 1.0f;
        private bool inventoryOpen = false;
        private bool guiVisible = true;

        private GameObject ballInHand = null;
        private bool powerLoading = false;
        private float powerAmount = 0f;
        private float maxPower = 35f;
        private float powerLoadSpeed = 18f;
        private GameObject targetIndicator = null;

        private int homeScore = 0;
        private int awayScore = 0;

        private string homeTeamName = "HOME";
        private string awayTeamName = "AWAY";
        private bool editingTeamNames = false;

        private Vector3 ballThrowPosition;
        private bool basketCheckActive = false;
        private float basketCheckDuration = 0f;

        private TextMesh homeScoreText = null;
        private TextMesh awayScoreText = null;
        private TextMesh homeTeamLabel = null;
        private TextMesh awayTeamLabel = null;
        private GameObject scoreboardObj = null;
        private Light scoreboardLight = null;

        private int activeLanguage = 1;
        private string[] languages = { "Türkçe", "English", "Français", "Deutsch", "Español", "Italiano" };

        private Dictionary<string, string[]> translation = new Dictionary<string, string[]>()
        {
            { "title", new string[] { "🏀 BASKETBOL v3.5", "🏀 BASKETBALL v3.5", "🏀 BASKETBALL v3.5", "🏀 BASKETBALL v3.5", "🏀 BALONCESTO v3.5", "🏀 PALLACANESTRO v3.5" } },
            { "selected_category", new string[] { "Seçili:", "Selected:", "Sélectionné:", "Ausgewählt:", "Seleccionado:", "Selezionato:" } },
            { "score", new string[] { "SKOR:", "SCORE:", "SCORE:", "PUNKTZAHL:", "PUNTUACIÓN:", "PUNTEGGIO:" } },
            { "home", new string[] { "EV", "HOME", "DOMICILE", "HEIM", "LOCAL", "CASA" } },
            { "away", new string[] { "DEPLASMAN", "AWAY", "EXTÉRIEUR", "AUSWÄRTS", "VISITANTE", "OSPITE" } },
            { "controls", new string[] { "KONTROLLER:", "CONTROLS:", "CONTRÔLES:", "STEUERUNG:", "CONTROLES:", "CONTROLLI:" } },
            { "inventory_open", new string[] { "F3 - Envanter", "F3 - Inventory", "F3 - Inventaire", "F3 - Inventar", "F3 - Inventario", "F3 - Inventario" } },
            { "panel_open", new string[] { "F1 - Panel", "F1 - Panel", "F1 - Panneau", "F1 - Panel", "F1 - Panel", "F1 - Pannello" } },
            { "object_create", new string[] { "Sol Tık - Obje Yarat", "Left Click - Create", "Clic Gauche - Créer", "Linksklick - Erstellen", "Clic Izq - Crear", "Clic Sin - Crea" } },
            { "ball_pickup", new string[] { "E - Topu Al/Bırak", "E - Pick/Drop Ball", "E - Prendre/Lâcher", "E - Ball Nehmen", "E - Coger/Soltar", "E - Prendi/Lascia" } },
            { "ball_throw", new string[] { "Sol Tık (Bas/Tut) - At", "Left Click (Hold) - Throw", "Clic Gauche - Lancer", "Linksklick - Werfen", "Clic Izq - Lanzar", "Clic Sin - Lancia" } },
            { "score_reset", new string[] { "F5 - Skoru Sıfırla", "F5 - Reset Score", "F5 - Réinit Score", "F5 - Score Zurücksetzen", "F5 - Reiniciar", "F5 - Reset" } },
            { "flight", new string[] { "G - Uçuş Modu", "G - Flight", "G - Vol", "G - Flug", "G - Vuelo", "G - Volo" } },
            { "cancel", new string[] { "İptal: Sol+Sağ Tık", "Cancel: L+R Click", "Annuler: Clic G+D", "Abbrechen: L+R", "Cancelar: Clic I+D", "Annulla: Clic S+D" } },
            { "power", new string[] { "GÜÇ:", "POWER:", "PUISSANCE:", "KRAFT:", "POTENCIA:", "POTENZA:" } },
            { "language", new string[] { "F6 - Dil", "F6 - Language", "F6 - Langue", "F6 - Sprache", "F6 - Idioma", "F6 - Lingua" } },
            { "edit_teams", new string[] { "Skor tahtasına yaklaş + E", "Near scoreboard + E", "Près tableau + E", "Nahe Anzeigetafel + E", "Cerca marcador + E", "Vicino tabellone + E" } }
        };

        private string Translate(string key)
        {
            if (translation.ContainsKey(key) && activeLanguage < translation[key].Length)
                return translation[key][activeLanguage];
            return key;
        }

        public override void OnApplicationStart()
        {
            MelonLogger.Msg("=== BASKETBALL MOD STARTING ===");
            MelonCoroutines.Start(CreateModelsAfterSceneLoad());
        }

        private System.Collections.IEnumerator CreateModelsAfterSceneLoad()
        {
            for (int i = 0; i < 240; i++)
                yield return null;

            MelonLogger.Msg("Creating basketball objects...");

            try
            {
                GameObject template = GameObject.Find("Bloc");
                if (template == null) template = GameObject.Find("Flare");
                if (template == null) template = GameObject.Find("SolBlanc");
                if (template == null) template = GameObject.Find("AmmoPack");

                if (template != null)
                {
                    MelonLogger.Msg($"Template found: {template.name}");

                    CreateRealisticBasketball(template);

                    GameObject hoopHome = CreateBasketballHoop(template, new Color(0f, 1f, 0.3f));
                    if (hoopHome != null)
                    {
                        hoopHome.name = "BuiltInBasketballHoopHome";
                        hoopHome.SetActive(false);
                        UnityEngine.Object.DontDestroyOnLoad(hoopHome);
                        objectCache["BuiltInBasketballHoopHome"] = hoopHome;
                        MelonLogger.Msg("✓ Home Basketball Hoop created (GREEN)");
                    }

                    GameObject hoopAway = CreateBasketballHoop(template, new Color(1f, 0.2f, 0f));
                    if (hoopAway != null)
                    {
                        hoopAway.name = "BuiltInBasketballHoopAway";
                        hoopAway.SetActive(false);
                        UnityEngine.Object.DontDestroyOnLoad(hoopAway);
                        objectCache["BuiltInBasketballHoopAway"] = hoopAway;
                        MelonLogger.Msg("✓ Away Basketball Hoop created (RED)");
                    }

                    CreateGlassWallObject();
                    CreateFloorObject();
                    CreateProScoreboard();
                    CreateCourtWall();

                    MelonLogger.Msg("=== BASKETBALL READY ===");
                }
                else
                {
                    TryCreatePrimitiveModels();
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Msg($"Error: {ex.Message}");
                TryCreatePrimitiveModels();
            }
        }

        private void CreateRealisticBasketball(GameObject template)
        {
            GameObject sphere = UnityEngine.Object.Instantiate(template);
            sphere.name = "BuiltInSphere";

            MeshFilter mfSphere = sphere.GetComponent<MeshFilter>();
            if (mfSphere != null)
                mfSphere.mesh = CreateSphereMesh(32, 32);

            MeshRenderer mrSphere = sphere.GetComponent<MeshRenderer>();
            if (mrSphere != null)
            {
                Material matSphere = new Material(Shader.Find("Standard"));
                matSphere.color = new Color(0.95f, 0.5f, 0.15f);
                matSphere.SetFloat("_Metallic", 0.0f);
                matSphere.SetFloat("_Glossiness", 0.4f);
                mrSphere.material = matSphere;
            }

            BoxCollider boxCol = sphere.GetComponent<BoxCollider>();
            if (boxCol != null) UnityEngine.Object.Destroy(boxCol);

            SphereCollider sphereCol = sphere.GetComponent<SphereCollider>();
            if (sphereCol == null)
                sphereCol = sphere.AddComponent<SphereCollider>();

            sphereCol.isTrigger = false;
            sphereCol.radius = 0.12f;

            Rigidbody rb = sphere.GetComponent<Rigidbody>();
            if (rb == null)
                rb = sphere.AddComponent<Rigidbody>();

            rb.mass = 0.624f;
            rb.drag = 0.2f;
            rb.angularDrag = 0.2f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.maxAngularVelocity = 15f;

            SphereCollider sphereCol2 = sphere.GetComponent<SphereCollider>();
            if (sphereCol2 == null) sphereCol2 = sphere.AddComponent<SphereCollider>();

            if (sphereCol2.material != null)
            {
                sphereCol2.material.bounciness = 0.5f;
                sphereCol2.material.dynamicFriction = 0.6f;
                sphereCol2.material.staticFriction = 0.6f;
                sphereCol2.material.frictionCombine = PhysicMaterialCombine.Average;
                sphereCol2.material.bounceCombine = PhysicMaterialCombine.Average;
            }

            TrailRenderer trail = sphere.AddComponent<TrailRenderer>();
            trail.time = 0.2f;
            trail.startWidth = 0.15f;
            trail.endWidth = 0.02f;
            trail.material = new Material(Shader.Find("Sprites/Default"));

            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[2];
            colorKeys[0].color = Color.red;
            colorKeys[0].time = 0.0f;
            colorKeys[1].color = new Color(0.8f, 0f, 0f);
            colorKeys[1].time = 1.0f;

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].alpha = 0.7f;
            alphaKeys[0].time = 0.0f;
            alphaKeys[1].alpha = 0.0f;
            alphaKeys[1].time = 1.0f;

            gradient.SetKeys(colorKeys, alphaKeys);
            trail.colorGradient = gradient;
            trail.enabled = false;

            sphere.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(sphere);
            objectCache["BuiltInSphere"] = sphere;
            MelonLogger.Msg("✓ Basketball created (red trail)");
        }

        private void CreateProScoreboard()
        {
            try
            {
                GameObject board = new GameObject("BuiltInScoreboard");

                GameObject casing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                casing.name = "Casing";
                casing.transform.SetParent(board.transform);
                casing.transform.localPosition = Vector3.zero;
                casing.transform.localScale = new Vector3(6f, 3f, 0.4f);

                MeshRenderer mrCasing = casing.GetComponent<MeshRenderer>();
                Material matCasing = new Material(Shader.Find("Standard"));
                matCasing.color = new Color(0.1f, 0.1f, 0.1f);
                matCasing.SetFloat("_Metallic", 0.6f);
                matCasing.SetFloat("_Glossiness", 0.7f);
                mrCasing.material = matCasing;

                GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
                screen.name = "Screen";
                screen.transform.SetParent(board.transform);
                screen.transform.localPosition = new Vector3(0, 0, -0.21f);
                screen.transform.localScale = new Vector3(5.6f, 2.6f, 0.02f);

                UnityEngine.Object.Destroy(screen.GetComponent<Collider>());

                MeshRenderer mrScreen = screen.GetComponent<MeshRenderer>();
                Material matScreen = new Material(Shader.Find("Standard"));
                matScreen.color = new Color(0.05f, 0.05f, 0.15f);
                matScreen.EnableKeyword("_EMISSION");
                matScreen.SetColor("_EmissionColor", new Color(0.1f, 0.1f, 0.3f) * 0.5f);
                mrScreen.material = matScreen;

                GameObject lightObj = new GameObject("ScoreboardLight");
                lightObj.transform.SetParent(board.transform);
                lightObj.transform.localPosition = new Vector3(0, 0, -0.5f);

                Light light = lightObj.AddComponent<Light>();
                light.type = LightType.Spot;
                light.color = Color.white;
                light.intensity = 2f;
                light.range = 10f;
                light.spotAngle = 60f;
                scoreboardLight = light;

                CreateScoreTextObjects(board.transform);

                Rigidbody rb = board.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;

                board.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(board);
                objectCache["BuiltInScoreboard"] = board;
                scoreboardObj = board;

                MelonLogger.Msg("✓ LED Scoreboard created!");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Msg("Scoreboard error: " + ex.Message);
            }
        }

        private void CreateScoreTextObjects(Transform parent)
        {
            GameObject homeTextObj = new GameObject("HomeScoreText");
            homeTextObj.transform.SetParent(parent);
            homeTextObj.transform.localPosition = new Vector3(-1.5f, 0.3f, -0.25f);
            homeTextObj.transform.localRotation = Quaternion.identity;
            homeTextObj.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

            TextMesh homeText = homeTextObj.AddComponent<TextMesh>();
            homeText.text = "000";
            homeText.fontSize = 50;
            homeText.color = new Color(0f, 1f, 0.3f);
            homeText.fontStyle = FontStyle.Bold;
            homeText.alignment = TextAlignment.Center;
            homeText.anchor = TextAnchor.MiddleCenter;
            homeScoreText = homeText;

            GameObject awayTextObj = new GameObject("AwayScoreText");
            awayTextObj.transform.SetParent(parent);
            awayTextObj.transform.localPosition = new Vector3(1.5f, 0.3f, -0.25f);
            awayTextObj.transform.localRotation = Quaternion.identity;
            awayTextObj.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

            TextMesh awayText = awayTextObj.AddComponent<TextMesh>();
            awayText.text = "000";
            awayText.fontSize = 50;
            awayText.color = new Color(1f, 0.2f, 0f);
            awayText.fontStyle = FontStyle.Bold;
            awayText.alignment = TextAlignment.Center;
            awayText.anchor = TextAnchor.MiddleCenter;
            awayScoreText = awayText;

            homeTeamLabel = CreateTeamLabel(parent, homeTeamName, new Vector3(-1.5f, 1.0f, -0.25f), new Color(0f, 1f, 0.3f));
            awayTeamLabel = CreateTeamLabel(parent, awayTeamName, new Vector3(1.5f, 1.0f, -0.25f), new Color(1f, 0.2f, 0f));
        }

        private TextMesh CreateTeamLabel(Transform parent, string text, Vector3 pos, Color color)
        {
            GameObject labelObj = new GameObject($"Label_{text}");
            labelObj.transform.SetParent(parent);
            labelObj.transform.localPosition = pos;
            labelObj.transform.localRotation = Quaternion.identity;
            labelObj.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);

            TextMesh label = labelObj.AddComponent<TextMesh>();
            label.text = text;
            label.fontSize = 30;
            label.color = color * 0.7f;
            label.fontStyle = FontStyle.Normal;
            label.alignment = TextAlignment.Center;
            label.anchor = TextAnchor.MiddleCenter;

            return label;
        }

        private void UpdateScoreboard()
        {
            if (homeScoreText != null)
                homeScoreText.text = homeScore.ToString("000");

            if (awayScoreText != null)
                awayScoreText.text = awayScore.ToString("000");

            if (homeTeamLabel != null)
                homeTeamLabel.text = homeTeamName;

            if (awayTeamLabel != null)
                awayTeamLabel.text = awayTeamName;
        }

        private void CheckForBasket()
        {
            if (!basketCheckActive) return;

            basketCheckDuration += Time.deltaTime;

            if (basketCheckDuration > 10f)
            {
                basketCheckActive = false;
                basketCheckDuration = 0f;
                hoopList.Clear();
                return;
            }

            if (hoopList.Count == 0)
            {
                foreach (GameObject obj in placedObjects)
                {
                    if (obj != null && (obj.name.Contains("BuiltInBasketballHoopHome") || obj.name.Contains("BuiltInBasketballHoopAway")) && obj.name.Contains("Mod_"))
                    {
                        hoopList.Add(obj);
                    }
                }

                if (hoopList.Count == 0) return;
            }

            foreach (GameObject obj in hoopList)
            {
                if (obj == null) continue;

                Transform rim = obj.transform.Find("Rim");
                if (rim != null)
                {
                    Vector3 hoopPos = rim.position;
                    bool isHomeHoop = obj.name.Contains("Home");

                    foreach (GameObject ballObj in placedObjects)
                    {
                        if (ballObj != null && ballObj.name.Contains("BuiltInSphere"))
                        {
                            Vector3 ballPos = ballObj.transform.position;
                            float horizontalDistance = Vector3.Distance(new Vector3(ballPos.x, hoopPos.y, ballPos.z), hoopPos);
                            float verticalDiff = ballPos.y - hoopPos.y;

                            if (horizontalDistance < 0.45f && verticalDiff < 0.2f && verticalDiff > -1.5f)
                            {
                                Rigidbody rbBall = ballObj.GetComponent<Rigidbody>();
                                if (rbBall != null && rbBall.velocity.y < -0.3f)
                                {
                                    BasketMade(ballObj, hoopPos, isHomeHoop);
                                    basketCheckActive = false;
                                    hoopList.Clear();
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void BasketMade(GameObject ball, Vector3 hoopPos, bool isHomeHoop)
        {
            float throwDistance = Vector3.Distance(ballThrowPosition, hoopPos);

            int points = 2;
            string message = "BASKET! +2";

            if (throwDistance >= 6.75f)
            {
                points = 3;
                message = "3-POINTER! +3";
            }

            if (isHomeHoop)
            {
                homeScore += points;
                MelonLogger.Msg($"🏀 {message} for {homeTeamName} | Distance: {throwDistance:F1}m | Score: {homeScore}");
            }
            else
            {
                awayScore += points;
                MelonLogger.Msg($"🏀 {message} for {awayTeamName} | Distance: {throwDistance:F1}m | Score: {awayScore}");
            }
        }

        private void CreateFloorObject()
        {
            try
            {
                GameObject floorParent = new GameObject("BuiltInFloor");

                GameObject mainFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mainFloor.name = "MainParquet";
                mainFloor.transform.SetParent(floorParent.transform);
                mainFloor.transform.localScale = new Vector3(28f, 0.1f, 15f);

                Material parquetMat = new Material(Shader.Find("Standard"));
                parquetMat.color = new Color(0.85f, 0.65f, 0.4f);
                parquetMat.SetFloat("_Glossiness", 0.7f);
                mainFloor.GetComponent<MeshRenderer>().material = parquetMat;

                Material lineMat = new Material(Shader.Find("Standard"));
                lineMat.color = Color.white;

                System.Action<string, Vector3, Vector3> addLine = (name, pos, scale) => {
                    GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    line.name = name;
                    line.transform.SetParent(floorParent.transform);
                    line.transform.localScale = scale;
                    line.transform.localPosition = new Vector3(pos.x, 0.06f, pos.z);
                    line.GetComponent<MeshRenderer>().material = lineMat;
                    Object.Destroy(line.GetComponent<Collider>());
                };

                addLine("SideLine1", new Vector3(0, 0, 7.5f), new Vector3(28f, 0.02f, 0.15f));
                addLine("SideLine2", new Vector3(0, 0, -7.5f), new Vector3(28f, 0.02f, 0.15f));
                addLine("BaseLine1", new Vector3(14f, 0, 0), new Vector3(0.15f, 0.02f, 15f));
                addLine("BaseLine2", new Vector3(-14f, 0, 0), new Vector3(0.15f, 0.02f, 15f));
                addLine("CenterLine", new Vector3(0, 0, 0), new Vector3(0.15f, 0.02f, 15f));

                for (int i = 0; i < 8; i++)
                {
                    float angle = i * 45 * Mathf.Deg2Rad;
                    Vector3 circlePos = new Vector3(Mathf.Cos(angle) * 1.8f, 0, Mathf.Sin(angle) * 1.8f);
                    addLine("CirclePart" + i, circlePos, new Vector3(0.5f, 0.02f, 0.15f));
                }

                addLine("KeyRight1", new Vector3(11.1f, 0, 2.45f), new Vector3(5.8f, 0.02f, 0.15f));
                addLine("KeyRight2", new Vector3(11.1f, 0, -2.45f), new Vector3(5.8f, 0.02f, 0.15f));
                addLine("KeyRight3", new Vector3(8.2f, 0, 0), new Vector3(0.15f, 0.02f, 4.9f));
                addLine("KeyLeft1", new Vector3(-11.1f, 0, 2.45f), new Vector3(5.8f, 0.02f, 0.15f));
                addLine("KeyLeft2", new Vector3(-11.1f, 0, -2.45f), new Vector3(5.8f, 0.02f, 0.15f));
                addLine("KeyLeft3", new Vector3(-8.2f, 0, 0), new Vector3(0.15f, 0.02f, 4.9f));

                Material threePtMat = new Material(Shader.Find("Standard"));
                threePtMat.color = new Color(1f, 0.8f, 0f);

                for (int i = 0; i < 20; i++)
                {
                    float angle = (i * 9f - 90f) * Mathf.Deg2Rad + Mathf.PI;
                    Vector3 pos = new Vector3(14f - 1.575f + Mathf.Cos(angle) * 6.75f, 0, Mathf.Sin(angle) * 6.75f);
                    GameObject arc = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    arc.name = "ThreePtRight" + i;
                    arc.transform.SetParent(floorParent.transform);
                    arc.transform.localScale = new Vector3(0.4f, 0.02f, 0.15f);
                    arc.transform.localPosition = new Vector3(pos.x, 0.06f, pos.z);
                    arc.GetComponent<MeshRenderer>().material = threePtMat;
                    Object.Destroy(arc.GetComponent<Collider>());
                }

                for (int i = 0; i < 20; i++)
                {
                    float angle = (i * 9f - 90f) * Mathf.Deg2Rad + Mathf.PI;
                    Vector3 pos = new Vector3(-14f + 1.575f - Mathf.Cos(angle) * 6.75f, 0, Mathf.Sin(angle) * 6.75f);
                    GameObject arc = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    arc.name = "ThreePtLeft" + i;
                    arc.transform.SetParent(floorParent.transform);
                    arc.transform.localScale = new Vector3(0.4f, 0.02f, 0.15f);
                    arc.transform.localPosition = new Vector3(pos.x, 0.06f, pos.z);
                    arc.GetComponent<MeshRenderer>().material = threePtMat;
                    Object.Destroy(arc.GetComponent<Collider>());
                }

                Rigidbody rb = floorParent.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;

                floorParent.SetActive(false);
                Object.DontDestroyOnLoad(floorParent);
                objectCache["BuiltInFloor"] = floorParent;

                MelonLogger.Msg("✓ Basketball Court (with 3-point line)");
            }
            catch (System.Exception ex) { MelonLogger.Msg("Court error: " + ex.Message); }
        }

        private void CreateGlassWallObject()
        {
            try
            {
                GameObject glassWall = new GameObject("BuiltInGlassWall");

                MeshFilter mfGlass = glassWall.AddComponent<MeshFilter>();
                mfGlass.mesh = CreateGlassWallMesh(10f, 5f, 0.1f);

                MeshRenderer mrGlass = glassWall.AddComponent<MeshRenderer>();
                Material glassMat = new Material(Shader.Find("Standard"));
                glassMat.color = new Color(0.7f, 0.85f, 1f, 0.3f);
                glassMat.SetFloat("_Mode", 3);
                glassMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                glassMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                glassMat.SetInt("_ZWrite", 0);
                glassMat.DisableKeyword("_ALPHATEST_ON");
                glassMat.EnableKeyword("_ALPHABLEND_ON");
                glassMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                glassMat.renderQueue = 3000;
                glassMat.SetFloat("_Glossiness", 0.9f);
                mrGlass.material = glassMat;

                MeshCollider colGlass = glassWall.AddComponent<MeshCollider>();
                colGlass.sharedMesh = mfGlass.mesh;
                colGlass.convex = true;

                Rigidbody rb = glassWall.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;

                glassWall.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(glassWall);
                objectCache["BuiltInGlassWall"] = glassWall;
                MelonLogger.Msg("✓ Glass Wall");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Msg($"GlassWall error: {ex.Message}");
            }
        }

        private void CreateCourtWall()
        {
            try
            {
                GameObject courtWall = new GameObject("BuiltInCourtWall");

                MeshFilter mf = courtWall.AddComponent<MeshFilter>();
                mf.mesh = CreateCubeMesh();

                courtWall.transform.localScale = new Vector3(30f, 3f, 0.2f);

                MeshRenderer mr = courtWall.AddComponent<MeshRenderer>();
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.8f, 0.9f, 1f, 0.4f);
                mat.SetFloat("_Mode", 3);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
                mat.SetFloat("_Glossiness", 0.7f);
                mr.material = mat;

                BoxCollider col = courtWall.AddComponent<BoxCollider>();

                Rigidbody rb = courtWall.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;

                courtWall.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(courtWall);
                objectCache["BuiltInCourtWall"] = courtWall;
                MelonLogger.Msg("✓ Court Wall");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Msg($"CourtWall error: {ex.Message}");
            }
        }

        private GameObject CreateBasketballHoop(GameObject template, Color rimColor)
        {
            try
            {
                GameObject hoopParent = new GameObject("BuiltInBasketballHoop");

                GameObject pole = UnityEngine.Object.Instantiate(template);
                pole.name = "Pole";
                pole.transform.SetParent(hoopParent.transform);
                pole.transform.localPosition = Vector3.zero;

                MeshFilter mfPole = pole.GetComponent<MeshFilter>();
                if (mfPole != null)
                    mfPole.mesh = CreateCylinderMesh(0.15f, 3.0f);

                MeshRenderer mrPole = pole.GetComponent<MeshRenderer>();
                if (mrPole != null)
                {
                    Material matPole = new Material(Shader.Find("Standard"));
                    matPole.color = new Color(0.3f, 0.3f, 0.3f);
                    mrPole.material = matPole;
                }

                GameObject backboard = UnityEngine.Object.Instantiate(template);
                backboard.name = "Backboard";
                backboard.transform.SetParent(hoopParent.transform);
                backboard.transform.localPosition = new Vector3(0, 1.5f, 0.3f);
                backboard.transform.localScale = new Vector3(1.8f, 1.2f, 0.1f);

                MeshFilter mfBackboard = backboard.GetComponent<MeshFilter>();
                if (mfBackboard != null)
                    mfBackboard.mesh = CreateCubeMesh();

                MeshRenderer mrBackboard = backboard.GetComponent<MeshRenderer>();
                if (mrBackboard != null)
                {
                    Material matBackboard = new Material(Shader.Find("Standard"));
                    matBackboard.color = new Color(0.95f, 0.95f, 0.95f, 0.4f);
                    mrBackboard.material = matBackboard;
                }

                GameObject rim = UnityEngine.Object.Instantiate(template);
                rim.name = "Rim";
                rim.transform.SetParent(hoopParent.transform);
                rim.transform.localPosition = new Vector3(0, 1.0f, 0.6f);
                rim.transform.localRotation = Quaternion.Euler(0, 0, 0);

                MeshFilter mfRim = rim.GetComponent<MeshFilter>();
                if (mfRim != null)
                    mfRim.mesh = CreateTorusMesh(0.45f, 0.05f);

                MeshRenderer mrRim = rim.GetComponent<MeshRenderer>();
                if (mrRim != null)
                {
                    Material matRim = new Material(Shader.Find("Standard"));
                    matRim.color = rimColor;
                    mrRim.material = matRim;
                }

                for (int seg = 0; seg < 24; seg++)
                {
                    GameObject rimSegment = new GameObject($"RimCollider{seg}");
                    rimSegment.transform.SetParent(rim.transform);

                    float angle = seg * 15f * Mathf.Deg2Rad;
                    float x = 0.45f * Mathf.Cos(angle);
                    float z = 0.45f * Mathf.Sin(angle);

                    rimSegment.transform.localPosition = new Vector3(x, 0, z);
                    rimSegment.transform.localRotation = Quaternion.Euler(0, seg * 15f, 0);

                    BoxCollider segCol = rimSegment.AddComponent<BoxCollider>();
                    segCol.size = new Vector3(0.1f, 0.05f, 0.15f);
                }

                for (int i = 0; i < 12; i++)
                {
                    GameObject netString = UnityEngine.Object.Instantiate(template);
                    netString.name = $"NetString{i}";
                    netString.transform.SetParent(hoopParent.transform);

                    float angle = i * 30f * Mathf.Deg2Rad;
                    float x = 0.45f * Mathf.Cos(angle);
                    float z = 0.45f * Mathf.Sin(angle);

                    netString.transform.localPosition = new Vector3(x, 0.7f, 0.6f + z);
                    netString.transform.localScale = new Vector3(0.02f, 0.4f, 0.02f);

                    MeshFilter mfNet = netString.GetComponent<MeshFilter>();
                    if (mfNet != null)
                        mfNet.mesh = CreateCylinderMesh(0.01f, 0.4f);

                    MeshRenderer mrNet = netString.GetComponent<MeshRenderer>();
                    if (mrNet != null)
                    {
                        Material matNet = new Material(Shader.Find("Standard"));
                        matNet.color = new Color(1.0f, 1.0f, 1.0f);
                        mrNet.material = matNet;
                    }

                    Collider netCol = netString.GetComponent<Collider>();
                    if (netCol != null) UnityEngine.Object.Destroy(netCol);
                }

                BoxCollider poleCollider = pole.AddComponent<BoxCollider>();
                poleCollider.size = new Vector3(0.3f, 3.0f, 0.3f);

                BoxCollider backboardCollider = backboard.AddComponent<BoxCollider>();

                Rigidbody rb = hoopParent.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;

                return hoopParent;
            }
            catch (System.Exception ex)
            {
                MelonLogger.Msg($"Hoop error: {ex.Message}");
                return null;
            }
        }

        private void TryCreatePrimitiveModels()
        {
            try
            {
                MelonLogger.Msg("Template not found, creating primitive objects...");

                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = "BuiltInSphere";

                MeshRenderer mrSphere = sphere.GetComponent<MeshRenderer>();
                if (mrSphere != null)
                {
                    Material matSphere = new Material(Shader.Find("Standard"));
                    matSphere.color = new Color(0.95f, 0.5f, 0.15f);
                    mrSphere.material = matSphere;
                }

                Rigidbody rb = sphere.AddComponent<Rigidbody>();
                rb.mass = 0.624f;
                rb.drag = 0.2f;
                rb.angularDrag = 0.2f;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.maxAngularVelocity = 15f;

                SphereCollider sphereCol3 = sphere.GetComponent<SphereCollider>();
                if (sphereCol3 == null) sphereCol3 = sphere.AddComponent<SphereCollider>();

                if (sphereCol3.material != null)
                {
                    sphereCol3.material.bounciness = 0.5f;
                    sphereCol3.material.dynamicFriction = 0.6f;
                    sphereCol3.material.staticFriction = 0.6f;
                    sphereCol3.material.frictionCombine = PhysicMaterialCombine.Average;
                    sphereCol3.material.bounceCombine = PhysicMaterialCombine.Average;
                }

                sphere.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(sphere);
                objectCache["BuiltInSphere"] = sphere;
                MelonLogger.Msg("✓ Basketball (primitive)");

                GameObject hoopHomeP = CreateBasketballHoopPrimitive(new Color(0f, 1f, 0.3f));
                if (hoopHomeP != null)
                {
                    hoopHomeP.name = "BuiltInBasketballHoopHome";
                    hoopHomeP.SetActive(false);
                    UnityEngine.Object.DontDestroyOnLoad(hoopHomeP);
                    objectCache["BuiltInBasketballHoopHome"] = hoopHomeP;
                    MelonLogger.Msg("✓ Home Basketball Hoop (primitive - GREEN)");
                }

                GameObject hoopAwayP = CreateBasketballHoopPrimitive(new Color(1f, 0.2f, 0f));
                if (hoopAwayP != null)
                {
                    hoopAwayP.name = "BuiltInBasketballHoopAway";
                    hoopAwayP.SetActive(false);
                    UnityEngine.Object.DontDestroyOnLoad(hoopAwayP);
                    objectCache["BuiltInBasketballHoopAway"] = hoopAwayP;
                    MelonLogger.Msg("✓ Away Basketball Hoop (primitive - RED)");
                }

                CreateGlassWallObject();
                CreateFloorObject();
                CreateProScoreboard();
                CreateCourtWall();
            }
            catch (System.Exception ex)
            {
                MelonLogger.Msg($"Primitive error: {ex.Message}");
            }
        }

        private GameObject CreateBasketballHoopPrimitive(Color rimColor)
        {
            try
            {
                GameObject hoopParent = new GameObject("BuiltInBasketballHoop");

                GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pole.name = "Pole";
                pole.transform.SetParent(hoopParent.transform);
                pole.transform.localPosition = Vector3.zero;
                pole.transform.localScale = new Vector3(0.3f, 1.5f, 0.3f);

                MeshRenderer mrPole = pole.GetComponent<MeshRenderer>();
                Material matPole = new Material(Shader.Find("Standard"));
                matPole.color = new Color(0.3f, 0.3f, 0.3f);
                mrPole.material = matPole;

                GameObject backboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                backboard.name = "Backboard";
                backboard.transform.SetParent(hoopParent.transform);
                backboard.transform.localPosition = new Vector3(0, 1.5f, 0.3f);
                backboard.transform.localScale = new Vector3(1.8f, 1.2f, 0.1f);

                MeshRenderer mrBackboard = backboard.GetComponent<MeshRenderer>();
                Material matBackboard = new Material(Shader.Find("Standard"));
                matBackboard.color = new Color(0.95f, 0.95f, 0.95f, 0.4f);
                mrBackboard.material = matBackboard;

                GameObject rim = new GameObject("Rim");
                rim.transform.SetParent(hoopParent.transform);
                rim.transform.localPosition = new Vector3(0, 1.0f, 0.6f);
                rim.transform.localRotation = Quaternion.Euler(0, 0, 0);

                MeshFilter mfRim = rim.AddComponent<MeshFilter>();
                mfRim.mesh = CreateTorusMesh(0.45f, 0.05f);

                MeshRenderer mrRim = rim.AddComponent<MeshRenderer>();
                Material matRim = new Material(Shader.Find("Standard"));
                matRim.color = rimColor;
                mrRim.material = matRim;

                for (int seg = 0; seg < 24; seg++)
                {
                    GameObject rimSegment = new GameObject($"RimCollider{seg}");
                    rimSegment.transform.SetParent(rim.transform);

                    float angle = seg * 15f * Mathf.Deg2Rad;
                    float x = 0.45f * Mathf.Cos(angle);
                    float z = 0.45f * Mathf.Sin(angle);

                    rimSegment.transform.localPosition = new Vector3(x, 0, z);
                    rimSegment.transform.localRotation = Quaternion.Euler(0, seg * 15f, 0);

                    BoxCollider segCol = rimSegment.AddComponent<BoxCollider>();
                    segCol.size = new Vector3(0.1f, 0.05f, 0.15f);
                }

                for (int i = 0; i < 12; i++)
                {
                    GameObject netString = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    netString.name = $"NetString{i}";
                    netString.transform.SetParent(hoopParent.transform);

                    float angle = i * 30f * Mathf.Deg2Rad;
                    float x = 0.45f * Mathf.Cos(angle);
                    float z = 0.45f * Mathf.Sin(angle);

                    netString.transform.localPosition = new Vector3(x, 0.7f, 0.6f + z);
                    netString.transform.localScale = new Vector3(0.02f, 0.4f, 0.02f);

                    MeshRenderer mrNet = netString.GetComponent<MeshRenderer>();
                    Material matNet = new Material(Shader.Find("Standard"));
                    matNet.color = Color.white;
                    mrNet.material = matNet;

                    Collider netCol = netString.GetComponent<Collider>();
                    if (netCol != null) UnityEngine.Object.Destroy(netCol);
                }

                Rigidbody rb = hoopParent.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;

                return hoopParent;
            }
            catch (System.Exception ex)
            {
                MelonLogger.Msg($"Primitive hoop error: {ex.Message}");
                return null;
            }
        }

        private Mesh CreateGlassWallMesh(float width = 10f, float height = 3f, float thickness = 0.1f)
        {
            Mesh mesh = new Mesh();
            mesh.name = "GlassWallMesh";

            float w = width / 2f;
            float h = height / 2f;
            float t = thickness / 2f;

            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-w, -h,  t), new Vector3( w, -h,  t),
                new Vector3( w,  h,  t), new Vector3(-w,  h,  t),
                new Vector3(-w, -h, -t), new Vector3( w, -h, -t),
                new Vector3( w,  h, -t), new Vector3(-w,  h, -t),
            };

            int[] triangles = new int[]
            {
                0, 2, 1, 0, 3, 2,
                5, 6, 4, 6, 7, 4,
                4, 7, 0, 7, 3, 0,
                1, 2, 5, 2, 6, 5,
                3, 7, 2, 7, 6, 2,
                4, 0, 5, 0, 1, 5
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private Mesh CreateCubeMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "CubeMesh";

            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f)
            };

            int[] triangles = new int[]
            {
                0, 2, 1, 0, 3, 2,
                1, 6, 5, 1, 2, 6,
                5, 7, 4, 5, 6, 7,
                4, 3, 0, 4, 7, 3,
                3, 6, 2, 3, 7, 6,
                4, 1, 5, 4, 0, 1
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private Mesh CreateSphereMesh(int segments = 16, int rings = 16)
        {
            Mesh mesh = new Mesh();
            mesh.name = "SphereMesh";

            float radius = 0.12f;

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            for (int ring = 0; ring <= rings; ring++)
            {
                float phi = Mathf.PI * ring / rings;
                for (int segment = 0; segment <= segments; segment++)
                {
                    float theta = 2f * Mathf.PI * segment / segments;

                    float x = radius * Mathf.Sin(phi) * Mathf.Cos(theta);
                    float y = radius * Mathf.Cos(phi);
                    float z = radius * Mathf.Sin(phi) * Mathf.Sin(theta);

                    vertices.Add(new Vector3(x, y, z));
                }
            }

            for (int ring = 0; ring < rings; ring++)
            {
                for (int segment = 0; segment < segments; segment++)
                {
                    int current = ring * (segments + 1) + segment;
                    int next = current + segments + 1;

                    triangles.Add(current);
                    triangles.Add(next);
                    triangles.Add(current + 1);

                    triangles.Add(current + 1);
                    triangles.Add(next);
                    triangles.Add(next + 1);
                }
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private Mesh CreateCylinderMesh(float radius = 0.5f, float height = 2.0f)
        {
            Mesh mesh = new Mesh();
            mesh.name = "CylinderMesh";

            int segments = 16;

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            for (int i = 0; i <= segments; i++)
            {
                float angle = 2f * Mathf.PI * i / segments;
                float x = radius * Mathf.Cos(angle);
                float z = radius * Mathf.Sin(angle);
                vertices.Add(new Vector3(x, height / 2, z));
            }

            for (int i = 0; i <= segments; i++)
            {
                float angle = 2f * Mathf.PI * i / segments;
                float x = radius * Mathf.Cos(angle);
                float z = radius * Mathf.Sin(angle);
                vertices.Add(new Vector3(x, -height / 2, z));
            }

            for (int i = 0; i < segments; i++)
            {
                int top1 = i;
                int top2 = i + 1;
                int bottom1 = i + segments + 1;
                int bottom2 = i + segments + 2;

                triangles.Add(top1);
                triangles.Add(bottom1);
                triangles.Add(top2);

                triangles.Add(top2);
                triangles.Add(bottom1);
                triangles.Add(bottom2);
            }

            int topCenter = vertices.Count;
            vertices.Add(new Vector3(0, height / 2, 0));
            for (int i = 0; i < segments; i++)
            {
                triangles.Add(topCenter);
                triangles.Add(i);
                triangles.Add(i + 1);
            }

            int bottomCenter = vertices.Count;
            vertices.Add(new Vector3(0, -height / 2, 0));
            for (int i = 0; i < segments; i++)
            {
                triangles.Add(bottomCenter);
                triangles.Add(i + segments + 2);
                triangles.Add(i + segments + 1);
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private Mesh CreateTorusMesh(float radius = 0.5f, float thickness = 0.1f)
        {
            Mesh mesh = new Mesh();
            mesh.name = "TorusMesh";

            int segments = 24;
            int sides = 12;

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            for (int segment = 0; segment <= segments; segment++)
            {
                float theta = 2f * Mathf.PI * segment / segments;

                for (int side = 0; side <= sides; side++)
                {
                    float phi = 2f * Mathf.PI * side / sides;

                    float x = (radius + thickness * Mathf.Cos(phi)) * Mathf.Cos(theta);
                    float y = thickness * Mathf.Sin(phi);
                    float z = (radius + thickness * Mathf.Cos(phi)) * Mathf.Sin(theta);

                    vertices.Add(new Vector3(x, y, z));
                }
            }

            for (int segment = 0; segment < segments; segment++)
            {
                for (int side = 0; side < sides; side++)
                {
                    int current = segment * (sides + 1) + side;
                    int next = current + sides + 1;

                    triangles.Add(current);
                    triangles.Add(next);
                    triangles.Add(current + 1);

                    triangles.Add(current + 1);
                    triangles.Add(next);
                    triangles.Add(next + 1);
                }
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            objectsInHand.Clear();
            placedObjects.Clear();
            flightMode = false;
            ballInHand = null;
            powerLoading = false;
            powerAmount = 0f;

            MelonLogger.Msg(sceneName + " loaded");
        }

        public override void OnGUI()
        {
            if (Camera.main == null) return;

            if (guiVisible)
            {
                DrawMainPanel();
            }

            if (ballInHand != null && powerLoading)
            {
                DrawPowerBar();
            }

            if (inventoryOpen)
            {
                DrawInventory();
            }

            if (editingTeamNames)
            {
                DrawTeamNameEditor();
            }
        }

        private void DrawMainPanel()
        {
            GUILayout.BeginArea(new Rect(10, 10, 420, 400));
            GUILayout.BeginVertical("box");

            GUILayout.Label($"<b><size=18>{Translate("title")}</size></b>");
            GUILayout.Space(5);

            GUILayout.Label($"<b><size=16><color=lime>{Translate("score")}</color></size></b>");
            GUILayout.Label($"<b>{Translate("home")}: <color=green>{homeScore}</color> | {Translate("away")}: <color=red>{awayScore}</color></b>");

            GUILayout.Space(10);
            GUILayout.Label($"<b>{Translate("selected_category")}</b> {selectedObjectName}");
            GUILayout.Space(5);

            GUILayout.Label($"<b><color=yellow>{Translate("controls")}</color></b>");
            GUILayout.Label(Translate("language"));
            GUILayout.Label(Translate("inventory_open"));
            GUILayout.Label(Translate("panel_open"));
            GUILayout.Label(Translate("object_create"));
            GUILayout.Label(Translate("ball_pickup"));
            GUILayout.Label(Translate("ball_throw"));
            GUILayout.Label(Translate("score_reset"));
            GUILayout.Label(Translate("flight"));
            GUILayout.Label($"<color=cyan>{Translate("edit_teams")}</color>");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawPowerBar()
        {
            float barWidth = 400f;
            float barHeight = 35f;
            float barX = (Screen.width - barWidth) / 2f;
            float barY = Screen.height - 120f;

            GUI.Box(new Rect(barX - 5, barY - 5, barWidth + 10, barHeight + 10), "");
            GUI.Box(new Rect(barX, barY, barWidth, barHeight), "");

            float powerRatio = powerAmount / maxPower;
            Color barColor = Color.Lerp(Color.green, new Color(1f, 0.5f, 0f), powerRatio);
            barColor = Color.Lerp(barColor, Color.red, Mathf.Max(0, powerRatio - 0.7f) * 3.33f);

            GUI.color = barColor;
            GUI.Box(new Rect(barX + 2, barY + 2, (barWidth - 4) * powerRatio, barHeight - 4), "");
            GUI.color = Color.white;

            GUIStyle style = new GUIStyle();
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 16;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.white;

            GUILayout.BeginArea(new Rect(barX, barY, barWidth, barHeight));
            GUILayout.Label($"{Translate("power")} {(int)(powerRatio * 100)}%", style);
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(barX, barY + barHeight + 5, barWidth, 20));
            GUILayout.Label(Translate("cancel"));
            GUILayout.EndArea();
        }

        private void DrawInventory()
        {
            float panelW = 700;
            float panelH = 700;
            Rect panelRect = new Rect((Screen.width - panelW) / 2, (Screen.height - panelH) / 2, panelW, panelH);

            GUI.Box(panelRect, "");

            GUILayout.BeginArea(panelRect);
            GUILayout.BeginVertical("box");
            GUILayout.Label($"<b><size=18>🎒 BUILD INVENTORY</size></b>");
            GUILayout.Space(10);

            int columns = 5;
            float slotSize = 100;

            GUILayout.BeginVertical();
            for (int row = 0; row < Mathf.CeilToInt((float)objectList.Length / columns); row++)
            {
                GUILayout.BeginHorizontal();

                for (int column = 0; column < columns; column++)
                {
                    int i = row * columns + column;
                    if (i >= objectList.Length) break;

                    GUILayout.BeginVertical("box", GUILayout.Width(slotSize), GUILayout.Height(slotSize));

                    string objName = objectList[i];
                    bool selected = (i == selectedIndex);

                    if (selected)
                        GUI.backgroundColor = Color.yellow;

                    if (GUILayout.Button($"[{objName}]", GUILayout.Width(slotSize - 10), GUILayout.Height(slotSize - 40)))
                    {
                        Select(i);
                    }

                    GUI.backgroundColor = Color.white;

                    GUILayout.Label($"<b>{objName}</b>");

                    GUILayout.EndVertical();
                }

                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);
            if (GUILayout.Button($"<b>CLOSE (ESC)</b>", GUILayout.Height(40)))
            {
                inventoryOpen = false;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawTeamNameEditor()
        {
            float panelW = 400;
            float panelH = 250;
            Rect panelRect = new Rect((Screen.width - panelW) / 2, (Screen.height - panelH) / 2, panelW, panelH);

            GUI.Box(panelRect, "");

            GUILayout.BeginArea(panelRect);
            GUILayout.BeginVertical("box");

            GUILayout.Label("<b><size=18>📝 EDIT TEAM NAMES</size></b>");
            GUILayout.Space(20);

            GUILayout.Label($"<b><color=green>Home Team:</color> {homeTeamName}</b>");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Lakers", GUILayout.Height(30))) homeTeamName = "LAKERS";
            if (GUILayout.Button("Bulls", GUILayout.Height(30))) homeTeamName = "BULLS";
            if (GUILayout.Button("Warriors", GUILayout.Height(30))) homeTeamName = "WARRIORS";
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.Label($"<b><color=red>Away Team:</color> {awayTeamName}</b>");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Celtics", GUILayout.Height(30))) awayTeamName = "CELTICS";
            if (GUILayout.Button("Heat", GUILayout.Height(30))) awayTeamName = "HEAT";
            if (GUILayout.Button("Nets", GUILayout.Height(30))) awayTeamName = "NETS";
            GUILayout.EndHorizontal();
            GUILayout.Space(20);

            if (GUILayout.Button("<b>✓ CLOSE (ESC)</b>", GUILayout.Height(40)))
            {
                editingTeamNames = false;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void Select(int i)
        {
            selectedIndex = i;
            selectedObjectName = objectList[selectedIndex];
            inventoryOpen = false;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        public override void OnUpdate()
        {
            if (Camera.main == null) return;

            UpdateScoreboard();
            CheckForBasket();

            if (Input.GetKeyDown(KeyCode.F1))
                guiVisible = !guiVisible;

            if (Input.GetKeyDown(KeyCode.F6))
            {
                activeLanguage = (activeLanguage + 1) % languages.Length;
                MelonLogger.Msg($"Language: {languages[activeLanguage]}");
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                inventoryOpen = !inventoryOpen;
                Cursor.visible = inventoryOpen;
                Cursor.lockState = inventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                homeScore = 0;
                awayScore = 0;
                MelonLogger.Msg("Scores reset!");
            }

            if (Input.GetKeyDown(KeyCode.Escape) && inventoryOpen)
            {
                inventoryOpen = false;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }

            if (Input.GetKeyDown(KeyCode.Escape) && editingTeamNames)
            {
                editingTeamNames = false;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }

            if (Input.GetKeyDown(KeyCode.Delete))
            {
                int total = objectsInHand.Count + placedObjects.Count;
                foreach (var obj in objectsInHand)
                    if (obj != null) GameObject.Destroy(obj);
                foreach (var obj in placedObjects)
                    if (obj != null) GameObject.Destroy(obj);
                objectsInHand.Clear();
                placedObjects.Clear();
                ballInHand = null;
                MelonLogger.Msg($"{total} objects deleted!");
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (editingTeamNames)
                {
                    editingTeamNames = false;
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                    MelonLogger.Msg($"Team names set: {homeTeamName} vs {awayTeamName}");
                }
                else
                {
                    GameObject nearestScoreboard = null;
                    float nearestDist = float.MaxValue;

                    foreach (GameObject obj in placedObjects)
                    {
                        if (obj != null && obj.name.Contains("BuiltInScoreboard"))
                        {
                            float dist = Vector3.Distance(Camera.main.transform.position, obj.transform.position);
                            if (dist < nearestDist)
                            {
                                nearestDist = dist;
                                nearestScoreboard = obj;
                            }
                        }
                    }

                    if (nearestScoreboard != null && nearestDist < 5f)
                    {
                        editingTeamNames = true;
                        Cursor.visible = true;
                        Cursor.lockState = CursorLockMode.None;
                    }
                    else
                    {
                        PickUpOrDropBall();
                    }
                }
            }

            if (inventoryOpen || editingTeamNames) return;

            HandleControls();
        }

        private void PickUpOrDropBall()
        {
            if (ballInHand != null)
            {
                ballInHand.transform.SetParent(null);

                Rigidbody rb = ballInHand.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                }

                Collider col = ballInHand.GetComponent<Collider>();
                if (col != null)
                    col.enabled = true;

                TrailRenderer trail = ballInHand.GetComponent<TrailRenderer>();
                if (trail != null)
                    trail.enabled = false;

                ballInHand = null;
                powerLoading = false;
                powerAmount = 0f;
            }
            else
            {
                Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 5f))
                {
                    if (hit.collider != null && hit.collider.gameObject.name.Contains("BuiltInSphere"))
                    {
                        ballInHand = hit.collider.gameObject;

                        Rigidbody rb = ballInHand.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.isKinematic = true;
                            rb.useGravity = false;
                            rb.velocity = Vector3.zero;
                            rb.angularVelocity = Vector3.zero;
                        }

                        Collider col = ballInHand.GetComponent<Collider>();
                        if (col != null)
                            col.enabled = false;

                        ballInHand.transform.SetParent(Camera.main.transform);
                        ballInHand.transform.localPosition = new Vector3(0.6f, -0.4f, 1.2f);
                        ballInHand.transform.localRotation = Quaternion.identity;

                        MelonLogger.Msg("🏀 Basketball picked up!");
                    }
                }
            }
        }

        private void HandleControls()
        {
            if (ballInHand != null)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    powerLoading = true;
                    powerAmount = 0f;
                    ballThrowPosition = ballInHand.transform.position;
                }

                if (Input.GetMouseButton(0) && powerLoading)
                {
                    if (Input.GetMouseButton(1))
                    {
                        powerLoading = false;
                        powerAmount = 0f;

                        if (targetIndicator != null)
                        {
                            GameObject.Destroy(targetIndicator);
                            targetIndicator = null;
                        }

                        return;
                    }

                    powerAmount += powerLoadSpeed * Time.deltaTime;
                    powerAmount = Mathf.Clamp(powerAmount, 0f, maxPower);

                    UpdateTargetIndicator();
                }

                if (Input.GetMouseButtonUp(0) && powerLoading)
                {
                    ThrowBall();
                    powerLoading = false;
                    powerAmount = 0f;

                    if (targetIndicator != null)
                    {
                        GameObject.Destroy(targetIndicator);
                        targetIndicator = null;
                    }
                }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.G))
                {
                    flightMode = !flightMode;
                    movingObject = Camera.main.transform.root;
                    SetGhostMode(flightMode);
                }

                if (flightMode && movingObject != null) FlightSystem();

                if (Input.GetMouseButtonDown(0)) CreateObject();
                if (Input.GetMouseButtonDown(1)) PlaceObject();

                UpdateObjectTransform();
            }
        }

        private void ThrowBall()
        {
            if (ballInHand == null) return;

            if (targetIndicator != null)
            {
                GameObject.Destroy(targetIndicator);
                targetIndicator = null;
            }

            ballInHand.transform.SetParent(null);

            Rigidbody rb = ballInHand.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;

                Vector3 throwDirection = Camera.main.transform.forward;
                float throwPower = Mathf.Lerp(8f, maxPower, powerAmount / maxPower);

                throwDirection = Quaternion.AngleAxis(-8f, Camera.main.transform.right) * throwDirection;

                rb.velocity = Vector3.zero;
                rb.AddForce(throwDirection * throwPower, ForceMode.Impulse);
                rb.AddTorque(-Camera.main.transform.right * (throwPower * 0.7f), ForceMode.Impulse);
            }

            Collider col = ballInHand.GetComponent<Collider>();
            if (col != null)
                col.enabled = true;

            TrailRenderer trail = ballInHand.GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.enabled = true;
                trail.Clear();
            }

            basketCheckActive = true;
            basketCheckDuration = 0f;

            ballInHand = null;
        }

        private void UpdateObjectTransform()
        {
            if (objectsInHand.Count > 0)
            {
                GameObject last = objectsInHand[objectsInHand.Count - 1];
                if (last == null) return;

                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (scroll != 0) objectDistance = Mathf.Clamp(objectDistance + scroll * 5f, 0.5f, 25f);
                if (Input.GetKey(KeyCode.R)) currentRotation.y += 100f * Time.deltaTime;
                if (Input.GetKey(KeyCode.T)) currentRotation.x += 100f * Time.deltaTime;
                if (Input.GetKey(KeyCode.Y)) currentScale += 1.5f * Time.deltaTime;
                if (Input.GetKey(KeyCode.U)) currentScale = Mathf.Max(0.1f, currentScale - 1.5f * Time.deltaTime);

                last.transform.localPosition = new Vector3(0, 0, objectDistance);

                if (selectedObjectName == "BuiltInBasketballHoopHome" ||
                    selectedObjectName == "BuiltInBasketballHoopAway" ||
                    selectedObjectName == "BuiltInScoreboard" ||
                    selectedObjectName == "BuiltInFloor" ||
                    selectedObjectName == "BuiltInCourtWall")
                {
                    last.transform.rotation = Quaternion.Euler(0, currentRotation.y, 0);
                }
                else
                {
                    last.transform.localRotation = Quaternion.Euler(currentRotation);
                }

                last.transform.localScale = Vector3.one * currentScale;
            }
        }

        private void CreateObject()
        {
            GameObject refObj = null;

            if (objectCache.ContainsKey(selectedObjectName))
            {
                refObj = objectCache[selectedObjectName];
                MelonLogger.Msg($"Retrieved from cache: {selectedObjectName}, Null? {(refObj == null ? "YES" : "NO")}");

                if (refObj == null)
                {
                    MelonLogger.Msg($"{selectedObjectName} in cache is destroyed! Cleaning...");
                    objectCache.Remove(selectedObjectName);
                }
            }
            else
            {
                MelonLogger.Msg($"{selectedObjectName} not in cache, searching scene...");
            }

            if (refObj == null)
            {
                foreach (var go in GameObject.FindObjectsOfType<GameObject>())
                {
                    if (go.name.Contains(selectedObjectName) && !go.name.StartsWith("Mod_"))
                    {
                        refObj = go;
                        MelonLogger.Msg($"Found in scene: {go.name}");
                        break;
                    }
                }
            }

            if (refObj != null)
            {
                MelonLogger.Msg($"🔨 CreateObject starting: {selectedObjectName}");
                MelonLogger.Msg($"   refObj active: {refObj.activeSelf}");

                GameObject newObj = GameObject.Instantiate(refObj);
                newObj.name = "Mod_" + selectedObjectName;
                newObj.isStatic = false;

                MelonLogger.Msg($"   Instantiated: {newObj.name}");

                newObj.SetActive(true);
                MelonLogger.Msg($"   SetActive(true) done");

                newObj.transform.SetParent(Camera.main.transform);
                newObj.transform.localPosition = new Vector3(0, 0, objectDistance);

                MelonLogger.Msg($"   Parent set, position: {newObj.transform.position}");

                int rendererCount = newObj.GetComponentsInChildren<Renderer>(true).Length;
                MelonLogger.Msg($"   Renderer count (including inactive): {rendererCount}");

                foreach (var r in newObj.GetComponentsInChildren<Renderer>(true))
                {
                    r.enabled = true;
                    MelonLogger.Msg($"   Renderer enabled: {r.gameObject.name}");
                }

                var rb = newObj.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;

                var col = newObj.GetComponent<Collider>();
                if (col != null) col.enabled = false;

                objectsInHand.Add(newObj);
                MelonLogger.Msg($"✅ {selectedObjectName} created and added to list!");
            }
            else
            {
                MelonLogger.Msg($"❌ ERROR: {selectedObjectName} not found anywhere!");
            }
        }

        private void PlaceObject()
        {
            if (objectsInHand.Count > 0)
            {
                int idx = objectsInHand.Count - 1;
                GameObject obj = objectsInHand[idx];

                if (obj != null)
                {
                    obj.transform.SetParent(null);

                    if (selectedObjectName == "BuiltInBasketballHoop" ||
                        selectedObjectName == "BuiltInScoreboard" ||
                        selectedObjectName == "BuiltInFloor" ||
                        selectedObjectName == "BuiltInCourtWall")
                    {
                        Vector3 currentPos = obj.transform.position;
                        obj.transform.rotation = Quaternion.Euler(0, obj.transform.rotation.eulerAngles.y, 0);
                        obj.transform.position = currentPos;
                    }

                    var col = obj.GetComponent<Collider>();
                    if (col != null) col.enabled = true;

                    var rb = obj.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        if (selectedObjectName == "BuiltInBasketballHoopHome" ||
                            selectedObjectName == "BuiltInBasketballHoopAway" ||
                            selectedObjectName == "BuiltInGlassWall" ||
                            selectedObjectName == "BuiltInFloor" ||
                            selectedObjectName == "BuiltInScoreboard" ||
                            selectedObjectName == "BuiltInCourtWall")
                        {
                            rb.isKinematic = true;
                            rb.useGravity = false;
                        }
                        else
                        {
                            rb.isKinematic = false;
                            rb.useGravity = true;
                            rb.AddForce(Camera.main.transform.forward * 10f, ForceMode.Impulse);
                        }
                    }

                    placedObjects.Add(obj);
                }

                objectsInHand.RemoveAt(idx);
                currentRotation = Vector3.zero;
                currentScale = 1.0f;
            }
        }

        private void FlightSystem()
        {
            Physics.gravity = Vector3.zero;
            float speed = Input.GetKey(KeyCode.LeftAlt) ? 40f : 15f;
            Vector3 velocity = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) velocity += Camera.main.transform.forward;
            if (Input.GetKey(KeyCode.S)) velocity -= Camera.main.transform.forward;
            if (Input.GetKey(KeyCode.D)) velocity += Camera.main.transform.right;
            if (Input.GetKey(KeyCode.A)) velocity -= Camera.main.transform.right;
            if (Input.GetKey(KeyCode.Space)) velocity += Vector3.up;
            if (Input.GetKey(KeyCode.LeftShift)) velocity -= Vector3.up;
            movingObject.position += velocity * speed * Time.deltaTime;
        }

        private void SetGhostMode(bool active)
        {
            if (movingObject == null) return;
            foreach (var c in movingObject.GetComponentsInChildren<Collider>())
                c.enabled = !active;
            if (!active) Physics.gravity = new Vector3(0, -9.81f, 0);
        }

        private void UpdateTargetIndicator()
        {
            if (ballInHand == null) return;

            Vector3 throwDirection = Camera.main.transform.forward;
            float throwPower = Mathf.Lerp(8f, maxPower, powerAmount / maxPower);
            throwDirection = Quaternion.AngleAxis(-8f, Camera.main.transform.right) * throwDirection;

            Vector3 startPos = ballInHand.transform.position;
            Vector3 velocity = throwDirection * throwPower;

            Vector3 targetPos = startPos;
            float time = 0f;
            float step = 0.05f;
            float maxTime = 5f;

            while (time < maxTime)
            {
                time += step;
                Vector3 newPos = startPos + velocity * time + 0.5f * Physics.gravity * time * time;

                RaycastHit hit;
                if (Physics.Raycast(targetPos, newPos - targetPos, out hit, (newPos - targetPos).magnitude))
                {
                    targetPos = hit.point;
                    break;
                }

                targetPos = newPos;

                if (targetPos.y < startPos.y - 50f)
                    break;
            }

            if (targetIndicator == null)
            {
                targetIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                targetIndicator.name = "TargetIndicator";

                Collider col = targetIndicator.GetComponent<Collider>();
                if (col != null) GameObject.Destroy(col);

                MeshRenderer mr = targetIndicator.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = new Color(1.0f, 1.0f, 0.0f, 0.6f);
                    mr.material = mat;
                }
            }

            targetIndicator.transform.position = targetPos + Vector3.up * 0.05f;
            targetIndicator.transform.rotation = Quaternion.Euler(0, 0, 0);
            targetIndicator.transform.localScale = new Vector3(0.8f, 0.05f, 0.8f);
        }
    }
}
